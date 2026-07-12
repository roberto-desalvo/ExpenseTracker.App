namespace RDS.ExpenseTracker.Domain.Common;

public interface ITransferMatchingOptions
{
    /// <summary>
    /// Account-pair / description-pattern rules used to link the two legs of a transfer
    /// during import when no reliable IBAN mapping is available.
    /// </summary>
    List<TransferMatchRule> Rules { get; }
}
