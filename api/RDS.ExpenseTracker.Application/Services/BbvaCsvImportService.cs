using CsvHelper;
using CsvHelper.Configuration;
using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Application.Extensions;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RDS.ExpenseTracker.Application.Services;

public class BbvaCsvImportService : IBbvaCsvImportService
{
    private const string HeaderPrefix = "Data valuta;Data;";
    private const int TransactionDescriptionMaxLength = 500;

    private static readonly char[] CategoryTagSeparators = [',', ';'];

    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBbvaCsvOptions _csvOptions;
    private readonly ILogger<BbvaCsvImportService> _logger;

    public BbvaCsvImportService(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        IBbvaCsvOptions csvOptions,
        ILogger<BbvaCsvImportService> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _csvOptions = csvOptions ?? throw new ArgumentNullException(nameof(csvOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<int>> ImportFromCsvAsync(Stream fileStream, string fileName, bool importAll = false)
    {
        try
        {
            if (fileStream == null || !fileStream.CanRead)
                return Result.Fail("File stream is null or unreadable");

            _logger.LogInformation(
                "Starting BBVA CSV import. ImportAll={ImportAll}, FileName={FileName}",
                importAll, fileName);

            var rows = ParseCsv(fileStream);
            if (rows.Count == 0)
            {
                _logger.LogInformation("BBVA CSV contains no data rows");
                return Result.Ok(0);
            }

            if (!importAll)
            {
                var fingerprints = rows.Select(BuildExternalId).ToList();
                var existingIds = (await _transactionRepository.GetExistingExternalIds(fingerprints))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var beforeCount = rows.Count;
                rows = rows
                    .Where(r => !existingIds.Contains(BuildExternalId(r)))
                    .ToList();

                var skipped = beforeCount - rows.Count;
                if (skipped > 0)
                {
                    _logger.LogInformation("Skipped {Skipped} BBVA rows already imported", skipped);
                }
            }

            if (rows.Count == 0)
            {
                _logger.LogInformation("No new BBVA transactions to import after deduplication");
                return Result.Ok(0);
            }

            var accounts = await EnsureAccountsExistAsync([_csvOptions.DefaultAccountName]);
            var categories = (await _categoryRepository.GetCategories())
                .OrderBy(c => c.Priority ?? int.MaxValue)
                .ToList();
            var defaultCategory = await _categoryRepository.GetDefaultCategory();
            var defaultCategoryId = defaultCategory?.Id ?? (int)CategoryEnum.Default;

            var account = accounts.FirstOrDefault(a =>
                a.Name.Equals(_csvOptions.DefaultAccountName, StringComparison.OrdinalIgnoreCase));

            if (account == null)
                return Result.Fail($"Account '{_csvOptions.DefaultAccountName}' not found");

            var transactions = new List<Transaction>(rows.Count);
            foreach (var row in rows)
            {
                var transactionDate = row.Date ?? row.ValueDate;
                if (!transactionDate.HasValue)
                {
                    _logger.LogWarning("Skipping BBVA row with missing valid date: {Row}", row.RawLine);
                    continue;
                }

                if (!row.Amount.HasValue)
                {
                    _logger.LogWarning("Skipping BBVA row with invalid amount: {Row}", row.RawLine);
                    continue;
                }

                var description = BuildDescription(row);

                transactions.Add(new Transaction
                {
                    AccountId = account.Id,
                    Amount = row.Amount.Value,
                    Date = transactionDate.Value,
                    Description = description,
                    CategoryId = DetermineCategoryId(description, categories, defaultCategoryId),
                    ExternalId = BuildExternalId(row),
                    CreatedOn = DateTime.UtcNow,
                });
            }

            if (transactions.Count == 0)
            {
                _logger.LogInformation("No valid BBVA rows after row-level validation");
                return Result.Ok(0);
            }

            await _transactionRepository.AddTransactions(transactions);
            await _transactionRepository.SaveChangesAsync();

            _logger.LogInformation("BBVA CSV import completed. Inserted {Count} transactions", transactions.Count);
            return Result.Ok(transactions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BBVA CSV import");
            return Result.Fail(new Error($"Import failed: {ex.Message}").CausedBy(ex));
        }
    }

    private List<BbvaRow> ParseCsv(Stream fileStream)
    {
        fileStream.Position = 0;

        using var reader = new StreamReader(fileStream, leaveOpen: true);
        var content = reader.ReadToEnd();

        var lines = content
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .ToList();

        var headerIndex = lines.FindIndex(l =>
            l.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase));

        if (headerIndex < 0)
        {
            _logger.LogWarning("BBVA CSV header not found. Expected prefix '{HeaderPrefix}'", HeaderPrefix);
            return [];
        }

        var normalizedCsv = string.Join(Environment.NewLine, lines.Skip(headerIndex));

        using var csvReader = new StringReader(normalizedCsv);
        using var csv = new CsvReader(csvReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
        });

        var rows = new List<BbvaRow>();
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var valueDateRaw = csv.GetField("Data valuta")?.Trim();
            var dateRaw = csv.GetField("Data")?.Trim();
            var keyword = csv.GetField("Parola chiave")?.Trim();
            var movement = csv.GetField("Movimento")?.Trim();
            var amountRaw = csv.GetField("Importo")?.Trim();
            var notes = csv.GetField("Osservazioni")?.Trim();

            // Ignore fully-empty rows that can appear at the end of exported files.
            if (string.IsNullOrWhiteSpace(valueDateRaw)
                && string.IsNullOrWhiteSpace(dateRaw)
                && string.IsNullOrWhiteSpace(keyword)
                && string.IsNullOrWhiteSpace(movement)
                && string.IsNullOrWhiteSpace(amountRaw)
                && string.IsNullOrWhiteSpace(notes))
            {
                continue;
            }

            rows.Add(new BbvaRow
            {
                ValueDate = ParseDate(valueDateRaw),
                Date = ParseDate(dateRaw),
                Keyword = keyword,
                Movement = movement,
                Amount = ParseAmount(amountRaw),
                Notes = notes,
                RawLine = string.Join(";", [valueDateRaw, dateRaw, keyword, movement, amountRaw, notes]),
            });
        }

        return rows;
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

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParseExact(
            value,
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseAmount(string? value)
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

    private int DetermineCategoryId(string description, List<Category> categories, int defaultCategoryId)
    {
        foreach (var category in categories)
        {
            var tags = SplitTags(category.Tags).ToArray();
            if (tags.Length > 0 && description.ContainsOne(ignoreCase: true, tags))
                return category.Id;
        }

        return defaultCategoryId;
    }

    private static IEnumerable<string> SplitTags(string? tags)
        => string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(CategoryTagSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string BuildDescription(BbvaRow row)
    {
        var parts = new[] { row.Keyword, row.Movement, row.Notes }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());

        var description = string.Join(" - ", parts);
        if (string.IsNullOrWhiteSpace(description))
            description = "Operazione BBVA";

        return description.Length > TransactionDescriptionMaxLength
            ? description[..TransactionDescriptionMaxLength]
            : description;
    }

    private static string BuildExternalId(BbvaRow row)
    {
        var key = string.Join("|", [
            (row.Date ?? row.ValueDate)?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            row.Amount?.ToString("0.00################", CultureInfo.InvariantCulture) ?? string.Empty,
            NormalizeForFingerprint(row.Keyword),
            NormalizeForFingerprint(row.Movement),
            NormalizeForFingerprint(row.Notes),
        ]);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"BBVA:v1:{Convert.ToHexString(hash)}";
    }

    private static string NormalizeForFingerprint(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(" ", value
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();

    private sealed class BbvaRow
    {
        public DateTime? ValueDate { get; set; }
        public DateTime? Date { get; set; }
        public string? Keyword { get; set; }
        public string? Movement { get; set; }
        public decimal? Amount { get; set; }
        public string? Notes { get; set; }
        public string RawLine { get; set; } = string.Empty;
    }
}