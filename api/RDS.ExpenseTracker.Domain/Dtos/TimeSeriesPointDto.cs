namespace RDS.ExpenseTracker.Domain.Dtos;

public class TimeSeriesPointDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Earned { get; set; }
    public decimal Spent { get; set; }
    /// <summary>Parte di <see cref="Spent"/> nella categoria Savings and investments.</summary>
    public decimal Invested { get; set; }
}