namespace RDS.ExpenseTracker.Domain.Dtos;

public class TransactionMonthOptionDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
}