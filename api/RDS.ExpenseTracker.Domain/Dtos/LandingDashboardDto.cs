namespace RDS.ExpenseTracker.Domain.Dtos;

public class LandingDashboardDto
{
    public DateTime AsOf { get; set; }
    public DateTime MonthStart { get; set; }
    public IEnumerable<LandingAccountBalanceDto> Accounts { get; set; } = [];
    public IEnumerable<LandingCategorySummaryDto> Categories { get; set; } = [];
    public LandingTotalsDto Totals { get; set; } = new();
    public TimeSeriesListDto NetWorthSeries { get; set; } = new();
}
