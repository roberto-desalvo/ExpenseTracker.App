using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

// Ogni operazione è limitata ai movimenti registrati su conti dell'utente indicato (userId).
public interface ITransactionService : IService
{
    Task<Result<PagedResult<TransactionDto>>> GetPagedTransactions(TransactionQueryRequest request, int userId);
    Task<Result<IEnumerable<TransactionMonthOptionDto>>> GetAvailableMonthOptions(int userId);
    Task<Result<TransactionDto?>> GetTransaction(int id, int userId);
    Task<Result<TransactionDto?>> GetLatestTransaction(int userId);
    Task<Result> AddTransactions(IEnumerable<TransactionDto> transactions, int userId);
    Task<Result<int>> AddTransaction(TransactionDto transaction, int userId);
    Task<Result> UpdateTransaction(TransactionDto transaction, int userId);
    Task<Result> DeleteTransaction(int id, int userId);
    // Scompone il movimento in più voci con gli stessi dati e gli importi indicati (somma = importo originale;
    // il primo resta sull'originale); restituisce gli id dei nuovi movimenti.
    Task<Result<List<int>>> SplitTransaction(int id, TransactionSplitRequest request, int userId);
    Task<Result> DeleteAllTransactions(int userId);
    // Operazioni massive: falliscono senza modificare nulla se anche un solo id non è dell'utente. Restituiscono i movimenti coinvolti.
    Task<Result<int>> DeleteTransactions(TransactionBulkRequest request, int userId);
    Task<Result<int>> AssignCategory(TransactionBulkCategoryRequest request, int userId);
    Task<Result<TimeSeriesListDto>> GetTimeSeries(TimeSeriesRequestDto request, int userId);
    Task<Result<TimeSeriesListDto>> GetStock(TimeSeriesRequestDto request, int userId);
    Task<Result<LandingDashboardDto>> GetLanding(int userId, bool excludeTransfers = true);
}
