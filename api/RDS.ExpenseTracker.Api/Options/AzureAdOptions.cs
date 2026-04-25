using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

public class AzureAdOptions : IAppOptions
{
    public string SectionName => "AzureAd";

    public string Instance { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = string.Empty;
}