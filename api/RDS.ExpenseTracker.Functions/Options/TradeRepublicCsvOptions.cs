using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

public class TradeRepublicCsvOptions : IAppOptions, ITradeRepublicCsvOptions
{
    public string SectionName => "TradeRepublicCsv";

    /// <inheritdoc/>
    public string DefaultAccountName { get; set; } = "Trade Republic";

    /// <inheritdoc/>
    public string? TradingAccountName { get; set; } = "Trade Republic Trading";

    /// <inheritdoc/>
    /// <example>
    /// appsettings.json:
    /// "IbanToAccountMap": { "IT23C0326822300052732779060": "Sella" }
    /// </example>
    public Dictionary<string, string> IbanToAccountMap { get; set; } = [];
}
