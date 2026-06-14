using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Text.RegularExpressions;

namespace RDS.ExpenseTracker.Application.Services;

public class SellaPdfImportService : ISellaPdfImportService
{
    private const int TransactionDescriptionMaxLength = 500;

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ISellaPdfOptions _options;
    private readonly SellaPdfTextParser _parser;
    private readonly ILogger<SellaPdfImportService> _logger;

    public SellaPdfImportService(
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        ISellaPdfOptions options,
        ILogger<SellaPdfImportService> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository = transferRepository ?? throw new ArgumentNullException(nameof(transferRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = new SellaPdfTextParser();
    }

    public async Task<Result<int>> ImportFromPdfAsync(Stream fileStream, string fileName, bool importAll = false)
    {
        try
        {
            if (fileStream == null || !fileStream.CanRead)
                return Result.Fail("File stream is null or unreadable");

            var rows = _parser.Parse(fileStream);
            if (rows.Count == 0)
            {
                _logger.LogWarning("No transaction rows parsed from Sella PDF file {FileName}", fileName);
                return Result.Fail("No transaction rows found in PDF. Check that the file is a text-based PDF statement and not a scanned image.");
            }

            var missingIdentifiers = rows.Where(r => string.IsNullOrWhiteSpace(r.Identifier)).ToList();
            if (missingIdentifiers.Count > 0)
            {
                return Result.Fail(
                    $"Import failed: {missingIdentifiers.Count} rows have no CodiceIdentificativo. The identifier is mandatory for all rows.");
            }

            if (!importAll)
            {
                var externalIds = rows.Select(r => r.Identifier!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existingIds = (await _transactionRepository.GetExistingExternalIds(externalIds))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                rows = rows
                    .Where(r => !existingIds.Contains(r.Identifier!))
                    .ToList();
            }

            if (rows.Count == 0)
                return Result.Ok(0);

            var accountNames = GetRequiredAccountNames(rows);
            var accounts = await EnsureAccountsExistAsync(accountNames);
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
                    var existingByExternalId = await _transferRepository.GetTransferByExternalId(row.Identifier!);
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
                        row.Date);

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
                        Date = row.Date,
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
                        Date = row.Date,
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
                    Date = row.Date,
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

            var imported = transactions.Count + transferTransactions.Count;

            _logger.LogInformation(
                "Sella PDF import completed. Imported {Imported} records ({Transfers} transfer legs, {Transactions} regular)",
                imported,
                transferTransactions.Count,
                transactions.Count);

            return Result.Ok(imported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Sella PDF import");
            return Result.Fail($"Import failed: {ex.Message}");
        }
    }

    private IEnumerable<string> GetRequiredAccountNames(IEnumerable<SellaPdfTextParser.SellaPdfRow> rows)
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

    private bool TryResolveCounterpartyAccount(
        SellaPdfTextParser.SellaPdfRow row,
        List<Account> accounts,
        out Account? account)
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

    private static string BuildSafeDescription(string? rawDescription, string? identifier)
    {
        var normalized = Regex.Replace(rawDescription ?? string.Empty, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            normalized = string.IsNullOrWhiteSpace(identifier)
                ? "Operazione Sella"
                : $"Operazione Sella {identifier}";

        if (normalized.Length > TransactionDescriptionMaxLength)
            normalized = normalized[..TransactionDescriptionMaxLength];

        return normalized;
    }
}
