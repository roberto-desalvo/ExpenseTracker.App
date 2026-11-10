namespace RDS.ExpenseTracker.Domain.Models.OneDrive;

/// <summary>Elemento (file o cartella) di OneDrive come restituito da Microsoft Graph.</summary>
/// <param name="ParentId">Id della cartella che contiene l'elemento (null per la radice).</param>
public sealed record OneDriveItem(
    string Id,
    string Name,
    string? ParentId,
    bool IsFile,
    bool IsFolder,
    bool IsDeleted);

/// <summary>
/// Risultato di una delta query completa (tutte le pagine seguite).
/// </summary>
/// <param name="Items">Elementi creati, modificati o eliminati dopo il token precedente.</param>
/// <param name="DeltaLink">Link da usare per la prossima delta query.</param>
public sealed record OneDriveDeltaResult(IReadOnlyList<OneDriveItem> Items, string DeltaLink);

/// <summary>Subscription Graph (change notifications) attiva su una cartella.</summary>
public sealed record OneDriveSubscription(string Id, DateTimeOffset ExpiresAt);

/// <summary>
/// Sollevata quando il deltaLink salvato non è più valido (HTTP 410 Gone): bisogna ripartire da una nuova baseline.
/// </summary>
public sealed class OneDriveDeltaExpiredException(string message) : Exception(message);

/// <summary>Codice emesso da Microsoft per autorizzare l'app da un altro dispositivo/scheda (device code flow).</summary>
/// <param name="DeviceCode">Segreto con cui il backend riscatta il token: non va mostrato all'utente.</param>
/// <param name="UserCode">Codice che l'utente inserisce su <paramref name="VerificationUri"/>.</param>
public sealed record OneDriveDeviceCode(
    string DeviceCode,
    string UserCode,
    string VerificationUri,
    DateTimeOffset ExpiresAt,
    int PollIntervalSeconds);

/// <summary>Esito di un tentativo di riscatto del device code.</summary>
public enum OneDriveDeviceCodeOutcome
{
    /// <summary>L'utente non ha ancora completato l'accesso.</summary>
    Pending,
    Completed,
    Declined,
    Expired,
}

/// <summary>
/// L'utente ha revocato l'accesso o il refresh token è scaduto (90 giorni senza utilizzo): OneDrive va ricollegato.
/// </summary>
public sealed class OneDriveAuthorizationException(string message) : Exception(message);
