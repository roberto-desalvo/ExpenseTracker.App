using ExcelDataReader;
using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Application.Extensions;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Services;
using System.Data;
using System.Text;

namespace RDS.ExpenseTracker.Application.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly ITransactionService _transactionService;
    private readonly ITransferService _transferService;
    private readonly ICategoryService _categoryService;
    private readonly IAccountService _accountService;
    private readonly IExpenseExcelFileOptions _excelOptions;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(
        ITransactionService transactionService,
        ITransferService transferService,
        ICategoryService categoryService,
        IAccountService accountService,
        IExpenseExcelFileOptions excelOptions,
        ILogger<ExcelImportService> logger)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
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
        var latestResult = await _transactionService.GetLatestTransaction();
        var latestTransaction = latestResult.ValueOrDefault;

        if (latestTransaction?.Date == null)
            return transactionsBySheet;

        var latestTransactionDate = latestTransaction.Date.Value;

        foreach (var sheetTransactions in transactionsBySheet)
        {
            if (sheetTransactions.SheetDate.Month != latestTransactionDate.Month ||
                sheetTransactions.SheetDate.Year != latestTransactionDate.Year)
                continue;

            var filter = new TransactionQueryRequest
            {
                FromDate = latestTransactionDate.AddDays(-1),
                ToDate = latestTransactionDate.AddDays(1)
            };
            var transactionsResult = await _transactionService.GetTransactions(filter);
            var existingTransactions = (transactionsResult.ValueOrDefault ?? Enumerable.Empty<TransactionDto>()).ToList();

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
                        x.Account == transaction.Account &&
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
                        x.Account == transfer.FromAccount &&
                        x.Amount == -Math.Abs(transfer.Amount));

                    var ingoingExists = existingTransactions.Any(x =>
                        x.Description == transfer.Description &&
                        x.Account == transfer.ToAccount &&
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
        var categoriesResult = await _categoryService.GetCategories();
        var categories = (categoriesResult.ValueOrDefault ?? Enumerable.Empty<CategoryDto>()).ToList();

        var defaultCategoryResult = await _categoryService.GetDefaultCategory();
        var defaultCategory = defaultCategoryResult.ValueOrDefault;

        var accountNames = transactionsBySheet
            .SelectMany(x => x.Transactions.Select(transaction => transaction.Account)
                .Concat(x.Transfers.Select(transfer => transfer.FromAccount))
                .Concat(x.Transfers.Select(transfer => transfer.ToAccount)))
            .Distinct()
            .ToList();

        var accountsResult = await _accountService.GetAccounts();
        var accounts = (accountsResult.ValueOrDefault ?? Enumerable.Empty<FinancialAccountDto>()).ToList();

        var missingNames = accountNames
            .Where(name => !accounts.Any(a => a.Name != null && a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingNames.Any())
        {
            var newAccounts = missingNames
                .Select(name => new FinancialAccountDto { Name = name, Description = name })
                .ToList();
            await _accountService.AddAccounts(newAccounts);

            var refreshedResult = await _accountService.GetAccounts();
            accounts = (refreshedResult.ValueOrDefault ?? Enumerable.Empty<FinancialAccountDto>()).ToList();
        }

        categories = categories.OrderBy(c => c.Priority).ToList();
        var transactions = new List<ImportTransactionModel>();
        var transfers = new List<ImportTransferModel>();

        foreach (var sheet in transactionsBySheet)
        {
            foreach (var transaction in sheet.Transactions)
            {
                foreach (var category in categories)
                {
                    var tags = category.Tags
                        .Select(tag => tag.Trim())
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .ToArray();

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
            foreach (var transfer in importTransactions.Transfers)
            {
                var transferResult = await _transferService.AddTransfer(new TransferDto
                {
                    FromAccountId = transfer.FromAccountId,
                    ToAccountId = transfer.ToAccountId,
                    Amount = transfer.Amount,
                    Description = transfer.Description,
                    Date = transfer.Date
                });

                if (transferResult.IsFailed)
                {
                    return Result.Fail(transferResult.Errors);
                }
            }
        }

        if (!importTransactions.Transactions.Any())
        {
            return Result.Ok();
        }

        var transactionsToSave = importTransactions.Transactions.Select(importTx => new TransactionDto
        {
            Amount = importTx.Amount,
            Date = importTx.Date,
            Description = importTx.Description,
            AccountId = importTx.AccountId,
            CategoryId = importTx.CategoryId
        }).ToList();

        var saveResult = await _transactionService.AddTransactions(transactionsToSave);
        return saveResult;
    }

    private sealed record EnrichedImportBatch(
        List<ImportTransactionModel> Transactions,
        List<ImportTransferModel> Transfers);
}
