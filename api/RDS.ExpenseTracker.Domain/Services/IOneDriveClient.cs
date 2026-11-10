using RDS.ExpenseTracker.Domain.Models.OneDrive;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Accesso, tramite Microsoft Graph, alla cartella dell'app nel OneDrive personale dell'utente <c>userId</c>:
/// il token delegato (<c>Files.ReadWrite.AppFolder</c>) non vede il resto del drive. Implementato in Infrastructure.
/// </summary>
/// <remarks>
/// Ogni metodo può sollevare <see cref="OneDriveAuthorizationException"/> se l'utente deve ricollegare OneDrive.
/// </remarks>
public interface IOneDriveClient
{
    /// <summary>Cartella dell'app (<c>Apps/&lt;nome dell'app&gt;</c>): OneDrive la crea al primo accesso.</summary>
    Task<OneDriveAppFolder> GetAppFolderAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Nome visualizzato del proprietario del drive; null se il token non consente di leggerlo.</summary>
    Task<string?> GetDriveOwnerAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Elenca file e sottocartelle diretti di una cartella.</summary>
    Task<IReadOnlyList<OneDriveItem>> GetChildrenAsync(int userId, string folderId, CancellationToken cancellationToken = default);

    /// <summary>Scarica il contenuto di un file in memoria.</summary>
    Task<Stream> DownloadAsync(int userId, string itemId, CancellationToken cancellationToken = default);

    /// <summary>Restituisce l'id della sottocartella <paramref name="name"/>, creandola se manca.</summary>
    Task<string> EnsureChildFolderAsync(int userId, string parentFolderId, string name, CancellationToken cancellationToken = default);

    /// <summary>Sposta l'elemento nella cartella indicata, rinominandolo se esiste già un file con lo stesso nome.</summary>
    Task MoveAsync(int userId, string itemId, string targetFolderId, CancellationToken cancellationToken = default);

    /// <summary>Crea una subscription Graph per le notifiche di modifica su <paramref name="resource"/>.</summary>
    Task<OneDriveSubscription> CreateSubscriptionAsync(
        int userId, string resource, string notificationUrl, string clientState, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prolunga una subscription esistente. Restituisce null se non esiste più (scaduta o eliminata).
    /// </summary>
    Task<OneDriveSubscription?> RenewSubscriptionAsync(int userId, string subscriptionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    /// <summary>Elimina una subscription; non fallisce se non esiste più.</summary>
    Task DeleteSubscriptionAsync(int userId, string subscriptionId, CancellationToken cancellationToken = default);
}
