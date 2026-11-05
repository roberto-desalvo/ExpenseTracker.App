namespace RDS.ExpenseTracker.Domain.Common;

public interface ITradeRepublicCsvOptions
{
    /// <summary>
    /// Name of an optional separate account for TRADING-type rows. If the user has no account with
    /// this name (or it is null) those rows go to the user's Trade Republic account (resolved by account type).
    /// </summary>
    string? TradingAccountName { get; }
}
