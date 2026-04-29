namespace RDS.ExpenseTracker.Domain.Models.DataImport;

public class ImportTransferModel
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string FromAccount { get; set; } = string.Empty;
    public int FromAccountId { get; set; }
    public string ToAccount { get; set; } = string.Empty;
    public int ToAccountId { get; set; }
}
