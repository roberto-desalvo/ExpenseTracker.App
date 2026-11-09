using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly TimeProvider _timeProvider;

    public BudgetService(IBudgetRepository repository, ICategoryRepository categoryRepository, TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime Today => _timeProvider.GetLocalNow().Date;

    private static DateTime StartOfMonth(DateTime date) => new(date.Year, date.Month, 1);

    // I trasferimenti non sono spesa: la loro categoria non si pianifica.
    private async Task<List<Category>> GetPlannableCategories(int userId)
        => (await _categoryRepository.GetCategories(userId))
            .Where(c => c.Kind != CategoryEnum.MoneyTransfers)
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public async Task<Result<BudgetMonthDto>> GetMonth(DateTime month, int userId)
    {
        var monthStart = StartOfMonth(month);
        var currentMonth = StartOfMonth(Today);
        var isEditable = monthStart >= currentMonth;
        var hasActuals = monthStart <= currentMonth;

        var categories = await GetPlannableCategories(userId);
        var planned = (await _repository.GetBudgets(monthStart, userId))
            .ToDictionary(b => b.CategoryId, b => b.Amount);

        Dictionary<int, decimal> spentByCategory = [];
        var uncategorizedSpent = 0m;
        if (hasActuals)
        {
            // mese corrente: speso fino a oggi; mesi passati: tutto il mese
            var endDate = monthStart == currentMonth ? Today.AddDays(1).AddTicks(-1) : monthStart.AddMonths(1).AddTicks(-1);
            foreach (var (categoryId, spent) in await _repository.GetSpentByCategory(monthStart, endDate, userId))
            {
                if (categoryId is int id)
                    spentByCategory[id] = spent;
                else
                    uncategorizedSpent += spent;
            }
        }

        var lines = categories
            .Select(c => new BudgetLineDto
            {
                CategoryId = c.Id,
                Name = c.Name,
                Planned = planned.TryGetValue(c.Id, out var amount) ? amount : null,
                Spent = hasActuals ? spentByCategory.GetValueOrDefault(c.Id) : null,
            })
            .ToList();

        var dto = new BudgetMonthDto
        {
            Month = monthStart,
            IsEditable = isEditable,
            HasActuals = hasActuals,
            TotalPlanned = lines.Sum(l => l.Planned ?? 0m),
            Lines = lines,
        };

        if (hasActuals)
        {
            dto.TotalSpent = lines.Sum(l => l.Spent ?? 0m) + uncategorizedSpent;
            dto.UnplannedSpent = lines.Where(l => l.Planned is null).Sum(l => l.Spent ?? 0m) + uncategorizedSpent;
        }

        return Result.Ok(dto);
    }

    public async Task<Result<List<BudgetTotalDto>>> GetTotals(DateTime startDate, DateTime endDate, int userId)
    {
        if (startDate > endDate)
            return Result.Fail(DomainErrors.BadRequest("EndDate must be greater than StartDate"));

        var totals = await _repository.GetPlannedTotals(StartOfMonth(startDate), StartOfMonth(endDate), userId);
        return Result.Ok(totals
            .Select(t => new BudgetTotalDto { Month = t.Month, Planned = t.Planned })
            .ToList());
    }

    public async Task<Result> SaveMonth(BudgetMonthUpdateRequest request, int userId)
    {
        if (request is null)
            return Result.Fail(DomainErrors.Required("request"));

        var monthStart = StartOfMonth(request.Month);
        if (monthStart < StartOfMonth(Today))
            return Result.Fail(DomainErrors.BadRequest("Previsioni dei mesi passati non modificabili."));

        var lines = request.Lines ?? [];
        if (lines.Any(l => l.Amount < 0m))
            return Result.Fail(DomainErrors.BadRequest("Gli importi previsti non possono essere negativi."));
        if (lines.GroupBy(l => l.CategoryId).Any(g => g.Count() > 1))
            return Result.Fail(DomainErrors.BadRequest("Categoria ripetuta nella previsione."));

        var categoryIds = (await GetPlannableCategories(userId)).Select(c => c.Id).ToHashSet();
        var unknown = lines.FirstOrDefault(l => !categoryIds.Contains(l.CategoryId));
        if (unknown is not null)
            return Result.Fail(DomainErrors.NotFound("Category", unknown.CategoryId));

        var budgets = lines
            .Where(l => l.Amount is > 0m)
            .Select(l => new CategoryBudget { CategoryId = l.CategoryId, Month = monthStart, Amount = l.Amount!.Value });

        await _repository.ReplaceMonthBudgets(monthStart, budgets, userId);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }
}
