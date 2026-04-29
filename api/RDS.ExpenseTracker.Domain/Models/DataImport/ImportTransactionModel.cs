namespace RDS.ExpenseTracker.Domain.Models.DataImport;

public class ImportTransactionModel
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public int AccountId { get; set; }
    public string Account { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}
