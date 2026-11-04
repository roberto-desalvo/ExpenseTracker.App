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
