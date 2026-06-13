using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface ITransferRepository : IRepositoryBase
{
    Task<Transfer?> GetTransfer(int id);
    Task<Transfer?> GetTransferByExternalId(string externalId);
    Task<IEnumerable<Transfer>> GetTransfers();
    Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date);
    Task AddTransfer(Transfer transfer);
    Task UpdateTransfer(Transfer transfer);
    Task DeleteTransfer(int id);
}