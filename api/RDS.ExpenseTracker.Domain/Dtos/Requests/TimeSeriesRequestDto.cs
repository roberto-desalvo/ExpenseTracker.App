namespace RDS.ExpenseTracker.Domain.Dtos.Requests;

public class TimeSeriesRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public IEnumerable<int> IdAccounts { get; set; } = [];
    public IEnumerable<int> IdCategories { get; set; } = [];
    public int Granularity { get; set; } = 1; // TimeGranularityEnum
}