using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

// Ogni operazione è limitata ai trasferimenti tra conti dell'utente indicato (userId).
public interface ITransferService : IService
{
    Task<Result<IEnumerable<TransferDto>>> GetTransfers(int userId);
    Task<Result<TransferDto?>> GetTransfer(int id, int userId);
    Task<Result<int>> AddTransfer(TransferDto transfer, int userId);
    Task<Result> UpdateTransfer(TransferDto transfer, int userId);
    Task<Result> DeleteTransfer(int id, int userId);
}
