using ExcelDataReader;
using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Application.Extensions;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Data;
using System.Text;

namespace RDS.ExpenseTracker.Application.Services;

public class ExcelImportService : IExcelImportService
{
    private static readonly char[] CategoryTagSeparators = [',', ';'];

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IExpenseExcelFileOptions _excelOptions;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository,
        IExpenseExcelFileOptions excelOptions,
        ILogger<ExcelImportService> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository = transferRepository ?? throw new ArgumentNullException(nameof(transferRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _excelOptions = excelOptions ?? throw new ArgumentNullException(nameof(excelOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<int>> ImportFromExcelAsync(Stream fileStream, string fileName, bool importAll = false)
    {
        try
        {
            if (fileStream == null || !fileStream.CanRead)
                return Result.Fail("File stream is null or unreadable");

            _logger.LogInformation("Starting Excel import. ImportAll={importAll}, FileName={fileName}", importAll, fileName);

            var dataTables = ReadExcelFile(fileStream);
            var transactionsBySheet = ExtractTransactionsFromSheets(dataTables);

            if (!importAll)
            {
                transactionsBySheet = await FilterSheetsByLatestTransactionAsync(transactionsBySheet);
            }

            var importTransactions = await EnrichTransactionsAsync(transactionsBySheet);
            var saveResult = await SaveTransactionsAsync(importTransactions);
            if (saveResult.IsFailed)
            {
                var persistedCount = importTransactions.Transactions.Count + (importTransactions.Transfers.Count * 2);
                return Result.Fail(new Error($"Failed to save {persistedCount} transactions").CausedBy(saveResult.Errors));
            }

            return Result.Ok(importTransactions.Transactions.Count + (importTransactions.Transfers.Count * 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Excel import");
            return Result.Fail(new Error($"Import failed: {ex.Message}").CausedBy(ex));
        }
    }

    public async Task<Result<int>> ImportFromExcelBase64Async(string base64Content, string fileName, bool importAll = false)
    {
        if (string.IsNullOrWhiteSpace(base64Content))
        {
            return Result.Fail("Base64 content is null or empty");
        }

        try
        {
            var normalizedBase64 = NormalizeBase64Payload(base64Content);
            var fileBytes = Convert.FromBase64String(normalizedBase64);

            using var stream = new MemoryStream(fileBytes);
            return await ImportFromExcelAsync(stream, fileName, importAll);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid base64 file payload provided for import");
            return Result.Fail(new Error("Invalid base64 file content").CausedBy(ex));
        }
    }

    private static string NormalizeBase64Payload(string base64Content)
    {
        var trimmed = base64Content.Trim();
        var dataPrefixIndex = trimmed.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);

        if (dataPrefixIndex >= 0)
        {
            return trimmed[(dataPrefixIndex + "base64,".Length)..];
        }

        return trimmed;
    }

    private List<DataTable> ReadExcelFile(Stream fileStream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        var dataSet = reader.AsDataSet();

        return dataSet.Tables.Cast<DataTable>()
            .Where(dt => !dt.TableName.ToLower().ContainsOne(_excelOptions.SheetsToIgnore.ToArray()))
            .ToList();
    }

    private ExcelDataRowModel GetDataRowModel(DataRow dataRow)
    {
        var transactionOutflow = dataRow[_excelOptions.TransactionOutflowIndex].ParseToDecimal() ?? 0;
        var transactionInflow = dataRow[_excelOptions.TransactionInflowIndex].ParseToDecimal() ?? 0;

        return new ExcelDataRowModel
        {
            TransactionDate = dataRow[_excelOptions.TransactionDateIndex].ParseToDateTime(),
            TransactionDescription = dataRow[_excelOptions.TransactionDescriptionIndex]?.ToString() ?? string.Empty,
            TransactionAmount = transactionOutflow > 0 ? transactionOutflow * -1 : transactionInflow > 0 ? transactionInflow : 0,
            TransactionAccountName = dataRow[_excelOptions.TransactionAccountNameIndex]?.ToString() ?? string.Empty,
            TransferDate = dataRow[_excelOptions.TransferDateIndex].ParseToDateTime(),
            TransferDescription = dataRow[_excelOptions.TransferDescriptionIndex]?.ToString() ?? string.Empty,
            TransferAmount = dataRow[_excelOptions.TransferAmountIndex].ParseToDecimal() ?? 0,
            TransferAccountFrom = dataRow[_excelOptions.TransferAccountFromIndex]?.ToString() ?? string.Empty,
            TransferAccountTo = dataRow[_excelOptions.TransferAccountToIndex]?.ToString() ?? string.Empty
        };
    }

    private static ImportTransactionModel ExtractStandardTransaction(ExcelDataRowModel model)
        => new()
        {
            Amount = model.TransactionAmount,
            Date = model.TransactionDate,
            Description = model.TransactionDescription,
            Account = model.TransactionAccountName
        };

    private static ImportTransferModel ExtractTransfer(ExcelDataRowModel model)
        => new()
        {
            Amount = Math.Abs(model.TransferAmount),
            Date = model.TransferDate,
            Description = model.TransferDescription,
            FromAccount = model.TransferAccountFrom,
            ToAccount = model.TransferAccountTo
        };

    private List<TransactionsBySheetModel> ExtractTransactionsFromSheets(List<DataTable> dataTables)
    {
        var result = new List<TransactionsBySheetModel>();

        Parallel.ForEach(dataTables, dataTable =>
        {
            var transactions = new List<ImportTransactionModel>();
            var transfers = new List<ImportTransferModel>();
            var dataRows = dataTable.Rows.Cast<DataRow>();

            foreach (var row in dataRows)
            {
                var rowModel = GetDataRowModel(row);

                if (rowModel.TransactionAmount != 0 && !string.IsNullOrWhiteSpace(rowModel.TransactionAccountName))
                {
                    transactions.Add(ExtractStandardTransaction(rowModel));
                }

                if (rowModel.TransferAmount != 0 && !string.IsNullOrWhiteSpace(rowModel.TransferDescription))
                {
                    transfers.Add(ExtractTransfer(rowModel));
                }
            }

            lock (result)
            {
                var sheetDate = dataTable.TableName.ParseDateFromSheetName();
                result.Add(new TransactionsBySheetModel(dataTable.TableName, sheetDate, transactions, transfers));
            }
        });

        return result;
    }

    private async Task<List<TransactionsBySheetModel>> FilterSheetsByLatestTransactionAsync(List<TransactionsBySheetModel> transactionsBySheet)
    {
        var latestTransaction = await _transactionRepository.GetLatestTransaction();

        if (latestTransaction.Date == null)
            return transactionsBySheet;

        var latestTransactionDate = latestTransaction.Date.Value;
        var fromDate = latestTransactionDate.AddDays(-1);
        var toDate = latestTransactionDate.AddDays(1);

        var existingTransactions = (await _transactionRepository.GetTransactions())
            .Where(transaction => transaction.Date.HasValue
                && transaction.Date.Value >= fromDate
                && transaction.Date.Value <= toDate)
            .ToList();

        foreach (var sheetTransactions in transactionsBySheet)
        {
            if (sheetTransactions.SheetDate.Month != latestTransactionDate.Month ||
                sheetTransactions.SheetDate.Year != latestTransactionDate.Year)
                continue;

            var newTransactions = new List<ImportTransactionModel>();
            var newTransfers = new List<ImportTransferModel>();

            foreach (var transaction in sheetTransactions.Transactions)
            {
                var transactionDate = transaction.Date ?? sheetTransactions.SheetDate;

                if (transactionDate > latestTransactionDate)
                {
                    newTransactions.Add(transaction);
                    continue;
                }

                if (transactionDate == latestTransactionDate)
                {
                    var isDuplicate = existingTransactions.Any(x =>
                        x.Description == transaction.Description &&
                        x.AccountNavigation.Name == transaction.Account &&
                        x.Amount == transaction.Amount);

                    if (!isDuplicate)
                    {
                        newTransactions.Add(transaction);
                    }
                }
            }

            foreach (var transfer in sheetTransactions.Transfers)
            {
                var transferDate = transfer.Date ?? sheetTransactions.SheetDate;

                if (transferDate > latestTransactionDate)
                {
                    newTransfers.Add(transfer);
                    continue;
                }

                if (transferDate == latestTransactionDate)
                {
                    var outgoingExists = existingTransactions.Any(x =>
                        x.Description == transfer.Description &&
                        x.AccountNavigation.Name == transfer.FromAccount &&
                        x.Amount == -Math.Abs(transfer.Amount));

                    var ingoingExists = existingTransactions.Any(x =>
                        x.Description == transfer.Description &&
                        x.AccountNavigation.Name == transfer.ToAccount &&
                        x.Amount == Math.Abs(transfer.Amount));

                    if (!(outgoingExists && ingoingExists))
                    {
                        newTransfers.Add(transfer);
                    }
                }
            }

            sheetTransactions.Transactions = newTransactions;
            sheetTransactions.Transfers = newTransfers;
        }

        return transactionsBySheet;
    }

    private async Task<EnrichedImportBatch> EnrichTransactionsAsync(List<TransactionsBySheetModel> transactionsBySheet)
    {
        var categories = (await _categoryRepository.GetCategories()).ToList();
        var defaultCategory = await _categoryRepository.GetDefaultCategory();

        var accountNames = transactionsBySheet
            .SelectMany(x => x.Transactions.Select(transaction => transaction.Account)
                .Concat(x.Transfers.Select(transfer => transfer.FromAccount))
                .Concat(x.Transfers.Select(transfer => transfer.ToAccount)))
            .Distinct()
            .ToList();

        var accounts = (await _accountRepository.GetAccounts()).ToList();

        var missingNames = accountNames
            .Where(name => !accounts.Any(a => a.Name != null && a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingNames.Any())
        {
            var newAccounts = missingNames
                .Select(name => new Account(0, name))
                .ToList();
            await _accountRepository.AddAccounts(newAccounts);
            await _accountRepository.SaveChangesAsync();

            accounts = (await _accountRepository.GetAccounts()).ToList();
        }

        categories = categories.OrderBy(c => c.Priority ?? int.MaxValue).ToList();
        var transactions = new List<ImportTransactionModel>();
        var transfers = new List<ImportTransferModel>();

        foreach (var sheet in transactionsBySheet)
        {
            foreach (var transaction in sheet.Transactions)
            {
                foreach (var category in categories)
                {
                    var tags = SplitCategoryTags(category.Tags).ToArray();

                    if (transaction.Description.ContainsOne(ignoreCase: true, tags))
                    {
                        transaction.CategoryId = category.Id;
                        break;
                    }
                }

                if (transaction.CategoryId == 0 && defaultCategory != null)
                {
                    transaction.CategoryId = defaultCategory.Id;
                }

                if (transaction.AccountId <= 0)
                {
                    var account = accounts.FirstOrDefault(a =>
                        a.Name != null && a.Name.Equals(transaction.Account, StringComparison.OrdinalIgnoreCase));
                    if (account != null)
                    {
                        transaction.AccountId = account.Id;
                    }
                }

                if (transaction.Date == null)
                {
                    transaction.Date = sheet.SheetDate;
                }
                else
                {
                    transaction.Date = new DateTime(
                        sheet.SheetDate.Year,
                        transaction.Date.Value.Month,
                        transaction.Date.Value.Day);
                }

                transactions.Add(transaction);
            }

            foreach (var transfer in sheet.Transfers)
            {
                var fromAccount = accounts.FirstOrDefault(a =>
                    a.Name != null && a.Name.Equals(transfer.FromAccount, StringComparison.OrdinalIgnoreCase));
                if (fromAccount != null)
                {
                    transfer.FromAccountId = fromAccount.Id;
                }

                var toAccount = accounts.FirstOrDefault(a =>
                    a.Name != null && a.Name.Equals(transfer.ToAccount, StringComparison.OrdinalIgnoreCase));
                if (toAccount != null)
                {
                    transfer.ToAccountId = toAccount.Id;
                }

                if (transfer.Date == null)
                {
                    transfer.Date = sheet.SheetDate;
                }
                else
                {
                    transfer.Date = new DateTime(
                        sheet.SheetDate.Year,
                        transfer.Date.Value.Month,
                        transfer.Date.Value.Day);
                }

                transfers.Add(transfer);
            }
        }

        return new EnrichedImportBatch(transactions, transfers);
    }

    private async Task<Result> SaveTransactionsAsync(EnrichedImportBatch importTransactions)
    {
        if (!importTransactions.Transactions.Any() && !importTransactions.Transfers.Any())
            return Result.Ok();

        if (importTransactions.Transfers.Any())
        {
            var transferEntities = new List<Transfer>(importTransactions.Transfers.Count);
            var transferTransactions = new List<Transaction>(importTransactions.Transfers.Count * 2);

            foreach (var transfer in importTransactions.Transfers)
            {
                if (transfer.FromAccountId <= 0 || transfer.ToAccountId <= 0)
                {
                    return Result.Fail("Transfer account mapping failed during import.");
                }

                if (transfer.Amount <= 0)
                {
                    return Result.Fail("Transfer amount must be greater than zero.");
                }

                if (transfer.FromAccountId == transfer.ToAccountId)
                {
                    return Result.Fail("Source and destination accounts must be different for transfers.");
                }

                var transferEntity = new Transfer
                {
                    CreatedOn = DateTime.UtcNow
                };

                var transferDate = transfer.Date ?? DateTime.UtcNow;

                transferEntities.Add(transferEntity);

                transferTransactions.AddRange([
                    new Transaction
                    {
                        AccountId = transfer.FromAccountId,
                        CategoryId = (int)CategoryEnum.MoneyTransfers,
                        Amount = -Math.Abs(transfer.Amount),
                        Description = transfer.Description,
                        Date = transferDate,
                        CreatedOn = DateTime.UtcNow,
                        TransferNavigation = transferEntity
                    },
                    new Transaction
                    {
                        AccountId = transfer.ToAccountId,
                        CategoryId = (int)CategoryEnum.MoneyTransfers,
                        Amount = Math.Abs(transfer.Amount),
                        Description = transfer.Description,
                        Date = transferDate,
                        CreatedOn = DateTime.UtcNow,
                        TransferNavigation = transferEntity
                    }
                ]);
            }

            foreach (var transferEntity in transferEntities)
            {
                await _transferRepository.AddTransfer(transferEntity);
            }

            await _transactionRepository.AddTransactions(transferTransactions);

            await _transferRepository.SaveChangesAsync();
        }

        if (!importTransactions.Transactions.Any())
        {
            return Result.Ok();
        }

        if (importTransactions.Transactions.Any(transaction => transaction.AccountId <= 0))
        {
            return Result.Fail("Transaction account mapping failed during import.");
        }

        var transactionsToSave = importTransactions.Transactions.Select(importTx => new Transaction
        {
            Amount = importTx.Amount,
            Date = importTx.Date,
            Description = importTx.Description,
            AccountId = importTx.AccountId,
            CategoryId = importTx.CategoryId,
            CreatedOn = DateTime.UtcNow
        }).ToList();

        await _transactionRepository.AddTransactions(transactionsToSave);
        await _transactionRepository.SaveChangesAsync();

        return Result.Ok();
    }

    private static IEnumerable<string> SplitCategoryTags(string? tags)
        => string.IsNullOrWhiteSpace(tags)
            ? Enumerable.Empty<string>()
            : tags.Split(CategoryTagSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed record EnrichedImportBatch(
        List<ImportTransactionModel> Transactions,
        List<ImportTransferModel> Transfers);
}
