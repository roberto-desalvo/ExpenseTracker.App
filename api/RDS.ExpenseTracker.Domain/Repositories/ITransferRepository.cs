using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface ITransferRepository : IRepositoryBase
{
    Task<Transfer?> GetTransfer(int id, int userId);
    Task<Transfer?> GetTransferByExternalId(string externalId, int userId);
    Task<IEnumerable<Transfer>> GetTransfers(int userId);
    Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date);
    Task AddTransfer(Transfer transfer);
    Task UpdateTransfer(Transfer transfer);
    Task DeleteTransfer(int id);
    Task DeleteTransfers(IEnumerable<int> transferIds);
}