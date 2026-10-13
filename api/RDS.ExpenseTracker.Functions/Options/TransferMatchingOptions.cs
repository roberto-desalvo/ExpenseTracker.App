using RDS.ExpenseTracker.Domain.Common;

namespace RDS.ExpenseTracker.Functions.Options;

public class TransferMatchingOptions : IAppOptions, ITransferMatchingOptions
{
    public const string SectionName = "TransferMatching";

    /// <inheritdoc />
    string IAppOptions.SectionName => TransferMatchingOptions.SectionName;

    /// <inheritdoc />
    public List<TransferMatchRule> Rules { get; set; } = [];
}
