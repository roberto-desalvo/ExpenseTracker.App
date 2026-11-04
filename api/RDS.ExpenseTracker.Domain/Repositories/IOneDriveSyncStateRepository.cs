using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Repositories;

// Gli aggiornamenti sono UPDATE diretti (non entità tracciate): durante la sincronizzazione gli import
// condividono il DbContext e ne svuotano il change tracker (vedi IImportLogRepository.SaveImportLog).
public interface IOneDriveSyncStateRepository : IRepositoryBase
{
    // Restituisce la riga di stato (creata dalla migration), non tracciata.
    Task<OneDriveSyncState> GetAsync(CancellationToken cancellationToken = default);

    // Acquisisce il lock in modo atomico (UPDATE condizionato). False se un'altra sincronizzazione lo detiene.
    Task<bool> TryAcquireLockAsync(TimeSpan duration, CancellationToken cancellationToken = default);

    Task ReleaseLockAsync(CancellationToken cancellationToken = default);

    Task UpdateDeltaLinkAsync(string deltaLink, DateTime syncedOn, CancellationToken cancellationToken = default);

    Task UpdateSubscriptionAsync(string subscriptionId, DateTime expiresAt, CancellationToken cancellationToken = default);
}
