using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class AccountRepository : RepositoryBase, IAccountRepository
{
    public AccountRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task AddAccounts(IEnumerable<Account> accounts)
    {
        await Context.Accounts.AddRangeAsync(accounts);
    }

    public async Task UpdateAccount(Account account)
    {
        var current = await Context.Accounts.FirstOrDefaultAsync(x => x.Id == account.Id);
        if (current != null)
        {
            Context.Entry(current).CurrentValues.SetValues(account);
        }
    }

    public async Task<decimal> GetAvailability(int accountId)
    {
        var exists = await Context.Accounts.AnyAsync(x => x.Id == accountId);
        if (!exists)
        {
            return 0;
        }

        return await Context.Transactions
            .Where(x => x.AccountId == accountId)
            .SumAsync(x => x.Amount);
    }

    public async Task<Account?> GetAccount(int id)
    {
        return await Context.Accounts.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<Account>> GetAccounts()
    {
        return await Context.Accounts
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request)
    {
        var query = Context.Accounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = request.Name.Trim();
            query = query.Where(a => EF.Functions.Like(a.Name, $"%{normalizedName}%"));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Name)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }


    public async Task CalculateAvailabilities(IEnumerable<Transaction> transactions)
    {
        var accountIds = transactions.Select(x => x.AccountId).Distinct();

        foreach (var id in accountIds)
        {
            var sum = transactions.Where(x => x.AccountId == id).Select(x => x.Amount).Sum();
            await UpdateAvailability(id, sum, false);
        }
    }

    public async Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges)
    {
        var exists = await Context.Accounts.AnyAsync(x => x.Id == accountId);
        if (!exists)
        {
            return false;
        }

        if (saveChanges)
        {
            await SaveChangesAsync();
        }

        return true;
    }

}
