using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Text;

namespace RDS.ExpenseTracker.Tests;

public class TradeRepublicCsvImportServiceTests
{
    private const int TestUserId = 1;

    private static readonly TransferMatchRule TradeRepublicRule = new()
    {
        AccountName1 = "Trade Republic",
        DescriptionPattern1 = "Sepa Direct Debit transfer to Satispay Europe S.A.",
        AccountName2 = "Satispay",
        DescriptionPattern2 = "Ricarica Satispay",
    };

    private const string Header = "datetime,account_type,type,name,amount,description,transaction_id,counterparty_iban\n";

    [Fact]
    public async Task ImportFromCsvAsync_ImportsPlainTransaction_WhenNotATransferType()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _);

        const string csv = Header
            + "2026-06-10T08:00:00Z,CURRENT,PAYMENT_OUTBOUND,Supermarket,-12.50,Supermarket purchase,tr-1,\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "traderepublic.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        transferRepository.AddedTransfers.Should().BeEmpty();

        var importedTransaction = transactionRepository.AddedTransactions.Should().ContainSingle().Subject;
        importedTransaction.Amount.Should().Be(-12.50m);
        importedTransaction.ExternalId.Should().Be("tr-1");
    }

    [Fact]
    public async Task ImportFromCsvAsync_RecordsSepaDirectDebitAsUnlinkedTransferLeg_WhenNoSatispayCounterpartYet()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _,
            rules: [TradeRepublicRule]);

        const string csv = Header
            + "2026-06-17T08:00:00Z,CURRENT,PAYMENT_OUTBOUND,Satispay Europe S.A.,-50.00,Sepa Direct Debit transfer to Satispay Europe S.A.,tr-2,\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "traderepublic.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        transferRepository.AddedTransfers.Should().BeEmpty();

        var importedTransaction = transactionRepository.AddedTransactions.Should().ContainSingle().Subject;
        importedTransaction.Amount.Should().Be(-50.00m);
        importedTransaction.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
        importedTransaction.TransferNavigation.Should().BeNull();
    }

    [Fact]
    public async Task ImportFromCsvAsync_LinksTransfer_WhenUnlinkedSatispayTransactionIsOneDayEarlier()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _,
            rules: [TradeRepublicRule],
            accounts: [new Account(1, "Trade Republic", TestUserId), new Account(2, "Satispay", TestUserId)]);

        // Pre-existing unlinked Satispay recharge, dated one day before the TR debit.
        await transactionRepository.AddTransactions([
            new Transaction
            {
                AccountId = 2,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = 50.00m,
                Description = "Ricarica Satispay",
                Date = new DateTime(2026, 6, 16, 9, 0, 0),
                ExternalId = "sat-recharge-42",
            },
        ]);

        const string csv = Header
            + "2026-06-17T08:00:00Z,CURRENT,PAYMENT_OUTBOUND,Satispay Europe S.A.,-50.00,Sepa Direct Debit transfer to Satispay Europe S.A.,tr-3,\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "traderepublic.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        transferRepository.AddedTransfers.Should().ContainSingle();

        var satispayCandidate = transactionRepository.AddedTransactions.Single(t => t.AccountId == 2);
        var trLeg = transactionRepository.AddedTransactions.Single(t => t.AccountId == 1);

        satispayCandidate.TransferNavigation.Should().NotBeNull();
        trLeg.TransferNavigation.Should().BeSameAs(satispayCandidate.TransferNavigation);
        trLeg.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
    }

    [Fact]
    public async Task ImportFromCsvAsync_CreatesTransferViaIban_WhenCounterpartyIbanMapped()
    {
        var options = new FakeTradeRepublicCsvOptions
        {
            IbanToAccountMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["IT60X0542811101000000123456"] = "Sella",
            },
        };

        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _,
            options: options,
            accounts: [new Account(1, "Trade Republic", TestUserId), new Account(2, "Sella", TestUserId)]);

        const string csv = Header
            + "2026-06-10T08:00:00Z,CURRENT,TRANSFER_INSTANT_OUTBOUND,Sella,-100.00,Transfer to Sella,tr-iban-1,IT60X0542811101000000123456\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "traderepublic.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        transferRepository.AddedTransfers.Should().ContainSingle();

        transactionRepository.AddedTransactions.Should().HaveCount(2);
        transactionRepository.AddedTransactions.Should().Contain(t => t.AccountId == 1 && t.Amount == -100m);
        transactionRepository.AddedTransactions.Should().Contain(t => t.AccountId == 2 && t.Amount == 100m);
    }

    private static TradeRepublicCsvImportService BuildService(
        out FakeTransactionRepository transactionRepository,
        out FakeTransferRepository transferRepository,
        out FakeAccountRepository accountRepository,
        FakeTradeRepublicCsvOptions? options = null,
        IEnumerable<TransferMatchRule>? rules = null,
        IEnumerable<Account>? accounts = null)
    {
        transactionRepository = new FakeTransactionRepository();
        transferRepository = new FakeTransferRepository();
        accountRepository = new FakeAccountRepository(accounts ?? [new Account(1, "Trade Republic", TestUserId)]);

        var categoryRepository = new FakeCategoryRepository();
        var transferMatchingOptions = new FakeTransferMatchingOptions { Rules = rules?.ToList() ?? [] };
        var transferMatchingService = new TransferMatchingService(transactionRepository, accountRepository, transferMatchingOptions);

        return new TradeRepublicCsvImportService(
            transactionRepository,
            transferRepository,
            categoryRepository,
            accountRepository,
            options ?? new FakeTradeRepublicCsvOptions(),
            transferMatchingService,
            NullLogger<TradeRepublicCsvImportService>.Instance);
    }

    private sealed class FakeTradeRepublicCsvOptions : ITradeRepublicCsvOptions
    {
        public string DefaultAccountName => "Trade Republic";
        public string? TradingAccountName => "Trade Republic Trading";
        public Dictionary<string, string> IbanToAccountMap { get; set; } = [];
    }

    private sealed class FakeTransferMatchingOptions : ITransferMatchingOptions
    {
        public List<TransferMatchRule> Rules { get; set; } = [];
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private int _nextId = 1;

        public List<Transaction> AddedTransactions { get; } = [];

        public Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            var txs = transactions.ToList();
            foreach (var tx in txs)
            {
                if (tx.Id == 0)
                    tx.Id = _nextId++;
            }

            AddedTransactions.AddRange(txs);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds)
            => Task.FromResult(Enumerable.Empty<string>());

        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains)
        {
            var targetDate = date.Date;

            var result = AddedTransactions
                .Where(t =>
                    t.AccountId == accountId &&
                    t.TransferNavigation == null &&
                    t.Amount == amount &&
                    t.Date.HasValue && t.Date.Value.Date == targetDate &&
                    t.Description.Contains(descriptionContains, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transaction?> GetTransaction(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactions() => throw new NotImplementedException();
        public Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate) => throw new NotImplementedException();
        public Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate) => throw new NotImplementedException();
        public Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges() => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId) => throw new NotImplementedException();
        public Task<Transaction> GetLatestTransaction() => throw new NotImplementedException();
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task DeleteAllTransactions() => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }

    private sealed class FakeTransferRepository : ITransferRepository
    {
        public List<Transfer> AddedTransfers { get; } = [];

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transfer?> GetTransfer(int id) => throw new NotImplementedException();
        public Task<Transfer?> GetTransferByExternalId(string externalId) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers() => throw new NotImplementedException();
        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date) => Task.FromResult<Transfer?>(null);
        public Task AddTransfer(Transfer transfer)
        {
            AddedTransfers.Add(transfer);
            return Task.CompletedTask;
        }
        public Task UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public Task DeleteTransfer(int id) => throw new NotImplementedException();
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<IEnumerable<Category>> GetCategories(string? name = null)
            => Task.FromResult<IEnumerable<Category>>([]);

        public Task<Category?> GetDefaultCategory()
            => Task.FromResult<Category?>(null);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Category?> GetCategory(int id) => throw new NotImplementedException();
        public Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategories(CategoryQueryRequest request) => throw new NotImplementedException();
        public Task AddCategories(IEnumerable<Category> categories) => throw new NotImplementedException();
        public Task UpdateCategory(Category category) => throw new NotImplementedException();
        public Task RemoveCategory(int id) => throw new NotImplementedException();
        public Task RemoveCategory(Category category) => throw new NotImplementedException();
        public Task ReassignTransactionsToCategory(int sourceCategoryId, int targetCategoryId) => throw new NotImplementedException();
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> seedAccounts) : IAccountRepository
    {
        private readonly List<Account> _accounts = [.. seedAccounts];

        public Task AddAccounts(IEnumerable<Account> accounts)
        {
            _accounts.AddRange(accounts);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Account>> GetAccounts()
            => Task.FromResult<IEnumerable<Account>>(_accounts);

        public Task<IEnumerable<Account>> GetAccounts(int userId)
            => Task.FromResult<IEnumerable<Account>>(_accounts.Where(a => a.UserId == userId));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id, int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }
}
