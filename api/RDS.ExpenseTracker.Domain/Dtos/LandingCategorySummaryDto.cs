namespace RDS.ExpenseTracker.Domain.Dtos;

public class LandingCategorySummaryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SpentMonth { get; set; }
    public decimal EarnedMonth { get; set; }
    public decimal NetMonth { get; set; }
}
