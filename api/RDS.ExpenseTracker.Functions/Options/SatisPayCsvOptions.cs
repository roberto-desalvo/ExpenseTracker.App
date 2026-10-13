using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

/// <summary>
/// Configuration options for Satispay CSV import
/// </summary>
public class SatisPayCsvOptions : IAppOptions, ISatisPayCsvOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "SatisPayCsv";

    /// <inheritdoc />
    string IAppOptions.SectionName => SatisPayCsvOptions.SectionName;

    /// <inheritdoc />
    public string DefaultAccountName { get; set; } = "Satispay";

    /// <inheritdoc />
    public string? BankAccountName { get; set; }

    /// <inheritdoc />
    public Dictionary<string, string> IbanToAccountMap { get; set; } = new();
}
