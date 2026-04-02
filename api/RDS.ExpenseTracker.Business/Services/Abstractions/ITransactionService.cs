using RDS.ExpenseTracker.Business.QueryFilters;
using RDS.ExpenseTracker.Domain.Models;

namespace RDS.ExpenseTracker.Business.Services.Abstractions
{
    public interface ITransactionService
    {
        Task<Transaction?> GetTransaction(int id);
        Task<IEnumerable<Transaction>> GetTransactions();
        Task<IEnumerable<Transaction>> GetTransactions(TransactionQueryFilter filter);
        Task<Transaction> GetLatestTransaction();
        Task AddTransactions(IEnumerable<Transaction> transactions);
        Task UpdateTransaction(Transaction transaction);
        Task DeleteTransaction(int id);
        Task DeleteAllTransactions();
        Task<int> AddTransaction(Transaction transaction);
        Task<int> AddTransaction(Transaction transaction, bool saveChanges);
        Task ResetTransactions(IEnumerable<Transaction> transactions);
    }
}
