using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class OneDriveSyncStateRepository : RepositoryBase, IOneDriveSyncStateRepository
{
    private const int Id = OneDriveSyncState.SingletonId;

    public OneDriveSyncStateRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task<OneDriveSyncState> GetAsync(CancellationToken cancellationToken = default)
    {
        return await Context.OneDriveSyncStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == Id, cancellationToken)
            ?? throw new InvalidOperationException("OneDriveSyncState row is missing: apply the database migrations.");
    }

    public async Task<bool> TryAcquireLockAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var lockedUntil = now + duration;

        var updated = await Context.OneDriveSyncStates
            .Where(s => s.Id == Id && (s.LockedUntil == null || s.LockedUntil < now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockedUntil, lockedUntil), cancellationToken);

        return updated == 1;
    }

    public Task ReleaseLockAsync(CancellationToken cancellationToken = default)
        => Context.OneDriveSyncStates
            .Where(s => s.Id == Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockedUntil, (DateTime?)null), cancellationToken);

    public Task UpdateDeltaLinkAsync(string deltaLink, DateTime syncedOn, CancellationToken cancellationToken = default)
        => Context.OneDriveSyncStates
            .Where(s => s.Id == Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.DeltaLink, deltaLink)
                .SetProperty(x => x.LastSyncOn, syncedOn), cancellationToken);

    public Task UpdateSubscriptionAsync(string subscriptionId, DateTime expiresAt, CancellationToken cancellationToken = default)
        => Context.OneDriveSyncStates
            .Where(s => s.Id == Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.SubscriptionId, subscriptionId)
                .SetProperty(x => x.SubscriptionExpiresAt, expiresAt), cancellationToken);
}
