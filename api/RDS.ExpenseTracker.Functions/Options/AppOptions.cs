using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

public class AppOptions : IAppOptions
{
    public string SectionName => "App";
    public string FEUrl { get; set; } = string.Empty;
    public string[] AllowedLocalOrigins { get; set; } = [];
}