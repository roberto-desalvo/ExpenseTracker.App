using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IAccountTypeRepository : IRepositoryBase
{
    Task<IEnumerable<AccountType>> GetAccountTypes();
}
