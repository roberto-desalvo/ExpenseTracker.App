using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

/// <summary>
/// Configuration options for BBVA CSV import.
/// </summary>
public class BbvaCsvOptions : IAppOptions, IBbvaCsvOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "BbvaCsv";

    /// <inheritdoc />
    string IAppOptions.SectionName => SectionName;

    /// <inheritdoc />
    public string DefaultAccountName { get; set; } = "BBVA";
}