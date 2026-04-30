using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface ITransactionRepository : IRepositoryBase
{
    Task<Transaction?> GetTransaction(int id);
    Task<IEnumerable<Transaction>> GetTransactions();
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedTransactions(TransactionQueryRequest request);
    Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges();
    Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId);
    Task<Transaction> GetLatestTransaction();
    Task AddTransactions(IEnumerable<Transaction> transactions);
    Task UpdateTransaction(Transaction transaction);
    Task DeleteTransaction(int id);
    Task DeleteAllTransactions();
    Task<int> AddTransaction(Transaction transaction);
    Task<int> AddTransaction(Transaction transaction, bool saveChanges);
    Task ResetTransactions(IEnumerable<Transaction> transactions);
}
