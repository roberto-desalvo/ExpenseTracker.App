using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

// Le letture che accettano userId restituiscono solo i movimenti registrati su conti di quell'utente.
public interface ITransactionRepository : IRepositoryBase
{
    Task<Transaction?> GetTransaction(int id, int userId);
    Task<IEnumerable<Transaction>> GetTransactions(int userId);
    Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request, int userId);
    Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request, int userId);
    Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request, int userId);
    Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate, int userId);
    Task<IEnumerable<(int AccountId, decimal Spent, decimal Earned)>> GetAccountMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId);
    Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId);
    Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId);
    Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges(int userId);
    Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId);
    Task<Transaction> GetLatestTransaction(int userId);
    Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds, int userId);
    // Movimenti tracciati (per modificarne la categoria) con data nel range, esclusi i trasferimenti.
    Task<List<Transaction>> GetCategorizableTransactions(int userId, DateTime startDate, DateTime endDate);
    Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains);
    Task AddTransactions(IEnumerable<Transaction> transactions);
    Task UpdateTransaction(Transaction transaction);
    Task DeleteTransaction(int id);
    Task DeleteAllTransactions(int userId);
    Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds);
    Task<int> AddTransaction(Transaction transaction);
    Task<int> AddTransaction(Transaction transaction, bool saveChanges);
    Task ResetTransactions(IEnumerable<Transaction> transactions, int userId);
}
