using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class TransactionRepository : RepositoryBase, ITransactionRepository
{
    public TransactionRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    // Un movimento appartiene all'utente proprietario del conto su cui è registrato.
    private IQueryable<Transaction> ForUser(int userId)
        => Context.Transactions.Where(t => t.AccountNavigation.UserId == userId);

    private const int MoneyTransfersCategoryId = (int)CategoryEnum.MoneyTransfers;

    // Un trasferimento sposta denaro tra conti dell'utente: non è né entrata né uscita.
    // Oltre ai movimenti collegati a un Transfer, si considerano tali anche quelli in categoria
    // "Money transfers" rimasti senza controparte (es. import senza match del conto opposto).
    private static IQueryable<Transaction> ExcludingTransfers(IQueryable<Transaction> query)
        => query.Where(t => t.TransferId == null && t.CategoryId != MoneyTransfersCategoryId);

    // Ordinamento della lista paginata; campo sconosciuto o assente => data. L'Id come
    // chiave secondaria rende stabile la paginazione a parità di valore.
    private static IQueryable<Transaction> ApplySorting(IQueryable<Transaction> query, string? sortBy, string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Transaction> ordered = sortBy?.ToLowerInvariant() switch
        {
            "description" => descending ? query.OrderByDescending(t => t.Description) : query.OrderBy(t => t.Description),
            "category" => descending ? query.OrderByDescending(t => t.CategoryNavigation!.Name) : query.OrderBy(t => t.CategoryNavigation!.Name),
            "account" => descending ? query.OrderByDescending(t => t.AccountNavigation.Name) : query.OrderBy(t => t.AccountNavigation.Name),
            "amount" => descending ? query.OrderByDescending(t => t.Amount) : query.OrderBy(t => t.Amount),
            _ => descending ? query.OrderByDescending(t => t.Date) : query.OrderBy(t => t.Date),
        };

        return descending ? ordered.ThenByDescending(t => t.Id) : ordered.ThenBy(t => t.Id);
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

    public async Task<Transaction?> GetTransaction(int id, int userId)
    {
        return await ForUser(userId).FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Transaction> GetLatestTransaction(int userId)
    {
        return await ForUser(userId)
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync() ?? new Transaction();
    }

    public async Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds, int userId)
    {
        var list = externalIds.ToList();
        if (list.Count == 0) return [];

        // deduplica per utente: lo stesso movimento può essere importato da utenti diversi
        return await ForUser(userId)
            .Where(t => t.ExternalId != null && list.Contains(t.ExternalId))
            .Select(t => t.ExternalId!)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetCategorizableTransactions(int userId, DateTime startDate, DateTime endDate)
    {
        var from = startDate.Date;
        var toExclusive = endDate.Date.AddDays(1);

        return await ExcludingTransfers(ForUser(userId))
            .Where(t => t.Date != null && t.Date >= from && t.Date < toExclusive)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains)
    {
        var targetDate = date.Date;

        return await Context.Transactions
            .Where(t =>
                t.AccountId == accountId &&
                t.TransferId == null &&
                t.Amount == amount &&
                t.Date != null &&
                t.Date.Value.Date == targetDate &&
                t.Description.Contains(descriptionContains))
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
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

    public async Task<IEnumerable<Transaction>> GetTransactions(int userId)
    {
        return await ForUser(userId)
            .Include(transaction => transaction.AccountNavigation)
            .Include(transaction => transaction.CategoryNavigation)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request, int userId)
    {
        var query = ForUser(userId)
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
        var flows = ExcludingTransfers(query);
        var totalIncomes = await flows
            .Where(t => t.Amount > 0m)
            .Select(t => (decimal?)t.Amount)
            .SumAsync() ?? 0m;
        var totalOutcomes = Math.Abs(
            await flows
                .Where(t => t.Amount < 0m)
                .Select(t => (decimal?)t.Amount)
                .SumAsync() ?? 0m);
        var totalNet = totalIncomes - totalOutcomes;

        var items = await ApplySorting(query, request.SortBy, request.SortDirection)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (items, totalCount, totalIncomes, totalOutcomes, totalNet);
    }

    public async Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request, int userId)
    {
        var query = ForUser(userId)
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date >= request.StartDate && t.Date <= request.EndDate);

        if (request.ExcludeTransfers)
            query = ExcludingTransfers(query);

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

    public async Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request, int userId)
    {
        var query = ForUser(userId)
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date <= request.EndDate);

        if (request.ExcludeTransfers)
            query = ExcludingTransfers(query);

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

    public async Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate, int userId)
    {
        return await ForUser(userId)
            .AsNoTracking()
            .Where(t => t.Date.HasValue && t.Date <= asOfDate)
            .GroupBy(t => t.AccountId)
            .Select(group => ValueTuple.Create(
                group.Key,
                group.Sum(t => t.Amount)))
            .ToListAsync();
    }

    public async Task<IEnumerable<(int AccountId, decimal Spent, decimal Earned)>> GetAccountMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId)
    {
        var query = ForUser(userId)
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date >= monthStart && t.Date <= asOfDate);

        if (excludeTransfers)
            query = ExcludingTransfers(query);

        return await query
            .GroupBy(t => t.AccountId)
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

    public async Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId)
    {
        var query = ForUser(userId)
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date >= monthStart && t.Date <= asOfDate)
            .Where(t => t.CategoryId.HasValue);

        if (excludeTransfers)
            query = ExcludingTransfers(query);

        return await query
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

    public async Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId)
    {
        var query = ForUser(userId)
            .AsNoTracking()
            .Where(t => t.Date.HasValue)
            .Where(t => t.Date >= monthStart && t.Date <= asOfDate);

        if (excludeTransfers)
            query = ExcludingTransfers(query);

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

    public async Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges(int userId)
    {
        var dateBounds = await ForUser(userId)
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

    public async Task DeleteAllTransactions(int userId)
    {
        var transactions = await ForUser(userId).ToListAsync();
        Context.Transactions.RemoveRange(transactions);
    }

    public async Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds)
    {
        var ids = accountIds.ToList();
        if (ids.Count == 0) return [];

        var transactions = await Context.Transactions
            .Where(t => ids.Contains(t.AccountId))
            .ToListAsync();

        var transferIds = transactions
            .Where(t => t.TransferId.HasValue)
            .Select(t => t.TransferId!.Value)
            .Distinct()
            .ToList();

        Context.Transactions.RemoveRange(transactions);

        return transferIds;
    }

    public async Task ResetTransactions(IEnumerable<Transaction> transactions, int userId)
    {
        await DeleteAllTransactions(userId);
        await AddTransactions(transactions);
    }
}
