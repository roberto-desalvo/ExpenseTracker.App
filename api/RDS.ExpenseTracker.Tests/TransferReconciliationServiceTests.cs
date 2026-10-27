using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using System.Diagnostics;

namespace RDS.ExpenseTracker.Tests;

public class TransferReconciliationServiceTests
{
    private const int UserA = 1;
    private const int UserB = 2;
    private const int SellaId = 1;
    private const int SatispayId = 2;
    private const int TradeRepublicId = 3;
    private const int OtherUserAccountId = 10;
    private const string TradeRepublicIban = "IT60X0542811101000000123456";

    private static readonly TransferMatchRule SellaSatispayRule = new()
    {
        AccountName1 = "Sella",
        DescriptionPattern1 = "Satispay Europe",
        DescriptionMatchMode1 = DescriptionMatchMode.StartsWith,
        AccountName2 = "Satispay",
        DescriptionPattern2 = "Ricarica Satispay",
    };

    private static List<Account> DefaultAccounts() =>
    [
        new Account(SellaId, "Sella", UserA),
        new Account(SatispayId, "Satispay", UserA),
        new Account(TradeRepublicId, "Trade Republic", UserA) { Iban = TradeRepublicIban },
        new Account(OtherUserAccountId, "Sella", UserB),
    ];

