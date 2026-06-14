using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Application.Extensions;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RDS.ExpenseTracker.Application.Services;

public class SatisPayCsvImportService : ISatisPayCsvImportService
{
    // ── Satispay transaction type identifiers ───────────────────────────────
    private const string SatispayChargeType = "CHARGE";           // "a una Persona" or "da una Persona"
    private const string SatispayBankRechargeType = "BANK_RECHARGE"; // "Ricarica Satispay"

    private static readonly HashSet<string> BankRechargeDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ricarica Satispay",
    };

    private static readonly Regex IbanExtractor = new(@"\(([A-Z]{2}\d{2}[A-Z0-9]{1,30})\)");
    private static readonly Regex AmountExtractor = new(@"[-+]?\d+(?:[\.,]\d+)?");

    // ── Dependencies ────────────────────────────────────────────────────────
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ISatisPayCsvOptions _csvOptions;
    private readonly ILogger<SatisPayCsvImportService> _logger;

    public SatisPayCsvImportService(
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        ISatisPayCsvOptions csvOptions,
        ILogger<SatisPayCsvImportService> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository    = transferRepository    ?? throw new ArgumentNullException(nameof(transferRepository));
        _categoryRepository    = categoryRepository    ?? throw new ArgumentNullException(nameof(categoryRepository));
        _accountRepository     = accountRepository     ?? throw new ArgumentNullException(nameof(accountRepository));
        _csvOptions            = csvOptions            ?? throw new ArgumentNullException(nameof(csvOptions));
        _logger                = logger                ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public async Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, bool importAll = false)
    {
        try
        {
            if (fileStream == null || !fileStream.CanRead)
                return Result.Fail("File stream is null or unreadable");

            _logger.LogInformation(
                "Starting Satispay CSV import. ImportAll={ImportAll}, FileName={FileName}",
                importAll, fileName);

            // 1. Parse -------------------------------------------------------
            var rows = ParseCsv(fileStream);
            if (rows.Count == 0)
            {
                _logger.LogInformation("CSV file contains no data rows");
                return Result.Ok(0);
            }

            // 2. Validate required identifier (Codice Identificativo / ID) --
            foreach (var row in rows)
            {
                row.TransactionId = row.TransactionId?.Trim();
            }

            var missingIdentifierRows = rows
                .Where(r => string.IsNullOrWhiteSpace(r.TransactionId))
                .ToList();

            if (missingIdentifierRows.Count > 0)
            {
                _logger.LogWarning(
                    "Satispay import rejected: {Count} rows have no codice identificativo (ID)",
                    missingIdentifierRows.Count);

                return Result.Fail(
                    $"Import failed: {missingIdentifierRows.Count} rows have no codice identificativo (ID). The identifier is mandatory for all rows.");
            }

            // 3. Deduplicate by ExternalId (transaction ID) ------------------
            if (!importAll)
            {
                var externalIds = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.TransactionId))
                    .Select(r => r.TransactionId!)
                    .ToList();

                if (externalIds.Count > 0)
                {
                    var existingIds = (await _transactionRepository.GetExistingExternalIds(externalIds))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var beforeCount = rows.Count;
                    rows = rows
                        .Where(r => !existingIds.Contains(r.TransactionId!))
                        .ToList();

                    var skipped = beforeCount - rows.Count;
                    if (skipped > 0)
                        _logger.LogInformation("Skipped {Skipped} existing transactions by ExternalId", skipped);
                }
            }

            if (rows.Count == 0)
            {
                _logger.LogInformation("No new transactions to import after deduplication");
                return Result.Ok(0);
            }

            // 3. Ensure accounts ──────────────────────────────────────────────
            var requiredAccountNames = GetRequiredAccountNames(rows);
            var accounts = await EnsureAccountsExistAsync(requiredAccountNames);

            // 4. Get categories ───────────────────────────────────────────────
            var categories = (await _categoryRepository.GetCategories()).ToList();

            // 5. Build transactions ───────────────────────────────────────────
            var transactions = new List<Transaction>();
            var transferEntities = new List<Transfer>();
            var transferTransactions = new List<Transaction>();

            var satispayAccount = accounts.FirstOrDefault(a =>
                a.Name.Equals(_csvOptions.DefaultAccountName, StringComparison.OrdinalIgnoreCase));

            if (satispayAccount == null)
            {
                _logger.LogError("Could not find Satispay account '{AccountName}'", _csvOptions.DefaultAccountName);
                return Result.Fail($"Account '{_csvOptions.DefaultAccountName}' not found");
            }

            var defaultCategoryId = (int)CategoryEnum.Default;

            foreach (var row in rows)
            {
                // Check if this is a bank recharge (Ricarica Satispay)
                if (IsBankRechargeRow(row))
                {
                    var transferResult = await BuildBankRechargeTransfer(
                        row, satispayAccount, accounts, transferEntities, transferTransactions);

                    if (transferResult.IsSuccess)
                        continue; // Successfully added as transfer, skip plain transaction
                }

                // Otherwise, treat as plain transaction
                var tx = BuildTransaction(row, satispayAccount, categories, defaultCategoryId);
                if (tx != null)
                    transactions.Add(tx);
            }

            // 6. Persist ──────────────────────────────────────────────────────
            if (transactions.Count > 0)
            {
                await _transactionRepository.AddTransactions(transactions);
                _logger.LogInformation("Added {Count} transactions", transactions.Count);
            }

            if (transferEntities.Count > 0)
            {
                // Add all transactions associated with transfers
                await _transactionRepository.AddTransactions(transferTransactions);
                foreach (var transfer in transferEntities)
                {
                    await _transferRepository.AddTransfer(transfer);
                }
                _logger.LogInformation("Added {Count} transfers with {TxCount} transactions",
                    transferEntities.Count, transferTransactions.Count);
            }

            await _transactionRepository.SaveChangesAsync();
            var totalImported = transactions.Count + transferTransactions.Count;

            _logger.LogInformation("Satispay CSV import completed successfully. Total imported: {Total}",
                totalImported);

            return Result.Ok(totalImported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Satispay CSV import");
            return Result.Fail($"Import failed: {ex.Message}");
        }
    }

    // ── Private Helpers ─────────────────────────────────────────────────────

    private List<SatisPayRow> ParseCsv(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";", // Satispay uses semicolon delimiter
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
        });

        csv.Context.TypeConverterOptionsCache.GetOptions<decimal?>().NullValues.Add(string.Empty);

        return [.. csv.GetRecords<SatisPayRow>()];
    }

    private IEnumerable<string> GetRequiredAccountNames(List<SatisPayRow> rows)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            _csvOptions.DefaultAccountName,
        };

        if (!string.IsNullOrEmpty(_csvOptions.BankAccountName))
            names.Add(_csvOptions.BankAccountName);

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.Description)) continue;

            var ibanMatch = IbanExtractor.Match(row.Description);
            if (!ibanMatch.Success) continue;

            var iban = ibanMatch.Groups[1].Value;
            var mapped = _csvOptions.IbanToAccountMap
                .FirstOrDefault(kvp =>
                    kvp.Key.Equals(iban.Trim(), StringComparison.OrdinalIgnoreCase))
                .Value;

            if (mapped != null) names.Add(mapped);
        }

        return names;
    }

    private async Task<List<Account>> EnsureAccountsExistAsync(IEnumerable<string> requiredAccountNames)
    {
        var accounts = (await _accountRepository.GetAccounts()).ToList();

        var missingNames = requiredAccountNames
            .Where(name => !accounts.Any(a =>
                a.Name != null && a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingNames.Count > 0)
        {
            var newAccounts = missingNames.Select(name => new Account(0, name)).ToList();
            await _accountRepository.AddAccounts(newAccounts);
            await _accountRepository.SaveChangesAsync();
            accounts = (await _accountRepository.GetAccounts()).ToList();
        }

        return accounts;
    }

    private bool IsBankRechargeRow(SatisPayRow row)
    {
        if (string.IsNullOrEmpty(row.Description)) return false;

        return BankRechargeDescriptions.Any(desc =>
            row.Description.Contains(desc, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Result> BuildBankRechargeTransfer(
        SatisPayRow row,
        Account satispayAccount,
        List<Account> accounts,
        List<Transfer> transferEntities,
        List<Transaction> transferTransactions)
    {
        // Extract IBAN from description: "Ricarica Satispay (IT21W0367401600000921182111)"
        var ibanMatch = IbanExtractor.Match(row.Description ?? "");
        if (!ibanMatch.Success)
        {
            _logger.LogWarning("Bank recharge row {ID} has no IBAN in description", row.TransactionId);
            return Result.Fail("No IBAN found in bank recharge description");
        }

        var iban = ibanMatch.Groups[1].Value;
        var bankAccountName = _csvOptions.IbanToAccountMap
            .FirstOrDefault(kvp =>
                kvp.Key.Equals(iban.Trim(), StringComparison.OrdinalIgnoreCase))
            .Value;

        if (string.IsNullOrEmpty(bankAccountName))
        {
            _logger.LogWarning("IBAN {IBAN} from row {ID} not found in IbanToAccountMap", iban, row.TransactionId);
            return Result.Fail($"IBAN {iban} not mapped to any account");
        }

        var bankAccount = accounts.FirstOrDefault(a =>
            a.Name.Equals(bankAccountName, StringComparison.OrdinalIgnoreCase));

        if (bankAccount == null)
        {
            _logger.LogWarning("Bank account '{BankAccount}' not found", bankAccountName);
            return Result.Fail($"Bank account '{bankAccountName}' not found");
        }

        // Check if transfer already exists (cross-source dedup)
        // Satispay Ricarica: bank → satispay, amount is positive in Satispay CSV
        var parsedAmount = ParseAmount(row.AmountRaw);
        if (!parsedAmount.HasValue)
        {
            _logger.LogWarning("Skipping bank recharge row {ID}: invalid amount '{AmountRaw}'", row.TransactionId, row.AmountRaw);
            return Result.Fail("Invalid amount");
        }

        var amount = Math.Abs(parsedAmount.Value);
        var date = ParseDate(row.Date);

        if (date.HasValue)
        {
            var existingTransfer = await _transferRepository.GetExistingTransfer(
                bankAccount.Id, satispayAccount.Id, amount, date.Value);

            if (existingTransfer != null)
            {
                _logger.LogInformation(
                    "Transfer already exists: {From} → {To} amount {Amount} on {Date}. Skipping row {ID}",
                    bankAccountName, _csvOptions.DefaultAccountName, amount, date.Value.Date, row.TransactionId);
                return Result.Fail("Transfer already exists");
            }
        }

        // Create transfer entity
        var transferEntity = new Transfer
        {
            CreatedOn = DateTime.UtcNow,
            ExternalId = row.TransactionId, // Mark transfer with Satispay ID
        };
        transferEntities.Add(transferEntity);

        var description = BuildDescription(row);

        // FROM: bank account (outbound, negative amount)
        transferTransactions.Add(new Transaction
        {
            AccountId = bankAccount.Id,
            CategoryId = (int)CategoryEnum.MoneyTransfers,
            Amount = -amount,
            Description = description,
            Date = date ?? DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow,
            ExternalId = null, // No ID on bank leg to avoid accidental dedup with bank CSV imports
            TransferNavigation = transferEntity,
        });

        // TO: Satispay account (inbound, positive amount)
        transferTransactions.Add(new Transaction
        {
            AccountId = satispayAccount.Id,
            CategoryId = (int)CategoryEnum.MoneyTransfers,
            Amount = amount,
            Description = description,
            Date = date ?? DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow,
            ExternalId = row.TransactionId, // Mark receive side for dedup
            TransferNavigation = transferEntity,
        });

        _logger.LogInformation("Created bank recharge transfer: {From} → {To} amount {Amount}", 
            bankAccountName, _csvOptions.DefaultAccountName, amount);

        return Result.Ok();
    }

    private Transaction? BuildTransaction(
        SatisPayRow row,
        Account satispayAccount,
        List<Category> categories,
        int defaultCategoryId)
    {
        // P2P transfers and other charges become plain transactions
        // (not Transfer entities, as counterparty is typically not in our accounts)

        var amount = ParseAmount(row.AmountRaw);
        if (!amount.HasValue)
        {
            _logger.LogWarning("Skipping row {ID}: invalid amount '{AmountRaw}'", row.TransactionId, row.AmountRaw);
            return null;
        }

        var date = ParseDate(row.Date) ?? DateTime.UtcNow;
        var description = BuildDescription(row);

        var categoryId = GetCategoryForRow(row, categories, defaultCategoryId);

        return new Transaction
        {
            AccountId = satispayAccount.Id,
            CategoryId = categoryId,
            Amount = amount.Value,
            Description = description,
            Date = date,
            CreatedOn = DateTime.UtcNow,
            ExternalId = row.TransactionId,
        };
    }

    private int GetCategoryForRow(SatisPayRow row, List<Category> categories, int defaultCategoryId)
    {
        // Satispay categories: use description to infer category
        // "Ricarica" → Savings & Investments or Banking
        // "Pagamento" → Expenses
        // Merchant-based matching if description contains known keywords

        if (string.IsNullOrEmpty(row.Description))
            return defaultCategoryId;

        var description = row.Description.ToLowerInvariant();

        // Check for recharge indicators
        if (description.Contains("ricarica"))
            return (int)CategoryEnum.SavingsAndInvestments;

        // Check for subscription/recurring patterns
        if (description.Contains("abbonamento") || description.Contains("subscription"))
            return (int)CategoryEnum.Entertainment;

        // Check for food/dining
        if (description.Contains("ristorante") || description.Contains("bar") || 
            description.Contains("pizza") || description.Contains("café"))
            return (int)CategoryEnum.FoodAndBeverage;

        // Check for transport
        if (description.Contains("uber") || description.Contains("taxi") || 
            description.Contains("benzina") || description.Contains("carburante"))
            return (int)CategoryEnum.Transportation;

        // Check for shopping
        if (description.Contains("amazon") || description.Contains("ebay") || 
            description.Contains("negozio") || description.Contains("shop"))
            return (int)CategoryEnum.FoodAndBeverage;

        return defaultCategoryId;
    }

    private string BuildDescription(SatisPayRow row)
    {
        return string.IsNullOrEmpty(row.Name)
            ? (row.Description ?? "Satispay transaction")
            : row.Name;
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        // Satispay formats: dd/MM/yyyy HH:mm or dd/MM/yyyy
        if (DateTime.TryParseExact(dateString, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dateTime))
            return dateTime;

        if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date))
            return date;

        _logger.LogWarning("Could not parse date '{Date}'", dateString);
        return null;
    }

    private decimal? ParseAmount(string? amountRaw)
    {
        if (string.IsNullOrWhiteSpace(amountRaw))
            return null;

        var normalized = amountRaw
            .Replace("€", string.Empty, StringComparison.Ordinal)
            .Replace("EUR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("∩┐╜", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

        var match = AmountExtractor.Match(normalized);
        if (!match.Success)
            return null;

        var numeric = match.Value.Replace(',', '.');

        if (decimal.TryParse(numeric, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var amount))
        {
            return amount;
        }

        return null;
    }

    // ── CSV Row Model ───────────────────────────────────────────────────────

    /// <summary>
    /// Satispay CSV row model
    /// Columns: Data, Nome, Descrizione, Importo, Tipo, ID, (additional columns ignored)
    /// </summary>
    public class SatisPayRow
    {
        [Name("Data")]
        public string? Date { get; set; }

        [Name("Nome")]
        public string? Name { get; set; }

        [Name("Descrizione")]
        public string? Description { get; set; }

        [Name("Importo")]
        public string? AmountRaw { get; set; }

        [Name("Tipo")]
        public string? Type { get; set; }

        [Index(8)]
        [Name("ID", "ID (Comunicalo all'Assistenza Clienti in caso di problemi)")]
        public string? TransactionId { get; set; }
    }
}
