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
/// Collega a posteriori, per tutti gli utenti, i movimenti già importati che rappresentano le due metà di un
/// trasferimento (es. import avvenuti senza IBAN sui conti). Le coppie si cercano solo tra i conti dello stesso
/// utente. Applica, in ordine di affidabilità:
/// <list type="number">
/// <item>Rule: le regole di <see cref="ITransferMatchingService"/>, come durante l'import;</item>
/// <item>Iban: la descrizione di un movimento contiene l'IBAN di un altro conto dell'utente;</item>
/// <item>Category: un movimento in "Money transfers" rimasto senza controparte.</item>
/// </list>
/// Per Iban e Category la controparte deve stare su un altro conto, con importo opposto e data entro
/// <see cref="DateToleranceDays"/> giorni.
/// <para>
/// Prestazioni: una query senza tracking per utente, matching interamente in memoria su un indice
/// (conto, importo) → movimenti ordinati per data, scrittura dei collegamenti a blocchi.
/// </para>
/// </summary>
public class TransferReconciliationService : ITransferReconciliationService
{
    // Le banche registrano lo stesso bonifico con qualche giorno di scarto (data operazione vs valuta).
    private const int DateToleranceDays = 3;

    private const int MoneyTransfersCategoryId = (int)CategoryEnum.MoneyTransfers;

    private readonly ITransferReconciliationRepository _repository;
    private readonly ITransferMatchingService _transferMatchingService;
    private readonly ILogger<TransferReconciliationService> _logger;

