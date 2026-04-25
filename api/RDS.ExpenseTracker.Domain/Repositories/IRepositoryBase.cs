namespace RDS.ExpenseTracker.Domain.Repositories;

public interface IRepositoryBase
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}