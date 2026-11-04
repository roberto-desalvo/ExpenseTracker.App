using FluentResults;
using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Import automatico dei file caricati nella cartella OneDrive configurata
/// (<c>/Transazioni/&lt;Banca&gt;/file</c>), guidato dalle change notifications di Microsoft Graph.
/// </summary>
public interface IOneDriveImportService : IService
{
    /// <summary>
    /// Legge le modifiche dalla delta query, importa i nuovi file delle cartelle banca e li sposta in
    /// <c>Importati</c> o <c>Errori</c>. Alla prima esecuzione registra solo la baseline (nessun import).
    /// </summary>
    /// <returns>Fallisce se un'altra sincronizzazione è in corso o se manca l'utente di destinazione.</returns>
    Task<Result<OneDriveSyncSummary>> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea la subscription Graph se manca, o la rinnova se scade entro pochi giorni.
    /// </summary>
    Task<Result<OneDriveSubscriptionStatus>> EnsureSubscriptionAsync(string notificationUrl, CancellationToken cancellationToken = default);

    /// <summary>Verifica il clientState di una notifica ricevuta.</summary>
    bool IsValidClientState(string? clientState);
}

public sealed record OneDriveSyncSummary(bool BaselineCreated, int ImportedFiles, int FailedFiles, int IgnoredItems);

public sealed record OneDriveSubscriptionStatus(string SubscriptionId, DateTimeOffset ExpiresAt, string Action);

public static class OneDriveErrors
{
    public static ConflictResultError SyncInProgress()
        => new("onedrive.sync_in_progress", "Another OneDrive sync is in progress.");
}
