using DocumentFormat.OpenXml.Packaging;
using FluentResults;
using RDS.ExpenseTracker.Application.Services.LegacyExcel;
using RDS.ExpenseTracker.Application.Utilities;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

/// <summary>
/// Abbinamento dello storico Excel (Spese.xlsx) ai movimenti a DB, un mese (foglio) alla volta.
/// Il file non viene salvato: il client lo rimanda a ogni richiesta. Riservato agli admin.
/// </summary>
public class LegacyMatchService : ILegacyMatchService
{
    // Le date bancarie possono slittare rispetto a quelle annotate a mano sul file.
    public const int MonthMarginDays = 7;
    private const int MaxDescriptionLength = 500;

    private readonly IUserService _userService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;

    public LegacyMatchService(
        IUserService userService,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public async Task<Result<IReadOnlyList<LegacySheetDto>>> GetSheetsAsync(Stream file, int userId)
    {
        var admin = await _userService.EnsureAdminAsync(userId);
        if (admin.IsFailed)
            return Result.Fail(admin.Errors);

        var sheets = await ReadWorkbook(file, LegacySpreadsheetReader.ListSheets);
        if (sheets.IsFailed)
            return Result.Fail(sheets.Errors);

        if (sheets.Value.Count == 0)
            return Result.Fail(DomainErrors.BadRequest("The file contains no monthly sheet (e.g. 'Gennaio2026')."));

        return Result.Ok<IReadOnlyList<LegacySheetDto>>(
            [.. sheets.Value.Select(s => new LegacySheetDto { Name = s.Name, Year = s.Year, Month = s.Month })]);
    }

    public async Task<Result<LegacyMonthDto>> GetMonthAsync(Stream file, string sheetName, int userId)
    {
        var admin = await _userService.EnsureAdminAsync(userId);
        if (admin.IsFailed)
            return Result.Fail(admin.Errors);

        if (string.IsNullOrWhiteSpace(sheetName))
            return Result.Fail(DomainErrors.Required("sheet"));

        var read = await ReadWorkbook(file, stream => LegacySpreadsheetReader.ReadSheet(stream, sheetName));
        if (read.IsFailed)
            return Result.Fail(read.Errors);

        var sheet = read.Value;
        if (sheet is null)
            return Result.Fail(DomainErrors.NotFound($"Sheet '{sheetName}'"));

        var accounts = (await _accountRepository.GetAccounts(userId)).ToList();
        var categories = (await _categoryRepository.GetCategories(userId)).ToList();

        var monthStart = new DateTime(sheet.Info.Year, sheet.Info.Month, 1);
        var transactions = await _transactionRepository.GetTransactionsInRange(
            userId,
            monthStart.AddDays(-MonthMarginDays),
            monthStart.AddMonths(1).AddDays(-1 + MonthMarginDays));

        var rows = sheet.Rows.Select(r => new LegacyExcelRowDto
        {
            RowNumber = r.RowNumber,
            Date = r.Date,
            Description = r.Description,
            Amount = r.Amount,
            AccountName = r.AccountName,
            AccountId = LegacyMatchSuggester.ResolveAccount(r.AccountName, accounts),
            ExcelCategory = r.Category,
            SuggestedCategoryId = LegacyMatchSuggester.ResolveCategory(r.Category, categories),
        }).ToList();

        var dbTransactions = transactions.Select(t => new LegacyDbTransactionDto
        {
            Id = t.Id,
            Date = t.Date,
            Description = t.Description,
            Amount = t.Amount,
            AccountId = t.AccountId,
            Account = t.AccountNavigation?.Name ?? string.Empty,
            CategoryId = t.CategoryId,
            Category = t.CategoryNavigation?.Name,
            IsTransfer = t.TransferId is not null || t.CategoryNavigation?.Kind == CategoryEnum.MoneyTransfers,
            LegacyMatchedAt = t.LegacyMatchedAt,
        }).ToList();

        return Result.Ok(new LegacyMonthDto
        {
            Sheet = new LegacySheetDto { Name = sheet.Info.Name, Year = sheet.Info.Year, Month = sheet.Info.Month },
            ExcelRows = rows,
            DbTransactions = dbTransactions,
            Suggestions = LegacyMatchSuggester.Suggest(rows, dbTransactions),
        });
    }

    public async Task<Result<int>> ApplyAsync(LegacyMatchApplyRequest request, int userId)
    {
        var admin = await _userService.EnsureAdminAsync(userId);
        if (admin.IsFailed)
            return Result.Fail(admin.Errors);

        var items = request?.Items ?? [];
        if (items.Count == 0)
            return Result.Fail(DomainErrors.BadRequest("No match to save."));

        if (items.Select(i => i.TransactionId).Distinct().Count() != items.Count)
            return Result.Fail(DomainErrors.BadRequest("Each transaction can be matched only once."));

        foreach (var item in items)
        {
            if (item.TransactionId <= 0)
                return Result.Fail(DomainErrors.InvalidId("transaction", item.TransactionId));

            var description = item.Description?.Trim() ?? string.Empty;
            if (description.Length == 0 || description.Length > MaxDescriptionLength)
                return Result.Fail(DomainErrors.BadRequest(
                    $"The description of transaction {item.TransactionId} must be between 1 and {MaxDescriptionLength} characters."));
        }

        var categoryIds = items.Where(i => i.CategoryId is not null).Select(i => i.CategoryId!.Value).ToHashSet();
        if (categoryIds.Count > 0)
        {
            var ownCategoryIds = (await _categoryRepository.GetCategories(userId)).Select(c => c.Id).ToHashSet();
            var foreign = categoryIds.FirstOrDefault(id => !ownCategoryIds.Contains(id));
            if (foreign != 0)
                return Result.Fail(DomainErrors.NotFound("Category", foreign));
        }

        var transactions = (await _transactionRepository.GetTransactionsByIds(items.Select(i => i.TransactionId), userId))
            .ToDictionary(t => t.Id);

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            if (!transactions.TryGetValue(item.TransactionId, out var transaction))
                return Result.Fail(DomainErrors.NotFound("Transaction", item.TransactionId));

            transaction.Description = item.Description.Trim();
            transaction.CategoryId = item.CategoryId;
            transaction.LegacyMatchedAt = now;
            transaction.UpdatedOn = now;
        }

        await _transactionRepository.SaveChangesAsync();
        return Result.Ok(items.Count);
    }

    // SpreadsheetDocument richiede uno stream con seek: il file viene copiato in memoria (~400 KB).
    private static async Task<Result<T>> ReadWorkbook<T>(Stream file, Func<Stream, T> read)
    {
        if (file is null)
            return Result.Fail(DomainErrors.Required("file"));

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        buffer.Position = 0;

        try
        {
            return Result.Ok(read(buffer));
        }
        catch (Exception ex) when (ex is OpenXmlPackageException or InvalidDataException or FileFormatException or InvalidOperationException)
        {
            return Result.Fail(DomainErrors.BadRequest("The file is not a valid .xlsx workbook."));
        }
    }
}
