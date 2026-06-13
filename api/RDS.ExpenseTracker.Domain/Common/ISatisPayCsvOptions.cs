namespace RDS.ExpenseTracker.Domain.Common;

/// <summary>
/// Configuration options for Satispay CSV import
/// </summary>
public interface ISatisPayCsvOptions
{
    /// <summary>
    /// Default account name for Satispay (usually "Satispay")
    /// </summary>
    string DefaultAccountName { get; }

    /// <summary>
    /// Bank account name for "Ricarica Satispay" (bank recharge) transfers
    /// Maps IBAN from "Ricarica Satispay" entries to this account
    /// </summary>
    string? BankAccountName { get; }

    /// <summary>
    /// Mapping of IBAN to Account name
    /// Used to identify counterparty accounts for bank transfers
    /// </summary>
    Dictionary<string, string> IbanToAccountMap { get; }
}
