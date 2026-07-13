using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IUserRepository : IRepositoryBase
{
    Task<User?> GetByAzureOid(string azureOid);
    Task<User?> GetByAppOid(string appOid);
    Task<User> GetOrCreateUserAsync(string azureOid, string email);
}
