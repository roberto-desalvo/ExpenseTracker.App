using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

public class SellaCsvOptions : IAppOptions, ISellaCsvOptions
{
    public const string SectionName = "SellaCsv";

    string IAppOptions.SectionName => SectionName;

    public string DefaultAccountName { get; set; } = "Sella";
}
