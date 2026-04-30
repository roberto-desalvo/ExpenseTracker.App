namespace RDS.ExpenseTracker.Domain.Dtos;

public class TransferDto
{
    public int Id { get; set; }
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
}