    [Fact]
    public async Task ReconcileAsync_LinksPairByRule_WhenDescriptionsMatchConfiguredRule()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            Tx(10, SellaId, -50m, new DateTime(2026, 6, 17), "Satispay Europe S.A. - Ricarica wallet", CategoryEnum.Default),
            Tx(11, SatispayId, 50m, new DateTime(2026, 6, 16, 9, 0, 0), "Ricarica Satispay", CategoryEnum.Default),
        ]);
        var service = BuildService(repository, rules: [SellaSatispayRule]);

        var result = await service.ReconcileAsync(null, null, dryRun: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.MatchedByRule.Should().Be(1);
        var pair = result.Value.Pairs.Should().ContainSingle().Subject;
        pair.UserId.Should().Be(UserA);
        pair.OutboundTransactionId.Should().Be(10);
        pair.InboundTransactionId.Should().Be(11);
        pair.FromAccount.Should().Be("Sella");
        pair.ToAccount.Should().Be("Satispay");

        repository.LinkedPairs.Should().Equal((10, 11));
        repository.LinkedCategoryIds.Should().OnlyContain(id => id == (int)CategoryEnum.MoneyTransfers);
    }

    [Fact]
    public async Task ReconcileAsync_DoesNotUseRule_WhenDateOffsetIsNotOneDay()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            Tx(10, SellaId, -50m, new DateTime(2026, 6, 17), "Satispay Europe S.A. - Ricarica wallet", CategoryEnum.Default),
            Tx(11, SatispayId, 50m, new DateTime(2026, 6, 17), "Ricarica Satispay", CategoryEnum.Default),
        ]);
        var service = BuildService(repository, rules: [SellaSatispayRule]);

        var result = await service.ReconcileAsync(null, null, dryRun: false);

        result.Value.MatchedByRule.Should().Be(0);
    }

    [Fact]
    public async Task ReconcileAsync_LinksPairByIban_WhenDescriptionContainsIbanOfAnotherAccount()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            Tx(20, SellaId, -100m, new DateTime(2026, 6, 10), $"Bonifico a favore di Roberto {TradeRepublicIban}", CategoryEnum.Default),
            Tx(21, TradeRepublicId, 100m, new DateTime(2026, 6, 12), "Incoming transfer", CategoryEnum.Default),
        ]);
        var service = BuildService(repository);

        var result = await service.ReconcileAsync(null, null, dryRun: false);

        result.Value.MatchedByIban.Should().Be(1);
        result.Value.Pairs.Should().ContainSingle(p => p.Method == "Iban" && p.OutboundTransactionId == 20 && p.InboundTransactionId == 21);
        repository.LinkedPairs.Should().Equal((20, 21));
    }

    [Fact]
    public async Task ReconcileAsync_LinksUnmatchedMoneyTransferLeg_ByCategory_PreferringMoneyTransferCounterpart()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            Tx(30, SellaId, -75m, new DateTime(2026, 6, 20), "Giroconto", CategoryEnum.MoneyTransfers),
            Tx(31, SatispayId, 75m, new DateTime(2026, 6, 18), "Ricarica", CategoryEnum.MoneyTransfers),
            Tx(32, TradeRepublicId, 75m, new DateTime(2026, 6, 20), "Rimborso", CategoryEnum.Default),
        ]);
        var service = BuildService(repository);

        var result = await service.ReconcileAsync(null, null, dryRun: false);

        result.Value.MatchedByCategory.Should().Be(1);
        repository.LinkedPairs.Should().Equal((30, 31));
    }

    [Fact]
    public async Task ReconcileAsync_SkipsAmbiguousMoneyTransferLeg()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            Tx(40, SellaId, -20m, new DateTime(2026, 6, 5), "Giroconto", CategoryEnum.MoneyTransfers),
            Tx(41, SatispayId, 20m, new DateTime(2026, 6, 5), "Entrata", CategoryEnum.Default),
            Tx(42, TradeRepublicId, 20m, new DateTime(2026, 6, 5), "Entrata", CategoryEnum.Default),
        ]);
        var service = BuildService(repository);

        var result = await service.ReconcileAsync(null, null, dryRun: false);

        result.Value.LinkedTransfers.Should().Be(0);
        result.Value.AmbiguousSkipped.Should().Be(1);
        repository.LinkedPairs.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAsync_IgnoresCounterparts_OnSameAccountOrBeyondDateTolerance()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            Tx(50, SellaId, -30m, new DateTime(2026, 6, 1), "Giroconto", CategoryEnum.MoneyTransfers),
            Tx(51, SellaId, 30m, new DateTime(2026, 6, 1), "Storno", CategoryEnum.MoneyTransfers),
            Tx(52, SatispayId, 30m, new DateTime(2026, 6, 5), "Ricarica", CategoryEnum.MoneyTransfers),
        ]);
        var service = BuildService(repository);

        var result = await service.ReconcileAsync(null, null, dryRun: false);

        result.Value.LinkedTransfers.Should().Be(0);
    }

    [Fact]
    public async Task ReconcileAsync_ProcessesAllUsers_WithoutMatchingAcrossUsers()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            // stesso importo e stessa data, ma il conto di arrivo è di un altro utente
            Tx(60, SellaId, -40m, new DateTime(2026, 6, 3), "Giroconto", CategoryEnum.MoneyTransfers),
            Tx(61, OtherUserAccountId, 40m, new DateTime(2026, 6, 3), "Giroconto", CategoryEnum.MoneyTransfers),
        ]);
        var service = BuildService(repository);

        var result = await service.ReconcileAsync(null, null, dryRun: false);

        result.Value.UsersProcessed.Should().Be(2);
        result.Value.ExaminedTransactions.Should().Be(2);
        result.Value.LinkedTransfers.Should().Be(0);
    }

    [Fact]
    public async Task ReconcileAsync_DryRun_ReturnsMatchesWithoutLinking()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(),
        [
            Tx(70, SellaId, -75m, new DateTime(2026, 6, 20), "Giroconto", CategoryEnum.MoneyTransfers),
            Tx(71, SatispayId, 75m, new DateTime(2026, 6, 20), "Ricarica", CategoryEnum.Default),
        ]);
        var service = BuildService(repository);

        var result = await service.ReconcileAsync(null, null, dryRun: true);

        result.Value.DryRun.Should().BeTrue();
        result.Value.LinkedTransfers.Should().Be(1);
        repository.LinkedPairs.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAsync_PassesInclusiveEndOfDay_WhenToDateHasNoTime()
    {
        var repository = new FakeReconciliationRepository(DefaultAccounts(), []);
        var service = BuildService(repository);

        await service.ReconcileAsync(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), dryRun: true);

        repository.LastToDate.Should().Be(new DateTime(2026, 6, 30).AddDays(1).AddTicks(-1));
    }

    [Fact]
    public async Task ReconcileAsync_Fails_WhenFromDateAfterToDate()
    {
        var service = BuildService(new FakeReconciliationRepository(DefaultAccounts(), []));

        var result = await service.ReconcileAsync(new DateTime(2026, 7, 1), new DateTime(2026, 6, 1), dryRun: true);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task ReconcileAsync_HandlesLargeVolumes()
    {
        // 20.000 coppie "Money transfers" + 20.000 movimenti non correlati: il matching è indicizzato, non quadratico
        var transactions = new List<Transaction>();
        var start = new DateTime(2020, 1, 1);
        var id = 1;
        for (var i = 0; i < 20_000; i++)
        {
            var date = start.AddDays(i / 10);
            var amount = 10m + i;
            transactions.Add(Tx(id++, SellaId, -amount, date, "Giroconto", CategoryEnum.MoneyTransfers));
            transactions.Add(Tx(id++, SatispayId, amount, date.AddDays(1), "Ricarica", CategoryEnum.MoneyTransfers));
            transactions.Add(Tx(id++, TradeRepublicId, -(amount + 0.5m), date, "Spesa", CategoryEnum.Default));
            transactions.Add(Tx(id++, SellaId, amount + 0.5m, date, "Entrata", CategoryEnum.Default));
        }

        var repository = new FakeReconciliationRepository(DefaultAccounts(),
            transactions.OrderBy(t => t.Date).ThenBy(t => t.Id).ToList());
        var service = BuildService(repository, rules: [SellaSatispayRule]);

        var stopwatch = Stopwatch.StartNew();
        var result = await service.ReconcileAsync(null, null, dryRun: true);
        stopwatch.Stop();

        result.Value.LinkedTransfers.Should().Be(20_000);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    private static Transaction Tx(int id, int accountId, decimal amount, DateTime date, string description, CategoryEnum category)
        => new()
        {
            Id = id,
            AccountId = accountId,
            Amount = amount,
            Date = date,
            Description = description,
            CategoryId = (int)category,
        };

    private static TransferReconciliationService BuildService(
        FakeReconciliationRepository repository,
        IEnumerable<TransferMatchRule>? rules = null)
    {
        var matchingService = new TransferMatchingService(
            new UnusedTransactionRepository(),
            new UnusedAccountRepository(),
            new FakeTransferMatchingOptions { Rules = rules?.ToList() ?? [] });

        return new TransferReconciliationService(
            repository,
            matchingService,
            NullLogger<TransferReconciliationService>.Instance);
    }

    private sealed class FakeTransferMatchingOptions : ITransferMatchingOptions
    {
        public List<TransferMatchRule> Rules { get; set; } = [];
    }

    private sealed class FakeReconciliationRepository(List<Account> accounts, List<Transaction> transactions) : ITransferReconciliationRepository
    {
        public List<(int, int)> LinkedPairs { get; } = [];
        public List<int> LinkedCategoryIds { get; } = [];
        public DateTime? LastToDate { get; private set; }

        public Task<List<Account>> GetAllAccounts() => Task.FromResult(accounts.ToList());

        public Task<List<Transaction>> GetUnlinkedTransactions(IReadOnlyCollection<int> accountIds, DateTime? fromDate, DateTime? toDate)
        {
            LastToDate = toDate;
            return Task.FromResult(transactions
                .Where(t => accountIds.Contains(t.AccountId))
                .Where(t => !fromDate.HasValue || t.Date >= fromDate)
                .Where(t => !toDate.HasValue || t.Date <= toDate)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .ToList());
        }

        public Task LinkTransfers(IReadOnlyCollection<(int OutboundTransactionId, int InboundTransactionId)> pairs, int categoryId)
        {
            LinkedPairs.AddRange(pairs.Select(p => (p.OutboundTransactionId, p.InboundTransactionId)));
            LinkedCategoryIds.Add(categoryId);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    // La riconciliazione usa solo il matching in memoria delle regole: le dipendenze DB del matching service non servono.
    private sealed class UnusedTransactionRepository : ITransactionRepository
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Transaction?> GetTransaction(int id, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactions(int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Spent, decimal Earned)>> GetAccountMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId) => throw new NotImplementedException();
        public Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId) => throw new NotImplementedException();
        public Task<Transaction> GetLatestTransaction(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds, int userId) => throw new NotImplementedException();
        public Task<List<Transaction>> GetCategorizableTransactions(int userId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains) => throw new NotImplementedException();
        public Task AddTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task DeleteAllTransactions(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions, int userId) => throw new NotImplementedException();
    }

    private sealed class UnusedAccountRepository : IAccountRepository
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<Account>> GetAccounts(int userId) => throw new NotImplementedException();
        public Task AddAccounts(IEnumerable<Account> accounts) => throw new NotImplementedException();
        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task DeleteAccounts(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id, int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }
}
