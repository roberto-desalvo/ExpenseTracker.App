namespace RDS.ExpenseTracker.Domain.Dtos;

public class TimeSeriesListDto
{
    public string Granularity { get; set; } = string.Empty;
    public IEnumerable<TimeSeriesDto> Series { get; set; } = [];
}