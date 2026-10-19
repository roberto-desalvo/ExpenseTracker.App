using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

public class TradeRepublicCsvOptions : IAppOptions, ITradeRepublicCsvOptions
{
    public string SectionName => "TradeRepublicCsv";

    /// <inheritdoc/>
    public string DefaultAccountName { get; set; } = "Trade Republic";

    /// <inheritdoc/>
    public string? TradingAccountName { get; set; } = "Trade Republic Trading";
}
