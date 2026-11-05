using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;

namespace RDS.ExpenseTracker.Domain.Services;

public record TransferMatchResult(bool RuleMatched, Transaction? Candidate);

/// <summary>
/// Links the two legs of a transfer during import using configured account-type-pair /
/// description-pattern rules (<see cref="ITransferMatchingOptions"/>), for cases where
/// no reliable IBAN mapping is available (e.g. Trade Republic/Sella to Satispay).
/// </summary>
public interface ITransferMatchingService : IService
{
    /// <summary>
    /// True if the given account (by its account type) + description matches the "current" side of any
    /// configured rule. Used to decide whether a row needs to go through transfer
    /// matching at all before being processed in date order.
    /// </summary>
    bool IsTransferCandidate(Account account, string description);

    /// <summary>
    /// The opposite side of every configured rule matched by the given account (by its account type) + description,
    /// evaluated in memory without touching the database (used by the bulk reconciliation).
    /// </summary>
    IReadOnlyList<TransferRuleCounterpart> GetCounterpartRules(Account account, string description);

    /// <summary>
    /// Attempts to find an already-imported, unlinked transaction on the other side of a
    /// matching rule. <paramref name="consumedCandidateIds"/> is shared across a single
    /// import call so that multiple rows being matched in the same batch don't claim the
    /// same existing transaction twice; matched candidate ids must be added to it by the
    /// caller. The counterpart is searched on the user's account of the rule's other account type.
    /// </summary>
    Task<TransferMatchResult> TryMatchAsync(
        Account account,
        string description,
        decimal signedAmount,
        DateTime date,
        int userId,
        HashSet<int> consumedCandidateIds);
}
