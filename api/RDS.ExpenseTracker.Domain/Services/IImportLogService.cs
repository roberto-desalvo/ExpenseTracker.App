using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Models.DataImport;

namespace RDS.ExpenseTracker.Domain.Services;

/// <summary>
/// Registra in <c>ImportLogs</c> l'esito di ogni flusso di import.
/// </summary>
public interface IImportLogService : IService
{
    /// <param name="source">Tipo di import (es. "BBVA CSV").</param>
    /// <param name="fileName">Nome del file ricevuto.</param>
    /// <param name="userId">Utente proprietario dei dati importati.</param>
    /// <param name="statistics">Statistiche compilate dal servizio di import.</param>
    /// <param name="errors">Messaggi di errore; vuoto o null se l'import è riuscito.</param>
    /// <remarks>Non solleva eccezioni: un errore di scrittura del log non deve far fallire l'import.</remarks>
    Task RecordAsync(string source, string? fileName, int userId, ImportStatistics statistics, IEnumerable<string>? errors);
}
