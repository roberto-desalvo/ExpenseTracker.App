using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IAccountRepository : IRepositoryBase
{
    Task AddAccounts(IEnumerable<Account> accounts);
    Task UpdateAccount(Account account);
    Task<Account?> GetAccount(int id);
    Task<IEnumerable<Account>> GetAccounts();
    Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request);
    Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges);
    Task<decimal> GetAvailability(int accountId);
    Task CalculateAvailabilities(IEnumerable<Transaction> transactions);
}
