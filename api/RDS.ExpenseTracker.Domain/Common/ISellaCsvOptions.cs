namespace RDS.ExpenseTracker.Domain.Common;

public interface ISellaCsvOptions
{
    string DefaultAccountName { get; }
    Dictionary<string, string> IbanToAccountMap { get; }
}
