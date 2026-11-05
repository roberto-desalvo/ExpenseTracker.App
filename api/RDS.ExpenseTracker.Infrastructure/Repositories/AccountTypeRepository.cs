using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class AccountTypeRepository : RepositoryBase, IAccountTypeRepository
{
    public AccountTypeRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<AccountType>> GetAccountTypes()
        => await Context.AccountTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
}
