using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RDS.ExpenseTracker.Application.Services;

public class SellaCsvImportService : ISellaCsvImportService
{
    private const int TransactionDescriptionMaxLength = 500;

    private static readonly Regex IbanRegex = new(@"\b[A-Z]{2}\d{2}[A-Z0-9]{11,30}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ISellaCsvOptions _options;
    private readonly ILogger<SellaCsvImportService> _logger;

    public SellaCsvImportService(
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        ISellaCsvOptions options,
        ILogger<SellaCsvImportService> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository = transferRepository ?? throw new ArgumentNullException(nameof(transferRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, bool importAll = false)
    {
        try
        {
            if (fileStream == null || !fileStream.CanRead)
                return Result.Fail("File stream is null or unreadable");

            var rows = ParseCsv(fileStream);
            if (rows.Count == 0)
                return Result.Ok(0);

            if (!importAll)
            {
                var externalIds = rows.Select(r => r.Identifier).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existingIds = (await _transactionRepository.GetExistingExternalIds(externalIds))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                rows = rows
                    .Where(r => !existingIds.Contains(r.Identifier))
                    .ToList();
            }

            if (rows.Count == 0)
                return Result.Ok(0);

            var accounts = await EnsureAccountsExistAsync(GetRequiredAccountNames(rows));
            var defaultCategory = await _categoryRepository.GetDefaultCategory();
            var defaultCategoryId = defaultCategory?.Id ?? (int)CategoryEnum.Default;

            var sellaAccount = accounts.FirstOrDefault(a =>
                a.Name.Equals(_options.DefaultAccountName, StringComparison.OrdinalIgnoreCase));

            if (sellaAccount == null)
                return Result.Fail($"Account '{_options.DefaultAccountName}' not found");

            var transactions = new List<Transaction>();
            var transferEntities = new List<Transfer>();
            var transferTransactions = new List<Transaction>();

            foreach (var row in rows)
            {
                if (TryResolveCounterpartyAccount(row, accounts, out var counterpartyAccount) && counterpartyAccount != null)
                {
                    var existingByExternalId = await _transferRepository.GetTransferByExternalId(row.Identifier);
                    if (existingByExternalId != null)
                        continue;

                    var amount = Math.Abs(row.Amount);
                    var isOutbound = row.Amount < 0;

                    var fromAccount = isOutbound ? sellaAccount : counterpartyAccount;
                    var toAccount = isOutbound ? counterpartyAccount : sellaAccount;

                    var existingTransfer = await _transferRepository.GetExistingTransfer(
                        fromAccount.Id,
                        toAccount.Id,
                        amount,
                        row.OperationDate);

                    if (existingTransfer != null)
                        continue;

                    var transfer = new Transfer
                    {
                        CreatedOn = DateTime.UtcNow,
                        ExternalId = row.Identifier,
                    };

                    transferEntities.Add(transfer);

                    var description = BuildSafeDescription(row.Description, row.Identifier);

                    transferTransactions.Add(new Transaction
                    {
                        AccountId = fromAccount.Id,
                        CategoryId = (int)CategoryEnum.MoneyTransfers,
                        Amount = -amount,
                        Description = description,
                        Date = row.OperationDate,
                        CreatedOn = DateTime.UtcNow,
                        ExternalId = isOutbound ? row.Identifier : null,
                        TransferNavigation = transfer,
                    });

                    transferTransactions.Add(new Transaction
                    {
                        AccountId = toAccount.Id,
                        CategoryId = (int)CategoryEnum.MoneyTransfers,
                        Amount = amount,
                        Description = description,
                        Date = row.OperationDate,
                        CreatedOn = DateTime.UtcNow,
                        ExternalId = isOutbound ? null : row.Identifier,
                        TransferNavigation = transfer,
                    });

                    continue;
                }

                transactions.Add(new Transaction
                {
                    AccountId = sellaAccount.Id,
                    CategoryId = defaultCategoryId,
                    Amount = row.Amount,
                    Description = BuildSafeDescription(row.Description, row.Identifier),
                    Date = row.OperationDate,
                    CreatedOn = DateTime.UtcNow,
                    ExternalId = row.Identifier,
                });
            }

            if (transferEntities.Count > 0)
            {
                foreach (var transfer in transferEntities)
                    await _transferRepository.AddTransfer(transfer);

                await _transactionRepository.AddTransactions(transferTransactions);
                await _transferRepository.SaveChangesAsync();
            }

            if (transactions.Count > 0)
            {
                await _transactionRepository.AddTransactions(transactions);
                await _transactionRepository.SaveChangesAsync();
            }

            return Result.Ok(transactions.Count + transferTransactions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Sella CSV import");
            return Result.Fail($"Import failed: {ex.Message}");
        }
    }

    private static List<SellaCsvRow> ParseCsv(Stream fileStream)
    {
        fileStream.Position = 0;

        using var reader = new StreamReader(fileStream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
        });

        var rawRows = csv.GetRecords<SellaCsvRawRow>().ToList();
        var rows = new List<SellaCsvRow>(rawRows.Count);

        foreach (var rawRow in rawRows)
        {
            if (IsEmptyOrBalanceRow(rawRow))
                continue;

            var identifier = rawRow.Identifier?.Trim();
            if (string.IsNullOrWhiteSpace(identifier))
                throw new InvalidOperationException("Import failed: Codice identificativo is mandatory for all rows.");

            var operationDate = ParseDate(rawRow.OperationDateRaw)
                ?? throw new InvalidOperationException($"Import failed: invalid Data operazione for row '{identifier}'.");

            var amount = ParseAmount(rawRow.DebitRaw, rawRow.CreditRaw)
                ?? throw new InvalidOperationException($"Import failed: invalid amount for row '{identifier}'.");

            var description = NormalizeDescription(rawRow.Description);
            var iban = ExtractIban(description);

            rows.Add(new SellaCsvRow(
                identifier,
                operationDate,
                amount,
                description,
                iban));
        }

        return rows;
    }

    private static bool IsEmptyOrBalanceRow(SellaCsvRawRow row)
    {
        var description = row.Description?.Trim() ?? string.Empty;

        if (description.StartsWith("Saldo al ", StringComparison.OrdinalIgnoreCase))
            return true;

        return string.IsNullOrWhiteSpace(row.Identifier)
            && string.IsNullOrWhiteSpace(row.OperationDateRaw)
            && string.IsNullOrWhiteSpace(row.ValueDateRaw)
            && string.IsNullOrWhiteSpace(row.Description)
            && string.IsNullOrWhiteSpace(row.DebitRaw)
            && string.IsNullOrWhiteSpace(row.CreditRaw);
    }

    private IEnumerable<string> GetRequiredAccountNames(IEnumerable<SellaCsvRow> rows)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            _options.DefaultAccountName,
        };

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.CounterpartyIban))
                continue;

