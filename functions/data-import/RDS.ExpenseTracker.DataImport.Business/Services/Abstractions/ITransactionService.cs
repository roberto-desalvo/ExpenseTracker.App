using RDS.ExpenseTracker.Domain.Models;

namespace RDS.ExpenseTracker.DataImport.Business.Services.Abstractions
{
    public interface ITransactionService
    {
        Task ResetAllTransactions(IEnumerable<Transaction> transactions);
        Task<Transaction> GetLatestAsync();
        Task<IEnumerable<Transaction>> GetAsync(DateTime fromDate, DateTime toDate);
        Task AddRangeAsync(IEnumerable<Transaction> transactions);
    }
}
