using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IAccountRepository : IRepositoryBase
{
    Task AddAccounts(IEnumerable<Account> accounts);
    Task UpdateAccount(Account account);

    // Non filtrati per utente: usati da TransactionService/TransferService, fuori
    // scope per lo scoping esplicito per utente in questa iterazione.
    Task<Account?> GetAccount(int id);
    Task<IEnumerable<Account>> GetAccounts();

    // Filtrati per utente: usati da AccountService/AccountController e dalla pipeline di import.
    Task<Account?> GetAccount(int id, int userId);
    Task<IEnumerable<Account>> GetAccounts(int userId);
    Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId);
    Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges);
    Task<decimal> GetAvailability(int accountId, int userId);
    Task CalculateAvailabilities(IEnumerable<Transaction> transactions);
}
