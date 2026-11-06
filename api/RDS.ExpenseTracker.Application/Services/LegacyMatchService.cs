using DocumentFormat.OpenXml.Packaging;
using FluentResults;
using RDS.ExpenseTracker.Application.Services.LegacyExcel;
using RDS.ExpenseTracker.Application.Utilities;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
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

        // solo i movimenti dello stesso mese del foglio
        var monthStart = new DateTime(sheet.Info.Year, sheet.Info.Month, 1);
        var transactions = await _transactionRepository.GetTransactionsInRange(
            userId,
            monthStart,
            monthStart.AddMonths(1).AddDays(-1));

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
        foreach (var row in rows)
            row.ToCreate = LegacyMatchSuggester.IsManualAccount(row.AccountName, accounts.FirstOrDefault(a => a.Id == row.AccountId));

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
            // le righe da creare non vanno abbinate a movimenti esistenti
            Suggestions = LegacyMatchSuggester.Suggest(rows.Where(r => !r.ToCreate), dbTransactions),
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

        var categoryCheck = await EnsureCategoriesBelongToUser(items.Select(i => i.CategoryId), userId);
        if (categoryCheck.IsFailed)
            return Result.Fail(categoryCheck.Errors);

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

    public async Task<Result<int>> CreateAsync(LegacyCreateRequest request, int userId)
    {
        var admin = await _userService.EnsureAdminAsync(userId);
        if (admin.IsFailed)
            return Result.Fail(admin.Errors);

        var items = request?.Items ?? [];
        if (items.Count == 0)
            return Result.Fail(DomainErrors.BadRequest("No transaction to create."));

        foreach (var item in items)
        {
            var description = item.Description?.Trim() ?? string.Empty;
            if (description.Length == 0 || description.Length > MaxDescriptionLength)
                return Result.Fail(DomainErrors.BadRequest(
                    $"Every description must be between 1 and {MaxDescriptionLength} characters."));
            if (item.Amount == 0m)
                return Result.Fail(DomainErrors.BadRequest("The amount cannot be zero."));
            if (item.Date == default)
                return Result.Fail(DomainErrors.Required("date"));
        }

        var ownAccountIds = (await _accountRepository.GetAccounts(userId)).Select(a => a.Id).ToHashSet();
        var foreignAccount = items.Select(i => i.AccountId).FirstOrDefault(id => !ownAccountIds.Contains(id), -1);
        if (foreignAccount != -1)
            return Result.Fail(DomainErrors.NotFound("Account", foreignAccount));

        var categoryCheck = await EnsureCategoriesBelongToUser(items.Select(i => i.CategoryId), userId);
        if (categoryCheck.IsFailed)
            return Result.Fail(categoryCheck.Errors);

        var now = DateTime.UtcNow;
        var transactions = items.Select(i => new Transaction
        {
            AccountId = i.AccountId,
            Date = i.Date.Date,
            Amount = Math.Round(i.Amount, 2, MidpointRounding.AwayFromZero),
            Description = i.Description.Trim(),
            CategoryId = i.CategoryId,
            CreatedOn = now,
            LegacyMatchedAt = now,
        }).ToList();

        await _transactionRepository.AddTransactions(transactions);
        await _transactionRepository.SaveChangesAsync();
        return Result.Ok(transactions.Count);
    }

    private async Task<Result> EnsureCategoriesBelongToUser(IEnumerable<int?> categoryIds, int userId)
    {
        var ids = categoryIds.Where(id => id is not null).Select(id => id!.Value).ToHashSet();
        if (ids.Count == 0)
            return Result.Ok();

        var ownCategoryIds = (await _categoryRepository.GetCategories(userId)).Select(c => c.Id).ToHashSet();
        var foreign = ids.FirstOrDefault(id => !ownCategoryIds.Contains(id));
        return foreign == 0 ? Result.Ok() : Result.Fail(DomainErrors.NotFound("Category", foreign));
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
