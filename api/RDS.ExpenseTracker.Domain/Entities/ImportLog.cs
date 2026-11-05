using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Entities;

// Traccia di un flusso di import (un file caricato), registrata sia in caso di successo sia di errore.
public class ImportLog
{
    public int Id { get; set; }

    // Istante in cui l'import è stato registrato (per gli import asincroni: quando il file è stato messo in coda).
    public DateTime ImportedOn { get; set; }
    public int UserId { get; set; }

    public ImportStatus Status { get; set; } = ImportStatus.Completed;
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }

    // Nome del blob con il file caricato dall'app (solo import asincroni).
    public string? BlobName { get; set; }

    // Tipo di import (es. "BBVA CSV", "Sella XLSX") e nome del file ricevuto.
    public string Source { get; set; } = string.Empty;
    public string? FileName { get; set; }

    // Conto a cui appartiene l'estratto conto; null se il file copre più conti (Excel) o non è stato risolto.
    public int? AccountId { get; set; }

    // Periodo coperto dalle righe valide del file, comprese quelle scartate perché già importate.
    public DateTime? MinTransactionDate { get; set; }
    public DateTime? MaxTransactionDate { get; set; }

    public int NewTransactionsCount { get; set; }
    public int NewTransfersCount { get; set; }
    public int SkippedDuplicatesCount { get; set; }

    public string? ErrorMessage { get; set; }

    public User? UserNavigation { get; set; }
    public Account? AccountNavigation { get; set; }
}
