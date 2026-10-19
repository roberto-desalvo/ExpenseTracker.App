using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class TransferReconciliationServiceTests
{
    private const int TestUserId = 1;
    private const int SellaId = 1;
    private const int SatispayId = 2;
    private const int TradeRepublicId = 3;
    private const string TradeRepublicIban = "IT60X0542811101000000123456";

    private static readonly TransferMatchRule SellaSatispayRule = new()
    {
        AccountName1 = "Sella",
        DescriptionPattern1 = "Satispay Europe",
        DescriptionMatchMode1 = DescriptionMatchMode.StartsWith,
        AccountName2 = "Satispay",
        DescriptionPattern2 = "Ricarica Satispay",
    };

    [Fact]
    public async Task ReconcileAsync_LinksPairByRule_WhenDescriptionsMatchConfiguredRule()
    {
        var sella = Tx(10, SellaId, -50m, new DateTime(2026, 6, 17), "Satispay Europe S.A. - Ricarica wallet", CategoryEnum.Default);
        var satispay = Tx(11, SatispayId, 50m, new DateTime(2026, 6, 16, 9, 0, 0), "Ricarica Satispay", CategoryEnum.Default);
        var service = BuildService(out var transferRepository, [sella, satispay], rules: [SellaSatispayRule]);

        var result = await service.ReconcileAsync(TestUserId, null, null, dryRun: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.MatchedByRule.Should().Be(1);
        var pair = result.Value.Pairs.Should().ContainSingle().Subject;
        pair.OutboundTransactionId.Should().Be(10);
        pair.InboundTransactionId.Should().Be(11);
        pair.FromAccount.Should().Be("Sella");
        pair.ToAccount.Should().Be("Satispay");

        transferRepository.AddedTransfers.Should().ContainSingle();
        sella.TransferNavigation.Should().NotBeNull().And.BeSameAs(satispay.TransferNavigation);
        sella.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
        satispay.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
    }

    [Fact]
    public async Task ReconcileAsync_LinksPairByIban_WhenDescriptionContainsIbanOfAnotherAccount()
    {
        var sella = Tx(20, SellaId, -100m, new DateTime(2026, 6, 10), $"Bonifico a favore di Roberto {TradeRepublicIban}", CategoryEnum.Default);
        var tradeRepublic = Tx(21, TradeRepublicId, 100m, new DateTime(2026, 6, 12), "Incoming transfer", CategoryEnum.Default);
        var service = BuildService(out var transferRepository, [sella, tradeRepublic]);

        var result = await service.ReconcileAsync(TestUserId, null, null, dryRun: false);

        result.Value.MatchedByIban.Should().Be(1);
        result.Value.Pairs.Should().ContainSingle(p => p.Method == "Iban" && p.OutboundTransactionId == 20 && p.InboundTransactionId == 21);
        transferRepository.AddedTransfers.Should().ContainSingle();
        tradeRepublic.TransferNavigation.Should().BeSameAs(sella.TransferNavigation);
    }

    [Fact]
    public async Task ReconcileAsync_LinksUnmatchedMoneyTransferLeg_ByCategory()
    {
        var sella = Tx(30, SellaId, -75m, new DateTime(2026, 6, 20), "Giroconto", CategoryEnum.MoneyTransfers);
        var satispay = Tx(31, SatispayId, 75m, new DateTime(2026, 6, 18), "Ricarica", CategoryEnum.MoneyTransfers);
        var unrelated = Tx(32, TradeRepublicId, 75m, new DateTime(2026, 6, 20), "Rimborso", CategoryEnum.Default);
        var service = BuildService(out _, [sella, satispay, unrelated]);

        var result = await service.ReconcileAsync(TestUserId, null, null, dryRun: false);

        // a parità di importo vince la controparte già in "Money transfers", anche se con data più distante
        result.Value.MatchedByCategory.Should().Be(1);
        result.Value.Pairs.Should().ContainSingle(p => p.OutboundTransactionId == 30 && p.InboundTransactionId == 31);
        unrelated.TransferNavigation.Should().BeNull();
    }

    [Fact]
    public async Task ReconcileAsync_SkipsAmbiguousMoneyTransferLeg()
    {
        var sella = Tx(40, SellaId, -20m, new DateTime(2026, 6, 5), "Giroconto", CategoryEnum.MoneyTransfers);
        var satispay = Tx(41, SatispayId, 20m, new DateTime(2026, 6, 5), "Entrata", CategoryEnum.Default);
        var tradeRepublic = Tx(42, TradeRepublicId, 20m, new DateTime(2026, 6, 5), "Entrata", CategoryEnum.Default);
        var service = BuildService(out var transferRepository, [sella, satispay, tradeRepublic]);

        var result = await service.ReconcileAsync(TestUserId, null, null, dryRun: false);

        result.Value.LinkedTransfers.Should().Be(0);
        result.Value.AmbiguousSkipped.Should().Be(1);
        transferRepository.AddedTransfers.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAsync_IgnoresCounterparts_OnSameAccountOrBeyondDateTolerance()
    {
        var outbound = Tx(50, SellaId, -30m, new DateTime(2026, 6, 1), "Giroconto", CategoryEnum.MoneyTransfers);
        var sameAccount = Tx(51, SellaId, 30m, new DateTime(2026, 6, 1), "Storno", CategoryEnum.MoneyTransfers);
        var tooLate = Tx(52, SatispayId, 30m, new DateTime(2026, 6, 5), "Ricarica", CategoryEnum.MoneyTransfers);
        var service = BuildService(out _, [outbound, sameAccount, tooLate]);

        var result = await service.ReconcileAsync(TestUserId, null, null, dryRun: false);

        result.Value.LinkedTransfers.Should().Be(0);
    }

    [Fact]
    public async Task ReconcileAsync_DryRun_ReturnsMatchesWithoutLinking()
    {
        var sella = Tx(60, SellaId, -75m, new DateTime(2026, 6, 20), "Giroconto", CategoryEnum.MoneyTransfers);
        var satispay = Tx(61, SatispayId, 75m, new DateTime(2026, 6, 20), "Ricarica", CategoryEnum.Default);
        var service = BuildService(out var transferRepository, [sella, satispay]);

        var result = await service.ReconcileAsync(TestUserId, null, null, dryRun: true);

        result.Value.DryRun.Should().BeTrue();
        result.Value.LinkedTransfers.Should().Be(1);
        transferRepository.AddedTransfers.Should().BeEmpty();
        transferRepository.SaveCount.Should().Be(0);
        sella.TransferNavigation.Should().BeNull();
        satispay.CategoryId.Should().Be((int)CategoryEnum.Default);
    }

    [Fact]
    public async Task ReconcileAsync_Fails_WhenFromDateAfterToDate()
    {
        var service = BuildService(out _, []);

        var result = await service.ReconcileAsync(TestUserId, new DateTime(2026, 7, 1), new DateTime(2026, 6, 1), dryRun: true);

        result.IsFailed.Should().BeTrue();
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
        out FakeTransferRepository transferRepository,
        IEnumerable<Transaction> transactions,
        IEnumerable<TransferMatchRule>? rules = null)
    {
        var transactionRepository = new FakeTransactionRepository(transactions);
        transferRepository = new FakeTransferRepository();
        var accountRepository = new FakeAccountRepository([
            new Account(SellaId, "Sella", TestUserId),
            new Account(SatispayId, "Satispay", TestUserId),
            new Account(TradeRepublicId, "Trade Republic", TestUserId) { Iban = TradeRepublicIban },
        ]);

        var matchingService = new TransferMatchingService(
            transactionRepository,
            accountRepository,
            new FakeTransferMatchingOptions { Rules = rules?.ToList() ?? [] });

        return new TransferReconciliationService(
            transactionRepository,
            transferRepository,
            accountRepository,
            matchingService,
            NullLogger<TransferReconciliationService>.Instance);
    }

    private sealed class FakeTransferMatchingOptions : ITransferMatchingOptions
    {
        public List<TransferMatchRule> Rules { get; set; } = [];
    }

    private sealed class FakeTransactionRepository(IEnumerable<Transaction> seed) : ITransactionRepository
    {
        private readonly List<Transaction> _transactions = [.. seed];

        public Task<List<Transaction>> GetUnlinkedTransactions(int userId, DateTime? fromDate, DateTime? toDate)
            => Task.FromResult(_transactions
                .Where(t => t.TransferNavigation == null && t.TransferId == null && t.Date.HasValue)
                .Where(t => !fromDate.HasValue || t.Date >= fromDate)
                .Where(t => !toDate.HasValue || t.Date <= toDate)
                .ToList());

        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains)
            => Task.FromResult(_transactions
                .Where(t =>
                    t.AccountId == accountId &&
                    t.TransferNavigation == null &&
                    t.Amount == amount &&
                    t.Date.HasValue && t.Date.Value.Date == date.Date &&
                    t.Description.Contains(descriptionContains, StringComparison.OrdinalIgnoreCase))
                .ToList());

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

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
        public Task AddTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task DeleteAllTransactions(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions, int userId) => throw new NotImplementedException();
    }

    private sealed class FakeTransferRepository : ITransferRepository
    {
        public List<Transfer> AddedTransfers { get; } = [];
        public int SaveCount { get; private set; }

        public Task AddTransfer(Transfer transfer)
        {
            AddedTransfers.Add(transfer);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<Transfer?> GetTransfer(int id, int userId) => throw new NotImplementedException();
        public Task<Transfer?> GetTransferByExternalId(string externalId, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers(int userId) => throw new NotImplementedException();
        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date) => throw new NotImplementedException();
        public Task UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public Task DeleteTransfer(int id) => throw new NotImplementedException();
        public Task DeleteTransfers(IEnumerable<int> transferIds) => throw new NotImplementedException();
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> seed) : IAccountRepository
    {
        private readonly List<Account> _accounts = [.. seed];

        public Task<IEnumerable<Account>> GetAccounts(int userId)
            => Task.FromResult<IEnumerable<Account>>(_accounts.Where(a => a.UserId == userId));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

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
