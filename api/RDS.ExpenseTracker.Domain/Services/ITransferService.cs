using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ITransferService : IService
{
    Task<Result<IEnumerable<TransferDto>>> GetTransfers();
    Task<Result<TransferDto?>> GetTransfer(int id);
    Task<Result<int>> AddTransfer(TransferDto transfer);
    Task<Result> UpdateTransfer(TransferDto transfer);
    Task<Result> DeleteTransfer(int id);
}