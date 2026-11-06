using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Domain.Dtos;

public class TimeSeriesDto
{
    public IEnumerable<TimeSeriesDimensionDto> Dimensions { get; set; } = [];
    public IEnumerable<TimeSeriesPointDto> Values { get; set; } = [];

    /// <summary>Solo serie di saldo: saldo puntuale prima di StartDate (movimenti con data &lt; StartDate).</summary>
    public decimal? OpeningBalance { get; set; }

    /// <summary>Solo serie di saldo: saldo puntuale a EndDate (movimenti con data &lt;= EndDate).</summary>
    public decimal? ClosingBalance { get; set; }
}