            var mapped = _options.IbanToAccountMap
                .FirstOrDefault(kvp => kvp.Key.Equals(row.CounterpartyIban, StringComparison.OrdinalIgnoreCase))
                .Value;

            if (!string.IsNullOrWhiteSpace(mapped))
                names.Add(mapped);
        }

        return names;
    }

    private async Task<List<Account>> EnsureAccountsExistAsync(IEnumerable<string> requiredAccountNames)
    {
        var accounts = (await _accountRepository.GetAccounts()).ToList();

        var missing = requiredAccountNames
            .Where(name => !accounts.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missing.Count > 0)
        {
            await _accountRepository.AddAccounts(missing.Select(name => new Account(0, name)));
            await _accountRepository.SaveChangesAsync();
            accounts = (await _accountRepository.GetAccounts()).ToList();
        }

        return accounts;
    }

    private bool TryResolveCounterpartyAccount(SellaCsvRow row, List<Account> accounts, out Account? account)
    {
        account = null;

        if (string.IsNullOrWhiteSpace(row.CounterpartyIban))
            return false;

        var mappedName = _options.IbanToAccountMap
            .FirstOrDefault(kvp => kvp.Key.Equals(row.CounterpartyIban, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (string.IsNullOrWhiteSpace(mappedName))
            return false;

        if (mappedName.Equals(_options.DefaultAccountName, StringComparison.OrdinalIgnoreCase))
            return false;

        account = accounts.FirstOrDefault(a => a.Name.Equals(mappedName, StringComparison.OrdinalIgnoreCase));
        return account != null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParseExact(
            value.Trim(),
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseAmount(string? debitRaw, string? creditRaw)
    {
        var debit = ParseItalianDecimal(debitRaw);
        var credit = ParseItalianDecimal(creditRaw);

        if (debit.HasValue && credit.HasValue)
            return null;

        if (credit.HasValue)
            return Math.Abs(credit.Value);

        if (debit.HasValue)
            return debit.Value > 0 ? -debit.Value : debit.Value;

        return null;
    }

    private static decimal? ParseItalianDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value
            .Replace("€", string.Empty, StringComparison.Ordinal)
            .Replace("EUR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(',', '.');

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private static string NormalizeDescription(string? description)
    {
        var normalized = Regex.Replace(description ?? string.Empty, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized;
    }

    private static string? ExtractIban(string description)
    {
        var match = IbanRegex.Match(description);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static string BuildSafeDescription(string? rawDescription, string identifier)
    {
        var normalized = Regex.Replace(rawDescription ?? string.Empty, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            normalized = $"Operazione Sella {identifier}";

        if (normalized.Length > TransactionDescriptionMaxLength)
            normalized = normalized[..TransactionDescriptionMaxLength];

        return normalized;
    }

    private sealed class SellaCsvRawRow
    {
        [Name("Codice identificativo")]
        public string? Identifier { get; set; }

        [Name("Data operazione")]
        public string? OperationDateRaw { get; set; }

        [Name("Data valuta")]
        public string? ValueDateRaw { get; set; }

        [Name("Descrizione")]
        public string? Description { get; set; }

        [Name("Debito")]
        public string? DebitRaw { get; set; }

        [Name("Credito")]
        public string? CreditRaw { get; set; }
    }

    private sealed record SellaCsvRow(
        string Identifier,
        DateTime OperationDate,
        decimal Amount,
        string Description,
        string? CounterpartyIban);
}
