using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Dtos;

public class ImportLogDto
{
    public int Id { get; set; }
    public DateTime ImportedOn { get; set; }
    public ImportStatus Status { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }

    public string Source { get; set; } = string.Empty;
    public string? FileName { get; set; }

    public int? AccountId { get; set; }
    public string? AccountName { get; set; }

    public DateTime? MinTransactionDate { get; set; }
    public DateTime? MaxTransactionDate { get; set; }

    public int NewTransactionsCount { get; set; }
    public int NewTransfersCount { get; set; }
    public int SkippedDuplicatesCount { get; set; }

    public string? ErrorMessage { get; set; }
}
