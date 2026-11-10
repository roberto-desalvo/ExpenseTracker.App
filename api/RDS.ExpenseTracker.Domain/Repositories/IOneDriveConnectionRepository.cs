using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Repositories;

// Le letture non sono tracciate e gli aggiornamenti durante la sincronizzazione sono UPDATE diretti: gli import
// condividono il DbContext e ne svuotano il change tracker (vedi IImportLogRepository.SaveImportLog).
public interface IOneDriveConnectionRepository : IRepositoryBase
{
    Task<OneDriveConnection?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<OneDriveConnection?> GetBySubscriptionIdAsync(string subscriptionId, CancellationToken cancellationToken = default);

    // Collegamenti attivi (Status = Connected), per il rinnovo delle subscription.
    Task<IReadOnlyList<OneDriveConnection>> GetConnectedAsync(CancellationToken cancellationToken = default);

    // Inserisce o aggiorna l'intera riga (flusso di collegamento).
    Task SaveAsync(OneDriveConnection connection, CancellationToken cancellationToken = default);

    Task DeleteAsync(int connectionId, CancellationToken cancellationToken = default);

    // Acquisisce il lock in modo atomico (UPDATE condizionato). False se un'altra sincronizzazione lo detiene.
    Task<bool> TryAcquireLockAsync(int connectionId, TimeSpan duration, CancellationToken cancellationToken = default);

    Task ReleaseLockAsync(int connectionId, CancellationToken cancellationToken = default);

    Task UpdateDeltaLinkAsync(int connectionId, string deltaLink, DateTime syncedOn, CancellationToken cancellationToken = default);

    // Azzera anche LastError: la subscription è di nuovo attiva.
    Task UpdateSubscriptionAsync(int connectionId, string subscriptionId, DateTime expiresAt, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(int connectionId, OneDriveConnectionStatus status, string? lastError, CancellationToken cancellationToken = default);
}
