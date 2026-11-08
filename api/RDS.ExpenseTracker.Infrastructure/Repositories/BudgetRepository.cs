using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class BudgetRepository : RepositoryBase, IBudgetRepository
{
    public BudgetRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    private IQueryable<CategoryBudget> ForUser(int userId)
        => Context.CategoryBudgets.Where(b => b.CategoryNavigation!.UserId == userId);

    public async Task<IEnumerable<CategoryBudget>> GetBudgets(DateTime monthStart, int userId)
        => await ForUser(userId)
            .AsNoTracking()
            .Where(b => b.Month == monthStart)
            .ToListAsync();

    public async Task ReplaceMonthBudgets(DateTime monthStart, IEnumerable<CategoryBudget> budgets, int userId)
    {
        var existing = await ForUser(userId)
            .Where(b => b.Month == monthStart)
            .ToDictionaryAsync(b => b.CategoryId);
        var incoming = budgets.ToDictionary(b => b.CategoryId);

        foreach (var (categoryId, current) in existing)
        {
            if (incoming.TryGetValue(categoryId, out var updated))
                current.Amount = updated.Amount;
            else
                Context.CategoryBudgets.Remove(current);
        }

        foreach (var (categoryId, budget) in incoming)
        {
            if (!existing.ContainsKey(categoryId))
                await Context.CategoryBudgets.AddAsync(budget);
        }
    }

    // Stessa regola di esclusione dei trasferimenti usata da Analisi e Dashboard (TransactionRepository).
    public async Task<IEnumerable<(int? CategoryId, decimal Spent)>> GetSpentByCategory(DateTime monthStart, DateTime endDate, int userId)
        => await Context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountNavigation.UserId == userId)
            .Where(t => t.Date.HasValue && t.Date >= monthStart && t.Date <= endDate)
            .Where(t => t.Amount < 0m)
            .Where(t => t.TransferId == null && t.CategoryNavigation!.Kind != CategoryEnum.MoneyTransfers)
            .GroupBy(t => t.CategoryId)
            .Select(group => ValueTuple.Create(group.Key, -group.Sum(t => t.Amount)))
            .ToListAsync();
}
