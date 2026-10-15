using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

/// <summary>
/// Local development authentication bypass: when enabled, requests can authenticate with
/// <c>Authorization: DevUser &lt;username&gt;</c> instead of an Entra ID bearer token.
/// Startup fails if it is enabled outside the Development environment.
/// </summary>
public class DevAuthOptions : IAppOptions
{
    public string SectionName => "DevAuth";

    public bool Enabled { get; set; }
}
