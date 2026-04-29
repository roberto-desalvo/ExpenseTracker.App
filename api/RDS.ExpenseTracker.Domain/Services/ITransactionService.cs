using FluentResults;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ITransactionService : IService
{
    Task<Result<PagedResult<TransactionDto>>> GetPagedTransactions(TransactionQueryRequest request);
    Task<Result<IEnumerable<TransactionDto>>> GetTransactions(TransactionQueryRequest? filter = null);
    Task<Result<TransactionDto?>> GetTransaction(int id);
    Task<Result<TransactionDto?>> GetLatestTransaction();
    Task<Result> AddTransactions(IEnumerable<TransactionDto> transactions);
    Task<Result<int>> AddTransaction(TransactionDto transaction);
    Task<Result> UpdateTransaction(TransactionDto transaction);
    Task<Result> DeleteTransaction(int id);
    Task<Result> DeleteAllTransactions();
    Task<Result> ResetTransactions(IEnumerable<TransactionDto> transactions);
}
