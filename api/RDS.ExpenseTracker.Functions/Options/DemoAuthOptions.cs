using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

/// <summary>
/// Demo login: when enabled, <c>POST api/auth/demo</c> issues a short-lived bearer token, signed by the API itself,
/// for the test user (<c>test@dev.local</c>). Unlike <see cref="DevAuthOptions"/> it can be enabled in production.
/// </summary>
public class DemoAuthOptions : IAppOptions
{
    public string SectionName => "DemoAuth";

    public bool Enabled { get; set; }

    /// <summary>HMAC-SHA256 key used to sign demo tokens: at least 32 bytes (UTF-8). Keep it secret.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 60;
}
