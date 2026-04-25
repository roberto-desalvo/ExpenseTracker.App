using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface ITransferRepository : IRepositoryBase
{
    Task<Transfer?> GetTransfer(int id);
    Task<IEnumerable<Transfer>> GetTransfers();
    Task AddTransfer(Transfer transfer);
    Task UpdateTransfer(Transfer transfer);
    Task DeleteTransfer(int id);
}