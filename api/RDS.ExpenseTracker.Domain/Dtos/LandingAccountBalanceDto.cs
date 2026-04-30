namespace RDS.ExpenseTracker.Domain.Dtos;

public class LandingAccountBalanceDto
{
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}
