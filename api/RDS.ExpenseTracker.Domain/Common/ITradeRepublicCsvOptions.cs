namespace RDS.ExpenseTracker.Domain.Common;

public interface ITradeRepublicCsvOptions
{
    /// <summary>Cash account name for Trade Republic (e.g. "Trade Republic").</summary>
    string DefaultAccountName { get; }

    /// <summary>Optional separate account for TRADING-type rows. Null to use DefaultAccountName.</summary>
    string? TradingAccountName { get; }
}
