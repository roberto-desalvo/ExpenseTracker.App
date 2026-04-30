namespace RDS.ExpenseTracker.Domain.Dtos;

public class TimeSeriesPointDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}