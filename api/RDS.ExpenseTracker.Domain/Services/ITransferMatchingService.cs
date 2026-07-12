using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Services;

public record TransferMatchResult(bool RuleMatched, Transaction? Candidate);

/// <summary>
/// Links the two legs of a transfer during import using configured account-pair /
/// description-pattern rules (<see cref="ITransferMatchingOptions"/>), for cases where
/// no reliable IBAN mapping is available (e.g. Trade Republic/Sella to Satispay).
/// </summary>
public interface ITransferMatchingService : IService
{
    /// <summary>
    /// True if the given account name + description matches the "current" side of any
    /// configured rule. Used to decide whether a row needs to go through transfer
    /// matching at all before being processed in date order.
    /// </summary>
    bool IsTransferCandidate(string accountName, string description);

    /// <summary>
    /// Attempts to find an already-imported, unlinked transaction on the other side of a
    /// matching rule. <paramref name="consumedCandidateIds"/> is shared across a single
    /// import call so that multiple rows being matched in the same batch don't claim the
    /// same existing transaction twice; matched candidate ids must be added to it by the
    /// caller.
    /// </summary>
    Task<TransferMatchResult> TryMatchAsync(
        string accountName,
        string description,
        decimal signedAmount,
        DateTime date,
        HashSet<int> consumedCandidateIds);
}
