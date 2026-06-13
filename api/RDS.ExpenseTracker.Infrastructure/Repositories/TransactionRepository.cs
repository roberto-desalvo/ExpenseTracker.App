using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class TransactionRepository : RepositoryBase, ITransactionRepository
{
    public TransactionRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task AddTransactions(IEnumerable<Transaction> transactions)
    {
        await Context.Transactions.AddRangeAsync(transactions);
    }

    public async Task<int> AddTransaction(Transaction transaction)
    {
        return await AddTransaction(transaction, false);
    }

    public async Task<int> AddTransaction(Transaction transaction, bool saveChanges)
    {
        await Context.Transactions.AddAsync(transaction);

        if (saveChanges)
        {
            await SaveChangesAsync();
        }

        return transaction.Id;
    }

    public async Task DeleteTransaction(int id)
    {
        var entity = await Context.Transactions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity != null)
        {
            Context.Transactions.Remove(entity);
        }
    }

    public async Task<Transaction?> GetTransaction(int id)
    {
        return await Context.Transactions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Transaction> GetLatestTransaction()
    {
        return await Context.Transactions
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync() ?? new Transaction();
    }

    public async Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds)
    {
        var list = externalIds.ToList();
        if (list.Count == 0) return [];

        return await Context.Transactions
            .Where(t => t.ExternalId != null && list.Contains(t.ExternalId))
            .Select(t => t.ExternalId!)
            .ToListAsync();
    }

    public async Task UpdateTransaction(Transaction modified)
    {
        var current = await Context.Transactions.FirstOrDefaultAsync(x => x.Id == modified.Id);
        if (current != null)
        {
            Context.Entry(current).CurrentValues.SetValues(modified);
        }
    }

    public async Task<IEnumerable<Transaction>> GetTransactions()
    {
        return await Context.Transactions
            .Include(transaction => transaction.AccountNavigation)
            .Include(transaction => transaction.CategoryNavigation)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request)
    {
        var query = Context.Transactions
            .Include(t => t.AccountNavigation)
            .Include(t => t.CategoryNavigation)
            .AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(t => t.Date >= request.FromDate);
        if (request.ToDate.HasValue)
            query = query.Where(t => t.Date <= request.ToDate);
        if (request.IdAccounts is { Length: > 0 })
            query = query.Where(t => request.IdAccounts.Contains(t.AccountId));
        if (request.IdCategories is { Length: > 0 })
            query = query.Where(t => t.CategoryId.HasValue && request.IdCategories.Contains(t.CategoryId.Value));
        if (request.IsIncome.HasValue)
            query = request.IsIncome.Value
                ? query.Where(t => t.Amount > 0m)
                : query.Where(t => t.Amount < 0m);

        var totalCount = await query.CountAsync();
        var totalIncomes = await query
            .Where(t => t.Amount > 0m && t.TransferId == null)
            .Select(t => (decimal?)t.Amount)
            .SumAsync() ?? 0m;
        var totalOutcomes = Math.Abs(
            await query
                .Where(t => t.Amount < 0m && t.TransferId == null)
                .Select(t => (decimal?)t.Amount)
                .SumAsync() ?? 0m);
        var totalNet = totalIncomes - totalOutcomes;

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (items, totalCount, totalIncomes, totalOutcomes, totalNet);
    }

    public async Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request)
    {
        var query = Context.Transactions
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date >= request.StartDate && t.Date <= request.EndDate);

        if (request.IdAccounts.Any())
            query = query.Where(t => request.IdAccounts.Contains(t.AccountId));

        if (request.IdCategories.Any())
            query = query.Where(t => t.CategoryId.HasValue && request.IdCategories.Contains(t.CategoryId.Value));

        return await query
            .Select(t => new
            {
                Date = t.Date!.Value,
                t.Amount,
                t.AccountId,
                t.CategoryId
            })
            .OrderBy(t => t.Date)
            .Select(t => ValueTuple.Create(t.Date, t.Amount, t.AccountId, t.CategoryId))
            .ToListAsync();
    }

    public async Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request)
    {
        var query = Context.Transactions
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date <= request.EndDate);

        if (request.IdAccounts.Any())
            query = query.Where(t => request.IdAccounts.Contains(t.AccountId));

        if (request.IdCategories.Any())
            query = query.Where(t => t.CategoryId.HasValue && request.IdCategories.Contains(t.CategoryId.Value));

        return await query
            .Select(t => new
            {
                Date = t.Date!.Value,
                t.Amount,
                t.AccountId,
                t.CategoryId
            })
            .OrderBy(t => t.Date)
            .Select(t => ValueTuple.Create(t.Date, t.Amount, t.AccountId, t.CategoryId))
            .ToListAsync();
    }

    public async Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate)
    {
        return await Context.Transactions
            .AsNoTracking()
            .Where(t => t.Date.HasValue && t.Date <= asOfDate)
            .GroupBy(t => t.AccountId)
            .Select(group => ValueTuple.Create(
                group.Key,
                group.Sum(t => t.Amount)))
            .ToListAsync();
    }

    public async Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate)
    {
        return await Context.Transactions
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date >= monthStart && t.Date <= asOfDate)
            .Where(t => t.TransferId == null)
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(group => ValueTuple.Create(
                group.Key,
                Math.Abs(group
                    .Where(t => t.Amount < 0m)
                    .Select(t => (decimal?)t.Amount)
                    .Sum() ?? 0m),
                group
                    .Where(t => t.Amount > 0m)
                    .Select(t => (decimal?)t.Amount)
                    .Sum() ?? 0m))
            .ToListAsync();
    }

    public async Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate)
    {
        var query = Context.Transactions
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date >= monthStart && t.Date <= asOfDate)
            .Where(t => t.TransferId == null);

        var spent = Math.Abs(await query
            .Where(t => t.Amount < 0m)
            .Select(t => (decimal?)t.Amount)
            .SumAsync() ?? 0m);

        var earned = await query
            .Where(t => t.Amount > 0m)
            .Select(t => (decimal?)t.Amount)
            .SumAsync() ?? 0m;

        return (spent, earned);
    }

    public async Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges()
    {
        var dateBounds = await Context.Transactions
            .Where(transaction => transaction.Date.HasValue)
            .Select(transaction => transaction.Date!.Value)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                MinDate = group.Min(),
                MaxDate = group.Max()
            })
            .FirstOrDefaultAsync();

        if (dateBounds is null)
        {
            return [];
        }

        var oldestMonth = new DateTime(dateBounds.MinDate.Year, dateBounds.MinDate.Month, 1);
        var newestMonth = new DateTime(dateBounds.MaxDate.Year, dateBounds.MaxDate.Month, 1);
        var monthRanges = new List<(DateTime StartDate, DateTime EndDate)>();

        for (var currentMonth = newestMonth; currentMonth >= oldestMonth; currentMonth = currentMonth.AddMonths(-1))
        {
            monthRanges.Add((
                currentMonth,
                currentMonth.AddMonths(1).AddTicks(-1)));
        }

        return monthRanges;
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId)
    {
        return await Context.Transactions
            .Where(x => x.TransferId == transferId)
            .ToListAsync();
    }

    public async Task DeleteAllTransactions()
    {
        var transactions = await Context.Transactions.ToListAsync();
        Context.Transactions.RemoveRange(transactions);
    }

    public async Task ResetTransactions(IEnumerable<Transaction> transactions)
    {
        await DeleteAllTransactions();
        await AddTransactions(transactions);
    }
}
