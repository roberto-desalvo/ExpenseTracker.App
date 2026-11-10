using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class OneDriveConnectionRepository : RepositoryBase, IOneDriveConnectionRepository
{
    public OneDriveConnectionRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public Task<OneDriveConnection?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        => Context.OneDriveConnections.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task<OneDriveConnection?> GetBySubscriptionIdAsync(string subscriptionId, CancellationToken cancellationToken = default)
        => Context.OneDriveConnections.AsNoTracking().FirstOrDefaultAsync(c => c.SubscriptionId == subscriptionId, cancellationToken);

    public async Task<IReadOnlyList<OneDriveConnection>> GetConnectedAsync(CancellationToken cancellationToken = default)
        => await Context.OneDriveConnections.AsNoTracking()
            .Where(c => c.Status == OneDriveConnectionStatus.Connected)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(OneDriveConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var entry = Context.Entry(connection);
        entry.State = connection.Id == 0 ? EntityState.Added : EntityState.Modified;
        await Context.SaveChangesAsync(cancellationToken);
        // Le letture sono non tracciate: l'entità non deve restare nel change tracker.
        entry.State = EntityState.Detached;
    }

    public Task DeleteAsync(int connectionId, CancellationToken cancellationToken = default)
        => Context.OneDriveConnections.Where(c => c.Id == connectionId).ExecuteDeleteAsync(cancellationToken);

    public async Task<bool> TryAcquireLockAsync(int connectionId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var lockedUntil = now + duration;

        var updated = await Context.OneDriveConnections
            .Where(c => c.Id == connectionId && (c.LockedUntil == null || c.LockedUntil < now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockedUntil, lockedUntil), cancellationToken);

        return updated == 1;
    }

    public Task ReleaseLockAsync(int connectionId, CancellationToken cancellationToken = default)
        => Context.OneDriveConnections
            .Where(c => c.Id == connectionId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockedUntil, (DateTime?)null), cancellationToken);

    public Task UpdateDeltaLinkAsync(int connectionId, string deltaLink, DateTime syncedOn, CancellationToken cancellationToken = default)
        => Context.OneDriveConnections
            .Where(c => c.Id == connectionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.DeltaLink, deltaLink)
                .SetProperty(x => x.LastSyncOn, syncedOn), cancellationToken);

    public Task UpdateSubscriptionAsync(int connectionId, string subscriptionId, DateTime expiresAt, CancellationToken cancellationToken = default)
        => Context.OneDriveConnections
            .Where(c => c.Id == connectionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.SubscriptionId, subscriptionId)
                .SetProperty(x => x.SubscriptionExpiresAt, expiresAt)
                .SetProperty(x => x.LastError, (string?)null), cancellationToken);

    public Task UpdateStatusAsync(int connectionId, OneDriveConnectionStatus status, string? lastError, CancellationToken cancellationToken = default)
        => Context.OneDriveConnections
            .Where(c => c.Id == connectionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.LastError, lastError), cancellationToken);
}
