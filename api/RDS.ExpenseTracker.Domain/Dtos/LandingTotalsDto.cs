namespace RDS.ExpenseTracker.Domain.Dtos;

public class LandingTotalsDto
{
    public decimal CurrentBalanceTotal { get; set; }
    public decimal SpentMonth { get; set; }
    public decimal EarnedMonth { get; set; }
    public decimal NetMonth { get; set; }
}
