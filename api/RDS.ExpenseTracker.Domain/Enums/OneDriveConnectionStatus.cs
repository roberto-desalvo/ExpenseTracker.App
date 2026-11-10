using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Domain.Enums;

/// <summary>Stato del collegamento OneDrive di un utente. Serializzato come stringa nelle API.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<OneDriveConnectionStatus>))]
public enum OneDriveConnectionStatus
{
    /// <summary>Nessun collegamento (solo nelle API: a DB la riga non esiste).</summary>
    NotConnected,

    /// <summary>Codice di accesso emesso: si attende che l'utente autorizzi l'app sul sito Microsoft.</summary>
    Pending,

    Connected,

    /// <summary>L'autorizzazione è stata revocata o è scaduta: l'utente deve ricollegare OneDrive.</summary>
    ReconnectRequired,
}
