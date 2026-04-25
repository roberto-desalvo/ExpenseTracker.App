using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

public class KeyVaultOptions : IAppOptions
{
    public string SectionName => "KeyVault";

    public string Uri { get; set; } = string.Empty;
    public string ConnectionStringSecretName { get; set; } = string.Empty;
}