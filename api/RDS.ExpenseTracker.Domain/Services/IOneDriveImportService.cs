using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Import automatico dei file caricati nella cartella OneDrive collegata da ogni utente
/// (<c>/&lt;Cartella&gt;/&lt;Banca&gt;/file</c>), guidato dalle change notifications di Microsoft Graph.
/// </summary>
public interface IOneDriveImportService : IService
{
    /// <summary>
    /// Legge le modifiche dalla delta query dell'utente, importa i nuovi file delle cartelle banca e li sposta in
    /// <c>Importati</c> o <c>Errori</c>. Senza un collegamento attivo non fa nulla.
    /// </summary>
    /// <returns>Fallisce se un'altra sincronizzazione dello stesso utente è in corso (da ritentare).</returns>
    Task<Result<OneDriveSyncSummary>> SyncAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea la subscription Graph del collegamento se manca, o la rinnova se scade entro pochi giorni.
    /// </summary>
    Task<Result<OneDriveSubscriptionStatus>> EnsureSubscriptionAsync(OneDriveConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue <see cref="EnsureSubscriptionAsync"/> per tutti i collegamenti attivi; l'errore di uno non blocca gli altri.
    /// </summary>
    Task<OneDriveRenewalSummary> RenewSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Utenti da sincronizzare per le notifiche ricevute: quelle di subscription sconosciute o con un
    /// <c>clientState</c> diverso da quello del collegamento vengono scartate.
    /// </summary>
    Task<IReadOnlyList<int>> ResolveNotifiedUsersAsync(
        IEnumerable<OneDriveNotification> notifications, CancellationToken cancellationToken = default);
}

public sealed record OneDriveNotification(string? SubscriptionId, string? ClientState);

public sealed record OneDriveSyncSummary(bool BaselineCreated, int ImportedFiles, int FailedFiles, int IgnoredItems);

public sealed record OneDriveSubscriptionStatus(string SubscriptionId, DateTimeOffset ExpiresAt, string Action);

public sealed record OneDriveRenewalSummary(int Connections, int Created, int Renewed, int Failed);

public static class OneDriveErrors
{
    public static ConflictResultError SyncInProgress()
        => new("onedrive.sync_in_progress", "Another OneDrive sync is in progress.");
}
