using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IUserRepository : IRepositoryBase
{
    Task<User?> GetByAzureOid(string azureOid);
    Task<User?> GetByAppOid(string appOid);
    Task<User?> GetById(int id);
    Task<User?> GetByEmail(string email);
    Task<User> GetOrCreateUserAsync(string azureOid, string email);

    // Aggiunge l'utente al contesto senza salvare (usato dal seed dell'utente di test, privo di oid).
    Task AddUser(User user);
}
