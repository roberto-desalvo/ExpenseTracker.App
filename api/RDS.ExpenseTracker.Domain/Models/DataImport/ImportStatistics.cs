namespace RDS.ExpenseTracker.Domain.Models.DataImport;

/// <summary>
/// Statistiche di un singolo import, compilate dal servizio di import man mano che elabora il file.
/// Il chiamante le legge anche quando l'import fallisce, per registrare il flusso in <c>ImportLogs</c>.
/// </summary>
public class ImportStatistics
{
    public int? AccountId { get; set; }
    public DateTime? MinTransactionDate { get; private set; }
    public DateTime? MaxTransactionDate { get; private set; }
    public int NewTransactionsCount { get; set; }
    public int NewTransfersCount { get; set; }
    public int SkippedDuplicatesCount { get; set; }

    public void TrackDates(IEnumerable<DateTime?> dates)
    {
        foreach (var date in dates)
        {
            if (!date.HasValue)
                continue;

            if (MinTransactionDate == null || date.Value < MinTransactionDate)
                MinTransactionDate = date.Value;

            if (MaxTransactionDate == null || date.Value > MaxTransactionDate)
                MaxTransactionDate = date.Value;
        }
    }
}
