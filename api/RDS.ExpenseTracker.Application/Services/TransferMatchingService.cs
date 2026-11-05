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
    public const int InboundOffsetDays = 1;

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

    public bool IsTransferCandidate(Account account, string description)
        => FindMatchingRules(account, description).Any();

    public IReadOnlyList<TransferRuleCounterpart> GetCounterpartRules(Account account, string description)
        => FindMatchingRules(account, description)
            .Select(m => new TransferRuleCounterpart(m.OtherAccountType, m.OtherDescriptionPattern, m.OtherMatchMode))
            .ToList();

    public async Task<TransferMatchResult> TryMatchAsync(
        Account account,
        string description,
        decimal signedAmount,
        DateTime date,
        int userId,
        HashSet<int> consumedCandidateIds)
    {
        var matches = FindMatchingRules(account, description).ToList();
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
                match.OtherAccountType,
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
        string otherAccountType,
        decimal expectedOppositeAmount,
        DateTime expectedDate,
        string otherDescriptionPattern,
        DescriptionMatchMode otherMatchMode,
        int userId,
        HashSet<int> consumedCandidateIds)
    {
        var accounts = await _accountRepository.GetAccounts(userId);
        var otherAccount = accounts.FirstOrDefault(a => HasAccountType(a, otherAccountType));

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

    /// <summary>True se il conto è del tipo indicato (per nome del tipo, es. "Satispay").</summary>
    public static bool HasAccountType(Account account, string accountTypeName)
        => account.AccountTypeNavigation is { } type
            && type.Name.Equals(accountTypeName, StringComparison.OrdinalIgnoreCase);

    private IEnumerable<(TransferMatchRule Rule, string OtherAccountType, string OtherDescriptionPattern, DescriptionMatchMode OtherMatchMode)>
        FindMatchingRules(Account account, string description)
    {
        ArgumentNullException.ThrowIfNull(account);

        foreach (var rule in _options.Rules)
        {
            if (HasAccountType(account, rule.AccountType1) &&
                MatchesDescription(description, rule.DescriptionPattern1, rule.DescriptionMatchMode1))
            {
                yield return (rule, rule.AccountType2, rule.DescriptionPattern2, rule.DescriptionMatchMode2);
            }

            if (HasAccountType(account, rule.AccountType2) &&
                MatchesDescription(description, rule.DescriptionPattern2, rule.DescriptionMatchMode2))
            {
                yield return (rule, rule.AccountType1, rule.DescriptionPattern1, rule.DescriptionMatchMode1);
            }
        }
    }

    private static bool MatchesDescription(string? description, string pattern, DescriptionMatchMode mode)
        => TransferMatchRule.MatchesDescription(description, pattern, mode);
}
