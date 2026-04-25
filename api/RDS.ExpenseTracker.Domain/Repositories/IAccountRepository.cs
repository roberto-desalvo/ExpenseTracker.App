using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories
{
    public interface IAccountRepository : IRepositoryBase
    {
        Task AddAccounts(IEnumerable<Account> accounts);
        Task DeleteAccount(int id);
        Task UpdateAccount(Account account);
        Task<Account?> GetAccount(int id);
        Task<IEnumerable<Account>> GetAccounts();
        Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges);
        Task<decimal> GetAvailability(int accountId);
        Task CalculateAvailabilities(IEnumerable<Transaction> transactions);
    }
}
