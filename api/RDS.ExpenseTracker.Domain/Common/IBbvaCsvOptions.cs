namespace RDS.ExpenseTracker.Domain.Common;

/// <summary>
/// Configuration options for BBVA CSV import.
/// </summary>
public interface IBbvaCsvOptions
{
    /// <summary>
    /// Default account name for BBVA imported transactions.
    /// </summary>
    string DefaultAccountName { get; }
}