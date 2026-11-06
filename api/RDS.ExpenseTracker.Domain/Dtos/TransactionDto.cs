namespace RDS.ExpenseTracker.Domain.Dtos;

public class TransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public int AccountId { get; set; }
    public string Account { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Category { get; set; } = string.Empty;

    // Sola lettura: il collegamento a un trasferimento si gestisce con gli endpoint dei trasferimenti.
    public int? TransferId { get; set; }

    /// <summary>True se il movimento è collegato a un trasferimento a cui manca il ramo di segno opposto.</summary>
    public bool TransferIncomplete { get; set; }
}
