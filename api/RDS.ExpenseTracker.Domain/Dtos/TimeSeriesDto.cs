using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Dtos;

public class TimeSeriesDto
{
    public IEnumerable<TimeSeriesDimensionDto> Dimensions { get; set; } = [];
    public IEnumerable<TimeSeriesPointDto> Values { get; set; } = [];
}