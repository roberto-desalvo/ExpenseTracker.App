using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ITransactionService : IService
{
    Task<Result<TransactionQueryResult>> GetPagedTransactions(TransactionQueryRequest request);
    Task<Result<IEnumerable<TransactionMonthOptionDto>>> GetAvailableMonthOptions();
    Task<Result<TransactionDto?>> GetTransaction(int id);
    Task<Result<TransactionDto?>> GetLatestTransaction();
    Task<Result> AddTransactions(IEnumerable<TransactionDto> transactions);
    Task<Result<int>> AddTransaction(TransactionDto transaction);
    Task<Result> UpdateTransaction(TransactionDto transaction);
    Task<Result> DeleteTransaction(int id);
    Task<Result> DeleteAllTransactions();
    Task<Result<TimeSeriesListDto>> GetTimeSeries(TimeSeriesRequestDto request);
}