    public TransferReconciliationService(
        ITransferReconciliationRepository repository,
        ITransferMatchingService transferMatchingService,
        ILogger<TransferReconciliationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _transferMatchingService = transferMatchingService ?? throw new ArgumentNullException(nameof(transferMatchingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TransferReconciliationResultDto>> ReconcileAsync(DateTime? fromDate, DateTime? toDate, bool dryRun)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            return Result.Fail(DomainErrors.BadRequest("toDate must be greater than fromDate"));

        // una data senza orario (es. 2026-10-31) include tutto quel giorno
        if (toDate.HasValue && toDate.Value.TimeOfDay == TimeSpan.Zero)
            toDate = toDate.Value.AddDays(1).AddTicks(-1);

        var accountsByUser = (await _repository.GetAllAccounts())
            .GroupBy(a => a.UserId)
            .OrderBy(g => g.Key);

        var result = new TransferReconciliationResultDto { DryRun = dryRun };
        var pairs = new List<TransferReconciliationPairDto>();

        // un utente alla volta: i trasferimenti sono sempre tra conti dello stesso utente,
        // e così la memoria resta proporzionale ai movimenti di un solo utente
        foreach (var userAccounts in accountsByUser)
        {
            var accounts = userAccounts.ToDictionary(a => a.Id);
            var unlinked = await _repository.GetUnlinkedTransactions(accounts.Keys.ToList(), fromDate, toDate);

            result.UsersProcessed++;
            result.ExaminedTransactions += unlinked.Count;

            if (unlinked.Count < 2)
                continue;

            var userMatcher = new UserMatcher(accounts, unlinked, _transferMatchingService);
            var userPairs = userMatcher.Match();
            result.AmbiguousSkipped += userMatcher.Ambiguous;

            if (userPairs.Count == 0)
                continue;

            if (!dryRun)
                await _repository.LinkTransfers(userPairs.Select(p => (p.Outbound.Id, p.Inbound.Id)).ToList(), MoneyTransfersCategoryId);

            pairs.AddRange(userPairs.Select(p => new TransferReconciliationPairDto
            {
                UserId = userAccounts.Key,
                OutboundTransactionId = p.Outbound.Id,
                InboundTransactionId = p.Inbound.Id,
                FromAccount = accounts[p.Outbound.AccountId].Name,
                ToAccount = accounts[p.Inbound.AccountId].Name,
                Amount = p.Inbound.Amount,
                OutboundDate = p.Outbound.Date,
                InboundDate = p.Inbound.Date,
                Method = p.Method,
            }));
        }

        result.Pairs = pairs;
        result.LinkedTransfers = pairs.Count;
        result.MatchedByRule = pairs.Count(p => p.Method == UserMatcher.RuleMethod);
        result.MatchedByIban = pairs.Count(p => p.Method == UserMatcher.IbanMethod);
        result.MatchedByCategory = pairs.Count(p => p.Method == UserMatcher.CategoryMethod);

        _logger.LogInformation(
            "Transfer reconciliation: {Linked} transfers found among {Examined} unlinked transactions of {Users} users (DryRun={DryRun}, Ambiguous={Ambiguous})",
            result.LinkedTransfers, result.ExaminedTransactions, result.UsersProcessed, dryRun, result.AmbiguousSkipped);

        return Result.Ok(result);
    }

    private sealed record MatchedPair(Transaction Outbound, Transaction Inbound, string Method);

    /// <summary>Matching in memoria sui movimenti non collegati di un singolo utente.</summary>
    private sealed class UserMatcher
    {
        public const string RuleMethod = "Rule";
        public const string IbanMethod = "Iban";
        public const string CategoryMethod = "Category";

        private readonly Dictionary<int, Account> _accounts;
        private readonly Dictionary<string, Account> _accountsByName;
        private readonly List<Transaction> _transactions;
        private readonly ITransferMatchingService _matchingService;

        // (conto, importo) → movimenti in ordine di data e id: la controparte di x si cerca in (altro conto, -x.Amount)
        private readonly Dictionary<(int AccountId, decimal Amount), List<Transaction>> _index;
        private readonly HashSet<int> _consumed = [];
        private readonly List<MatchedPair> _pairs = [];

        public int Ambiguous { get; private set; }

        public UserMatcher(Dictionary<int, Account> accounts, List<Transaction> transactions, ITransferMatchingService matchingService)
        {
            _accounts = accounts;
            _transactions = transactions;
            _matchingService = matchingService;

            _accountsByName = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase);
            foreach (var account in accounts.Values.OrderBy(a => a.Id))
                _accountsByName.TryAdd(account.Name, account);

            // le transazioni arrivano già ordinate per Date, Id: le liste dell'indice ereditano l'ordine
            _index = transactions
                .GroupBy(t => (t.AccountId, t.Amount))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public List<MatchedPair> Match()
        {
            MatchByRule();
            MatchByIban();
            MatchByCategory();
            return _pairs;
        }

        // 1. Regole configurate: stesso conto/descrizione attesi e offset fisso di un giorno usati dall'import
        private void MatchByRule()
        {
            foreach (var tx in _transactions)
            {
                if (_consumed.Contains(tx.Id))
                    continue;

                var rules = _matchingService.GetCounterpartRules(_accounts[tx.AccountId].Name, tx.Description);
                if (rules.Count == 0)
                    continue;

                var expectedDate = tx.Amount > 0
                    ? tx.Date!.Value.Date.AddDays(TransferMatchingService.InboundOffsetDays)
                    : tx.Date!.Value.Date.AddDays(-TransferMatchingService.InboundOffsetDays);

                foreach (var rule in rules)
                {
                    if (!_accountsByName.TryGetValue(rule.AccountName, out var otherAccount) || otherAccount.Id == tx.AccountId)
                        continue;

                    var candidate = Bucket(otherAccount.Id, -tx.Amount).FirstOrDefault(c =>
                        !_consumed.Contains(c.Id) &&
                        c.Date!.Value.Date == expectedDate &&
                        rule.Matches(c.Description));

                    if (candidate is not null)
                    {
                        AddPair(tx, candidate, RuleMethod);
                        break;
                    }
                }
            }
        }

        // 2. IBAN di un altro conto dell'utente presente nella descrizione
        private void MatchByIban()
        {
            if (!_accounts.Values.Any(a => a.Iban is not null))
                return;

            foreach (var tx in _transactions)
            {
                if (_consumed.Contains(tx.Id))
                    continue;

                var counterpartAccountIds = IbanUtilities.ExtractAll(tx.Description)
                    .Select(iban => IbanUtilities.FindAccount(_accounts.Values, iban))
                    .Where(a => a is not null && a.Id != tx.AccountId)
                    .Select(a => a!.Id)
                    .Distinct()
                    .ToList();

                if (counterpartAccountIds.Count == 0)
                    continue;

                var candidate = Counterparts(tx, counterpartAccountIds)
                    .OrderBy(c => DaysBetween(tx, c))
                    .ThenBy(c => c.Id)
                    .FirstOrDefault();

                if (candidate is not null)
                    AddPair(tx, candidate, IbanMethod);
            }
        }

        // 3. Movimenti "Money transfers" rimasti senza controparte, verso qualsiasi altro conto dell'utente
        private void MatchByCategory()
        {
            foreach (var tx in _transactions)
            {
                if (_consumed.Contains(tx.Id) || tx.CategoryId != MoneyTransfersCategoryId)
                    continue;

                var otherAccountIds = _accounts.Keys.Where(id => id != tx.AccountId).ToList();
                var ranked = Counterparts(tx, otherAccountIds)
                    .Select(c => (Candidate: c, CategoryRank: c.CategoryId == MoneyTransfersCategoryId ? 0 : 1, Days: DaysBetween(tx, c)))
                    .OrderBy(x => x.CategoryRank)
                    .ThenBy(x => x.Days)
                    .ThenBy(x => x.Candidate.Id)
                    .Take(2)
                    .ToList();

                if (ranked.Count == 0)
                    continue;

                var best = ranked[0];
                if (ranked.Count > 1 && ranked[1].CategoryRank == best.CategoryRank && ranked[1].Days == best.Days)
                {
                    Ambiguous++;
                    continue;
                }

                AddPair(tx, best.Candidate, CategoryMethod);
            }
        }

        private List<Transaction> Bucket(int accountId, decimal amount)
            => _index.TryGetValue((accountId, amount), out var list) ? list : [];

        // Controparti possibili sui conti indicati: importo opposto, data entro la tolleranza, non ancora usate.
        private IEnumerable<Transaction> Counterparts(Transaction tx, IEnumerable<int> accountIds)
            => accountIds
                .SelectMany(accountId => Bucket(accountId, -tx.Amount))
                .Where(c => !_consumed.Contains(c.Id) && DaysBetween(tx, c) <= DateToleranceDays);

        private void AddPair(Transaction tx, Transaction counterpart, string method)
        {
            _consumed.Add(tx.Id);
            _consumed.Add(counterpart.Id);

            _pairs.Add(tx.Amount < 0
                ? new MatchedPair(tx, counterpart, method)
                : new MatchedPair(counterpart, tx, method));
        }

        private static int DaysBetween(Transaction a, Transaction b)
            => Math.Abs((a.Date!.Value.Date - b.Date!.Value.Date).Days);
    }
}
