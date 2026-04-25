using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IRepositoryBase : IRepository
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}