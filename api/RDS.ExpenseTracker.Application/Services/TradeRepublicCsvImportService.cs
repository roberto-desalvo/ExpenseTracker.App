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

namespace RDS.ExpenseTracker.Application.Services;

public class TradeRepublicCsvImportService : ITradeRepublicCsvImportService
{
    // ── CSV transaction types ───────────────────────────────────────────────
    private const string TransferInstantInbound = "TRANSFER_INSTANT_INBOUND";
    private const string TransferInstantOutbound = "TRANSFER_INSTANT_OUTBOUND";
    private const string TransferDirectDebitInbound = "TRANSFER_DIRECT_DEBIT_INBOUND";
    private const string AccountTypeTrading = "TRADING";

    private static readonly HashSet<string> TransferTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        TransferInstantInbound,
        TransferInstantOutbound,
        TransferDirectDebitInbound,
    };

    /// <summary>
    /// TR transaction types that are always assigned to "Savings and investments",
    /// regardless of category tag matching.
    /// </summary>
    private static readonly HashSet<string> InvestmentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BUY", "SELL", "DIVIDEND", "INTEREST_PAYMENT", "BENEFITS_SAVEBACK",
    };

    private static readonly char[] TagSeparators = [',', ';'];

    // ── Dependencies ────────────────────────────────────────────────────────
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITradeRepublicCsvOptions _csvOptions;
    private readonly ITransferMatchingService _transferMatchingService;
    private readonly ILogger<TradeRepublicCsvImportService> _logger;

    public TradeRepublicCsvImportService(
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        ITradeRepublicCsvOptions csvOptions,
        ITransferMatchingService transferMatchingService,
        ILogger<TradeRepublicCsvImportService> logger)
    {
        _transactionRepository   = transactionRepository   ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository      = transferRepository      ?? throw new ArgumentNullException(nameof(transferRepository));
        _categoryRepository      = categoryRepository      ?? throw new ArgumentNullException(nameof(categoryRepository));
        _accountRepository       = accountRepository       ?? throw new ArgumentNullException(nameof(accountRepository));
        _csvOptions              = csvOptions              ?? throw new ArgumentNullException(nameof(csvOptions));
        _transferMatchingService = transferMatchingService ?? throw new ArgumentNullException(nameof(transferMatchingService));
        _logger                  = logger                  ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public async Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, bool importAll = false)
    {
        try
        {
            if (fileStream == null || !fileStream.CanRead)
                return Result.Fail("File stream is null or unreadable");

            _logger.LogInformation(
                "Starting Trade Republic CSV import. ImportAll={ImportAll}, FileName={FileName}",
                importAll, fileName);

            // 1. Parse -------------------------------------------------------
            var rows = ParseCsv(fileStream);
            if (rows.Count == 0)
            {
                _logger.LogInformation("CSV file contains no data rows");
                return Result.Ok(0);
            }

            // 2. Deduplicate by ExternalId (transaction_id) ------------------
            if (!importAll)
            {
                var externalIds = rows
                    .Where(r => !string.IsNullOrEmpty(r.TransactionId))
                    .Select(r => r.TransactionId!)
                    .ToList();

                if (externalIds.Count > 0)
                {
                    var existingIds = (await _transactionRepository.GetExistingExternalIds(externalIds))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var beforeCount = rows.Count;
                    rows = rows
                        .Where(r => string.IsNullOrEmpty(r.TransactionId) || !existingIds.Contains(r.TransactionId))
                        .ToList();

                    _logger.LogInformation(
                        "Deduplication: {Skipped} rows skipped (already imported), {Remaining} rows to process",
                        beforeCount - rows.Count, rows.Count);
                }
            }

            if (rows.Count == 0)
                return Result.Ok(0);

            // 3. Ensure all referenced accounts exist (auto-create if needed) -
            var accounts = await EnsureAccountsExistAsync(GetRequiredAccountNames(rows));

            // 4. Load categories ---------------------------------------------
            var categories = (await _categoryRepository.GetCategories())
                .OrderBy(c => c.Priority ?? int.MaxValue)
                .ToList();

            var defaultCategory   = await _categoryRepository.GetDefaultCategory();
            var defaultCategoryId = defaultCategory?.Id ?? (int)CategoryEnum.Default;

            // 5. Build entity lists ------------------------------------------
            var transactions       = new List<Transaction>();
            var transferEntities   = new List<Transfer>();
            var transferTransactions = new List<Transaction>();

            var candidateRows = new List<TradeRepublicRow>();
            var remainingRows = new List<TradeRepublicRow>();

            foreach (var row in rows)
            {
                if (row.Amount is null || row.Amount == 0) continue;

                var accountName = ResolveAccountName(row);
                var description = BuildDescription(row);

                if (_transferMatchingService.IsTransferCandidate(accountName, description))
                    candidateRows.Add(row);
                else
                    remainingRows.Add(row);
            }

            // Rule-matched rows must be processed oldest-first so that, when several rows
            // of the same amount are unmatched, each claims the oldest still-unlinked
            // counterpart (see ITransferMatchingService).
            var consumedCandidateIds = new HashSet<int>();
            foreach (var row in candidateRows.OrderBy(r => ParseDate(r.Datetime) ?? DateTime.UtcNow))
            {
                await ProcessTransferCandidateRow(
                    row, accounts, transactions, transferEntities, transferTransactions, consumedCandidateIds);
            }

            foreach (var row in remainingRows)
            {
                var isTransferType        = TransferTypes.Contains(row.Type ?? string.Empty);
                string? counterpartyName  = null;

                if (isTransferType && !string.IsNullOrEmpty(row.CounterpartyIban))
                {
                    counterpartyName = _csvOptions.IbanToAccountMap
                        .FirstOrDefault(kvp =>
                            kvp.Key.Equals(row.CounterpartyIban.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Value;
                }

                if (isTransferType && counterpartyName != null)
                {
                    BuildTransferEntities(row, counterpartyName, accounts, transferEntities, transferTransactions);
                }
                else
                {
                    var tx = BuildTransaction(row, accounts, categories, defaultCategoryId);
                    if (tx != null) transactions.Add(tx);
                }
            }

            // 6. Persist -----------------------------------------------------
            if (transferEntities.Count > 0)
            {
                foreach (var transferEntity in transferEntities)
                    await _transferRepository.AddTransfer(transferEntity);

                await _transactionRepository.AddTransactions(transferTransactions);
                await _transferRepository.SaveChangesAsync();
            }

            if (transactions.Count > 0)
            {
                await _transactionRepository.AddTransactions(transactions);
                await _transactionRepository.SaveChangesAsync();
            }

            var importedCount = transactions.Count + transferTransactions.Count;
            _logger.LogInformation(
                "Trade Republic CSV import completed. {Count} transaction records inserted ({Transfers} transfer legs, {Regular} regular)",
                importedCount, transferTransactions.Count, transactions.Count);

            return Result.Ok(importedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Trade Republic CSV import");
            return Result.Fail(new Error($"Import failed: {ex.Message}").CausedBy(ex));
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static List<TradeRepublicRow> ParseCsv(Stream fileStream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StreamReader(fileStream, leaveOpen: true);
        using var csv    = new CsvReader(reader, config);

        // Treat empty strings as null for nullable decimal columns
        csv.Context.TypeConverterOptionsCache.GetOptions<decimal?>().NullValues.Add(string.Empty);

        return [.. csv.GetRecords<TradeRepublicRow>()];
    }

    private IEnumerable<string> GetRequiredAccountNames(List<TradeRepublicRow> rows)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            _csvOptions.DefaultAccountName,
        };

        if (!string.IsNullOrEmpty(_csvOptions.TradingAccountName))
            names.Add(_csvOptions.TradingAccountName);

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.CounterpartyIban)) continue;

            var mapped = _csvOptions.IbanToAccountMap
                .FirstOrDefault(kvp =>
                    kvp.Key.Equals(row.CounterpartyIban.Trim(), StringComparison.OrdinalIgnoreCase))
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

    private void BuildTransferEntities(
        TradeRepublicRow row,
        string counterpartyAccountName,
        List<Account> accounts,
        List<Transfer> transferEntities,
        List<Transaction> transferTransactions)
    {
        // TRANSFER_INSTANT_INBOUND  → money flowing IN  to TR  (positive amount in CSV)
        //   FromAccount = counterparty,  ToAccount = Trade Republic
        // TRANSFER_INSTANT_OUTBOUND / TRANSFER_DIRECT_DEBIT_INBOUND
        //                           → money flowing OUT from TR (negative amount in CSV)
        //   FromAccount = Trade Republic, ToAccount = counterparty

        bool isInbound = string.Equals(row.Type, TransferInstantInbound, StringComparison.OrdinalIgnoreCase);

        string fromAccountName = isInbound ? counterpartyAccountName : _csvOptions.DefaultAccountName;
        string toAccountName   = isInbound ? _csvOptions.DefaultAccountName : counterpartyAccountName;

        var fromAccount = accounts.FirstOrDefault(a =>
            a.Name.Equals(fromAccountName, StringComparison.OrdinalIgnoreCase));
        var toAccount = accounts.FirstOrDefault(a =>
            a.Name.Equals(toAccountName, StringComparison.OrdinalIgnoreCase));

        if (fromAccount == null || toAccount == null)
        {
            _logger.LogWarning(
                "Skipping transfer row {TransactionId}: could not resolve account '{From}' or '{To}'",
                row.TransactionId, fromAccountName, toAccountName);
            return;
        }

        var transferEntity = new Transfer { CreatedOn = DateTime.UtcNow };
        transferEntities.Add(transferEntity);

        var date        = ParseDate(row.Datetime) ?? DateTime.UtcNow;
        var amount      = Math.Abs((row.Amount ?? 0) + (row.Tax ?? 0) + (row.Fee ?? 0));
        var description = BuildDescription(row);

        // ExternalId is placed on the Trade Republic side of the pair so that
        // deduplication (by transaction_id) works correctly on re-import.
        string? fromExternalId = isInbound ? null              : row.TransactionId;
        string? toExternalId   = isInbound ? row.TransactionId : null;

        transferTransactions.Add(new Transaction
        {
            AccountId          = fromAccount.Id,
            CategoryId         = (int)CategoryEnum.MoneyTransfers,
            Amount             = -amount,
            Description        = description,
            Date               = date,
            CreatedOn          = DateTime.UtcNow,
            ExternalId         = fromExternalId,
            TransferNavigation = transferEntity,
        });

        transferTransactions.Add(new Transaction
        {
            AccountId          = toAccount.Id,
            CategoryId         = (int)CategoryEnum.MoneyTransfers,
            Amount             = amount,
            Description        = description,
            Date               = date,
            CreatedOn          = DateTime.UtcNow,
            ExternalId         = toExternalId,
            TransferNavigation = transferEntity,
        });
    }

    private async Task ProcessTransferCandidateRow(
        TradeRepublicRow row,
        List<Account> accounts,
        List<Transaction> transactions,
        List<Transfer> transferEntities,
        List<Transaction> transferTransactions,
        HashSet<int> consumedCandidateIds)
    {
        var accountName = ResolveAccountName(row);
        var account = accounts.FirstOrDefault(a =>
            a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));

        if (account == null)
        {
            _logger.LogWarning(
                "Skipping row {TransactionId}: account '{Account}' not found",
                row.TransactionId, accountName);
            return;
        }

        var amount      = (row.Amount ?? 0) + (row.Tax ?? 0) + (row.Fee ?? 0);
        var date        = ParseDate(row.Datetime) ?? DateTime.UtcNow;
        var description = BuildDescription(row);

        var matchResult = await _transferMatchingService.TryMatchAsync(
            accountName, description, amount, date, consumedCandidateIds);

        if (matchResult.Candidate != null)
        {
            var transfer = new Transfer { CreatedOn = DateTime.UtcNow };
            transferEntities.Add(transfer);

            matchResult.Candidate.TransferNavigation = transfer;
            matchResult.Candidate.CategoryId = (int)CategoryEnum.MoneyTransfers;
            consumedCandidateIds.Add(matchResult.Candidate.Id);

            transferTransactions.Add(new Transaction
            {
                AccountId          = account.Id,
                CategoryId         = (int)CategoryEnum.MoneyTransfers,
                Amount             = amount,
                Description        = description,
                Date               = date,
                CreatedOn          = DateTime.UtcNow,
                ExternalId         = row.TransactionId,
                TransferNavigation = transfer,
            });
        }
        else
        {
            transactions.Add(new Transaction
            {
                AccountId   = account.Id,
                CategoryId  = (int)CategoryEnum.MoneyTransfers,
                Amount      = amount,
                Description = description,
                Date        = date,
                CreatedOn   = DateTime.UtcNow,
                ExternalId  = row.TransactionId,
            });
        }
    }

    private string ResolveAccountName(TradeRepublicRow row)
        => row.AccountType == AccountTypeTrading && !string.IsNullOrEmpty(_csvOptions.TradingAccountName)
            ? _csvOptions.TradingAccountName
            : _csvOptions.DefaultAccountName;

    private Transaction? BuildTransaction(
        TradeRepublicRow row,
        List<Account> accounts,
        List<Category> categories,
        int defaultCategoryId)
    {
        var accountName = ResolveAccountName(row);

        var account = accounts.FirstOrDefault(a =>
            a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase));

        if (account == null)
        {
            _logger.LogWarning(
                "Skipping row {TransactionId}: account '{Account}' not found",
                row.TransactionId, accountName);
            return null;
        }

        var amount = (row.Amount ?? 0) + (row.Tax ?? 0) + (row.Fee ?? 0);

        return new Transaction
        {
            AccountId   = account.Id,
            CategoryId  = DetermineCategory(row, categories, defaultCategoryId),
            Amount      = amount,
            Description = BuildDescription(row),
            Date        = ParseDate(row.Datetime) ?? DateTime.UtcNow,
            CreatedOn   = DateTime.UtcNow,
            ExternalId  = row.TransactionId,
        };
    }

    private int DetermineCategory(TradeRepublicRow row, List<Category> categories, int defaultCategoryId)
    {
        if (InvestmentTypes.Contains(row.Type ?? string.Empty))
            return (int)CategoryEnum.SavingsAndInvestments;

        var description = BuildDescription(row);

        foreach (var category in categories)
        {
            var tags = SplitTags(category.Tags).ToArray();
            if (tags.Length > 0 && description.ContainsOne(ignoreCase: true, tags))
                return category.Id;
        }

        return defaultCategoryId;
    }

    private static string BuildDescription(TradeRepublicRow row)
    {
        var desc = (row.Description ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(desc))
            desc = (row.Name ?? string.Empty).Trim();

        return desc.Length > 500 ? desc[..500] : desc;
    }

    private static DateTime? ParseDate(string? datetimeStr)
    {
        if (string.IsNullOrEmpty(datetimeStr)) return null;

        return DateTimeOffset.TryParse(
            datetimeStr,
            null,
            DateTimeStyles.RoundtripKind,
            out var dto)
            ? dto.UtcDateTime
            : null;
    }

    private static IEnumerable<string> SplitTags(string? tags)
        => string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // ── CSV row model ────────────────────────────────────────────────────────

    private sealed class TradeRepublicRow
    {
        [Name("datetime")]          public string?  Datetime        { get; set; }
        [Name("account_type")]      public string?  AccountType     { get; set; }
        [Name("type")]              public string?  Type            { get; set; }
        [Name("name")]              public string?  Name            { get; set; }
        [Name("amount")]            public decimal? Amount          { get; set; }
        [Name("fee")]               public decimal? Fee             { get; set; }
        [Name("tax")]               public decimal? Tax             { get; set; }
        [Name("description")]       public string?  Description     { get; set; }
        [Name("transaction_id")]    public string?  TransactionId   { get; set; }
        [Name("counterparty_iban")] public string?  CounterpartyIban { get; set; }
    }
}
