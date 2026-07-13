using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class TransferMatchingService : ITransferMatchingService
{
    /// <summary>
    /// The inbound (positive-amount) leg of a matched transfer pair is always dated exactly
    /// one calendar day before the outbound (negative-amount) leg. Fixed by design, not
    /// configurable.
    /// </summary>
    private const int InboundOffsetDays = 1;

    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransferMatchingOptions _options;

    public TransferMatchingService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ITransferMatchingOptions options)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsTransferCandidate(string accountName, string description)
        => FindMatchingRules(accountName, description).Any();

    public async Task<TransferMatchResult> TryMatchAsync(
        string accountName,
        string description,
        decimal signedAmount,
        DateTime date,
        int userId,
        HashSet<int> consumedCandidateIds)
    {
        var matches = FindMatchingRules(accountName, description).ToList();
        if (matches.Count == 0)
            return new TransferMatchResult(false, null);

        var expectedOppositeAmount = -signedAmount;
        var isInbound = signedAmount > 0;
        var expectedDate = isInbound
            ? date.Date.AddDays(InboundOffsetDays)
            : date.Date.AddDays(-InboundOffsetDays);

        foreach (var match in matches)
        {
            var candidate = await ClaimCandidateAsync(
                match.OtherAccountName,
                expectedOppositeAmount,
                expectedDate,
                match.OtherDescriptionPattern,
                match.OtherMatchMode,
                userId,
                consumedCandidateIds);

            if (candidate != null)
                return new TransferMatchResult(true, candidate);
        }

        return new TransferMatchResult(true, null);
    }

    private async Task<Transaction?> ClaimCandidateAsync(
        string otherAccountName,
        decimal expectedOppositeAmount,
        DateTime expectedDate,
        string otherDescriptionPattern,
        DescriptionMatchMode otherMatchMode,
        int userId,
        HashSet<int> consumedCandidateIds)
    {
        var accounts = await _accountRepository.GetAccounts(userId);
        var otherAccount = accounts.FirstOrDefault(a =>
            a.Name.Equals(otherAccountName, StringComparison.OrdinalIgnoreCase));

        if (otherAccount == null)
            return null;

        var candidates = await _transactionRepository.GetUnlinkedTransferCandidates(
            otherAccount.Id, expectedOppositeAmount, expectedDate, otherDescriptionPattern);

        return candidates
            .Where(c => !consumedCandidateIds.Contains(c.Id))
            .Where(c => MatchesDescription(c.Description, otherDescriptionPattern, otherMatchMode))
            .OrderBy(c => c.Date)
            .ThenBy(c => c.Id)
            .FirstOrDefault();
    }

    private IEnumerable<(TransferMatchRule Rule, string OtherAccountName, string OtherDescriptionPattern, DescriptionMatchMode OtherMatchMode)>
        FindMatchingRules(string accountName, string description)
    {
        foreach (var rule in _options.Rules)
        {
            if (rule.AccountName1.Equals(accountName, StringComparison.OrdinalIgnoreCase) &&
                MatchesDescription(description, rule.DescriptionPattern1, rule.DescriptionMatchMode1))
            {
                yield return (rule, rule.AccountName2, rule.DescriptionPattern2, rule.DescriptionMatchMode2);
            }

            if (rule.AccountName2.Equals(accountName, StringComparison.OrdinalIgnoreCase) &&
                MatchesDescription(description, rule.DescriptionPattern2, rule.DescriptionMatchMode2))
            {
                yield return (rule, rule.AccountName1, rule.DescriptionPattern1, rule.DescriptionMatchMode1);
            }
        }
    }

    private static bool MatchesDescription(string? description, string pattern, DescriptionMatchMode mode)
    {
        if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(pattern))
            return false;

        return mode == DescriptionMatchMode.StartsWith
            ? description.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)
            : description.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
