using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Application.Utilities;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

/// <summary>
/// Collega a posteriori i movimenti già importati che rappresentano le due metà di un trasferimento
/// (es. import avvenuto senza IBAN configurati sui conti). Applica, in ordine di affidabilità:
/// <list type="number">
/// <item>Rule: le regole di <see cref="ITransferMatchingService"/>, come durante l'import;</item>
/// <item>Iban: la descrizione di un movimento contiene l'IBAN di un altro conto dell'utente;</item>
/// <item>Category: un movimento in "Money transfers" rimasto senza controparte.</item>
/// </list>
/// Per Iban e Category la controparte deve stare su un altro conto, con importo opposto e data entro
/// <see cref="DateToleranceDays"/> giorni.
/// </summary>
public class TransferReconciliationService : ITransferReconciliationService
{
    // Le banche registrano lo stesso bonifico con qualche giorno di scarto (data operazione vs valuta).
    private const int DateToleranceDays = 3;

    private const int MoneyTransfersCategoryId = (int)CategoryEnum.MoneyTransfers;

    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransferMatchingService _transferMatchingService;
    private readonly ILogger<TransferReconciliationService> _logger;

    public TransferReconciliationService(
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        IAccountRepository accountRepository,
        ITransferMatchingService transferMatchingService,
        ILogger<TransferReconciliationService> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository = transferRepository ?? throw new ArgumentNullException(nameof(transferRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transferMatchingService = transferMatchingService ?? throw new ArgumentNullException(nameof(transferMatchingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TransferReconciliationResultDto>> ReconcileAsync(int userId, DateTime? fromDate, DateTime? toDate, bool dryRun)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            return Result.Fail(DomainErrors.BadRequest("toDate must be greater than fromDate"));

        // una data senza orario (es. 2026-10-31) include tutto quel giorno
        if (toDate.HasValue && toDate.Value.TimeOfDay == TimeSpan.Zero)
            toDate = toDate.Value.AddDays(1).AddTicks(-1);

        var accounts = (await _accountRepository.GetAccounts(userId)).ToDictionary(a => a.Id);
        var unlinked = (await _transactionRepository.GetUnlinkedTransactions(userId, fromDate, toDate))
            .Where(t => t.TransferId == null && t.Date.HasValue && t.Amount != 0m && accounts.ContainsKey(t.AccountId))
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToList();

        var consumed = new HashSet<int>();
        var pairs = new List<MatchedPair>();
        var ambiguous = 0;

        // 1. Regole configurate (stesso comportamento dell'import, controparte cercata a DB)
        foreach (var tx in unlinked)
        {
            if (consumed.Contains(tx.Id))
                continue;

            var accountName = accounts[tx.AccountId].Name;
            if (!_transferMatchingService.IsTransferCandidate(accountName, tx.Description))
                continue;

            consumed.Add(tx.Id);
            var match = await _transferMatchingService.TryMatchAsync(
                accountName, tx.Description, tx.Amount, tx.Date!.Value, userId, consumed);

            if (match.Candidate is { TransferId: null } candidate && candidate.Id != tx.Id)
                pairs.Add(Pair(tx, candidate, "Rule", consumed));
            else
                consumed.Remove(tx.Id);
        }

        // 2. IBAN di un altro conto dell'utente presente nella descrizione
        foreach (var tx in unlinked)
        {
            if (consumed.Contains(tx.Id))
                continue;

            var counterpartAccountIds = IbanUtilities.ExtractAll(tx.Description)
                .Select(iban => IbanUtilities.FindAccount(accounts.Values, iban))
                .Where(a => a is not null && a.Id != tx.AccountId)
                .Select(a => a!.Id)
                .ToHashSet();

            if (counterpartAccountIds.Count == 0)
                continue;

            var candidate = FindCounterparts(tx, unlinked, consumed)
                .Where(c => counterpartAccountIds.Contains(c.AccountId))
                .OrderBy(c => DaysBetween(tx, c))
                .ThenBy(c => c.Id)
                .FirstOrDefault();

            if (candidate is not null)
                pairs.Add(Pair(tx, candidate, "Iban", consumed));
        }

        // 3. Movimenti "Money transfers" rimasti senza controparte
        foreach (var tx in unlinked)
        {
            if (consumed.Contains(tx.Id) || tx.CategoryId != MoneyTransfersCategoryId)
                continue;

            var ranked = FindCounterparts(tx, unlinked, consumed)
                .Select(c => (Candidate: c, CategoryRank: c.CategoryId == MoneyTransfersCategoryId ? 0 : 1, Days: DaysBetween(tx, c)))
                .OrderBy(x => x.CategoryRank)
                .ThenBy(x => x.Days)
                .ThenBy(x => x.Candidate.Id)
                .ToList();

            if (ranked.Count == 0)
                continue;

            var best = ranked[0];
            if (ranked.Count > 1 && ranked[1].CategoryRank == best.CategoryRank && ranked[1].Days == best.Days)
            {
                ambiguous++;
                _logger.LogInformation(
                    "Transaction {TransactionId} has several equivalent transfer counterparts, left unlinked", tx.Id);
                continue;
            }

            pairs.Add(Pair(tx, best.Candidate, "Category", consumed));
        }

        if (!dryRun && pairs.Count > 0)
        {
            foreach (var pair in pairs)
                await _transferRepository.AddTransfer(Link(pair));

            await _transferRepository.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Transfer reconciliation for user {UserId}: {Linked} transfers found among {Examined} unlinked transactions (DryRun={DryRun})",
            userId, pairs.Count, unlinked.Count, dryRun);

        return Result.Ok(new TransferReconciliationResultDto
        {
            DryRun = dryRun,
            ExaminedTransactions = unlinked.Count,
            LinkedTransfers = pairs.Count,
            MatchedByRule = pairs.Count(p => p.Method == "Rule"),
            MatchedByIban = pairs.Count(p => p.Method == "Iban"),
            MatchedByCategory = pairs.Count(p => p.Method == "Category"),
            AmbiguousSkipped = ambiguous,
            Pairs = pairs.Select(p => new TransferReconciliationPairDto
            {
                OutboundTransactionId = p.Outbound.Id,
                InboundTransactionId = p.Inbound.Id,
                FromAccount = accounts.TryGetValue(p.Outbound.AccountId, out var from) ? from.Name : string.Empty,
                ToAccount = accounts.TryGetValue(p.Inbound.AccountId, out var to) ? to.Name : string.Empty,
                Amount = p.Inbound.Amount,
                OutboundDate = p.Outbound.Date,
                InboundDate = p.Inbound.Date,
                Method = p.Method,
            }).ToList(),
        });
    }

    // Controparti possibili: altro conto, importo opposto, data entro la tolleranza, non ancora usate.
    private static IEnumerable<Transaction> FindCounterparts(Transaction tx, IEnumerable<Transaction> unlinked, HashSet<int> consumed)
        => unlinked.Where(c =>
            c.Id != tx.Id &&
            !consumed.Contains(c.Id) &&
            c.AccountId != tx.AccountId &&
            c.Amount == -tx.Amount &&
            DaysBetween(tx, c) <= DateToleranceDays);

    private static int DaysBetween(Transaction a, Transaction b)
        => Math.Abs((a.Date!.Value.Date - b.Date!.Value.Date).Days);

    private static MatchedPair Pair(Transaction tx, Transaction counterpart, string method, HashSet<int> consumed)
    {
        consumed.Add(tx.Id);
        consumed.Add(counterpart.Id);

        return tx.Amount < 0
            ? new MatchedPair(tx, counterpart, method)
            : new MatchedPair(counterpart, tx, method);
    }

    private static Transfer Link(MatchedPair pair)
    {
        var now = DateTime.UtcNow;
        var transfer = new Transfer { CreatedOn = now };

        foreach (var leg in new[] { pair.Outbound, pair.Inbound })
        {
            leg.TransferNavigation = transfer;
            leg.CategoryId = MoneyTransfersCategoryId;
            leg.UpdatedOn = now;
        }

        return transfer;
    }

    private sealed record MatchedPair(Transaction Outbound, Transaction Inbound, string Method);
}
