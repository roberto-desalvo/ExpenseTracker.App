using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Domain.Enums;

/// <summary>
/// Stato di un import registrato in ImportLogs. Gli import sincroni (HTTP, OneDrive) nascono già conclusi;
/// quelli caricati dall'app passano da Queued e Processing. Serializzato come stringa nelle API.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ImportStatus>))]
public enum ImportStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
}
