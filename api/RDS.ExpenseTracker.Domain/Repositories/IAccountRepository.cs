using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IAccountRepository : IRepositoryBase
{
    Task AddAccounts(IEnumerable<Account> accounts);
    Task UpdateAccount(Account account);
    Task DeleteAccounts(IEnumerable<int> accountIds);

    // Filtrati per utente: usati da AccountService, TransactionService, TransferService e dalla pipeline di import.
    Task<Account?> GetAccount(int id, int userId);
    Task<IEnumerable<Account>> GetAccounts(int userId);

    // Il conto dell'utente il cui tipo è elaborato da quell'import (al massimo uno, per vincolo sul tipo).
    Task<Account?> GetAccountByImportSource(int userId, BankFileSource source);
    Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId);
    Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges);
    Task<decimal> GetAvailability(int accountId, int userId);
    Task CalculateAvailabilities(IEnumerable<Transaction> transactions);
}
