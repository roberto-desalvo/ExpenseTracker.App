using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Api.Options;

public class SellaPdfOptions : IAppOptions, ISellaPdfOptions
{
    public const string Section = "SellaPdf";

    string IAppOptions.SectionName => Section;

    public string DefaultAccountName { get; set; } = "Sella";

    public Dictionary<string, string> IbanToAccountMap { get; set; } = [];
}
