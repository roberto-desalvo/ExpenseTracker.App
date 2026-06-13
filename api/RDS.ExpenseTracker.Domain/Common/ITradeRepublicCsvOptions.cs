namespace RDS.ExpenseTracker.Domain.Common;

public interface ITradeRepublicCsvOptions
{
    /// <summary>Cash account name for Trade Republic (e.g. "Trade Republic").</summary>
    string DefaultAccountName { get; }

    /// <summary>Optional separate account for TRADING-type rows. Null to use DefaultAccountName.</summary>
    string? TradingAccountName { get; }

    /// <summary>Maps a counterparty IBAN to an internal account name.
    /// Rows whose counterparty IBAN matches an entry are imported as Transfer entities;
    /// all other transfer-type rows are imported as plain Transactions.</summary>
    Dictionary<string, string> IbanToAccountMap { get; }
}
