using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public abstract class RepositoryBase : IRepositoryBase
{
    protected ExpenseTrackerContext Context { get; }

    protected RepositoryBase(ExpenseTrackerContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}