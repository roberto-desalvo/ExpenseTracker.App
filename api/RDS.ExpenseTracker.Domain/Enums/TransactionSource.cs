using System.Text.Json.Serialization;

namespace RDS.ExpenseTracker.Domain.Enums;

/// <summary>
/// Origine di un movimento, salvata a DB come stringa. Serializzata come stringa nelle API.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TransactionSource>))]
public enum TransactionSource
{
    // Inserito a mano dall'utente (o da dati demo/test).
    UserCreated,
    // Importato da un file della banca (upload, OneDrive, import in-app).
    FileImport,
    // Creato dall'abbinamento dello storico Excel (Spese.xlsx), per i conti senza import automatico.
    LegacyImport,
}
