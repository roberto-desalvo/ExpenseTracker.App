namespace RDS.ExpenseTracker.Domain.Common;

public interface ISellaPdfOptions
{
    string DefaultAccountName { get; }
    Dictionary<string, string> IbanToAccountMap { get; }
}
