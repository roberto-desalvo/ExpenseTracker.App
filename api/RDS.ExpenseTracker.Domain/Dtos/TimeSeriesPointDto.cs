namespace RDS.ExpenseTracker.Domain.Dtos;

public class TimeSeriesPointDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Earned { get; set; }
    public decimal Spent { get; set; }
}