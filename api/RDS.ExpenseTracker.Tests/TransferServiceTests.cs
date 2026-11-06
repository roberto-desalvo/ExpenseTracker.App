using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Mappings;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class TransferServiceTests
{
    private const int UserId = 1;
    private const int OtherUserId = 2;
    private static readonly Account Checking = new(1, "Conto", UserId);
    private static readonly Account Savings = new(2, "Risparmi", UserId);
    private static readonly Account Foreign = new(3, "Altro utente", OtherUserId);

    private static (TransferService Service, FakeTransactionRepository Transactions, FakeTransferRepository Transfers) Create(
        params Transaction[] transactions)
    {
        var transactionRepository = new FakeTransactionRepository(transactions);
        var transferRepository = new FakeTransferRepository(transactionRepository);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var service = new TransferService(transferRepository, transactionRepository, new FakeAccountRepository([Checking, Savings, Foreign]), FakeCategoryRepository.WithDefaults(UserId), mapper);
        return (service, transactionRepository, transferRepository);
    }

    private const int MoneyTransfers = (int)CategoryEnum.MoneyTransfers;

    private static Transaction Tx(int id, Account account, decimal amount, DateTime date, int? transferId = null, int? categoryId = null) => new()
    {
        CategoryId = categoryId,
        Id = id,
        AccountId = account.Id,
        AccountNavigation = account,
        Amount = amount,
        Date = date,
        Description = $"Movimento {id}",
        TransferId = transferId,
    };

    [Fact]
    public async Task GetCounterpartCandidates_ReturnsOppositeAmountsOfSameMonth_OnOtherAccounts_ClosestDateFirst()
    {
        var (service, _, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 28)),
            Tx(3, Savings, 100m, new DateTime(2026, 3, 11)),
            Tx(4, Savings, 100m, new DateTime(2026, 5, 1)),   // oltre la tolleranza sul mese successivo
            Tx(5, Savings, -100m, new DateTime(2026, 3, 10)), // stesso segno
            Tx(6, Savings, 90m, new DateTime(2026, 3, 10)),   // importo diverso
            Tx(7, Checking, 100m, new DateTime(2026, 3, 10)), // stesso conto
            Tx(8, Foreign, 100m, new DateTime(2026, 3, 10))); // altro utente

        var result = await service.GetCounterpartCandidates(1, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(t => t.Id).Should().Equal(3, 2);
    }

    [Fact]
    public async Task GetCounterpartCandidates_IncludesThePreviousAndNextMonth()
    {
        var (service, _, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(2, Savings, 100m, new DateTime(2026, 1, 31, 23, 0, 0)),
            Tx(3, Savings, 100m, new DateTime(2026, 2, 1, 9, 30, 0)),
            Tx(4, Savings, 100m, new DateTime(2026, 4, 30, 23, 0, 0)),
            Tx(5, Savings, 100m, new DateTime(2026, 5, 1)));

        var result = await service.GetCounterpartCandidates(1, UserId);

        result.Value.Select(t => t.Id).Should().BeEquivalentTo([3, 4]);
    }

    [Fact]
    public async Task LinkCounterpart_AcceptsACounterpartInTheToleranceOfTheNextMonth()
    {
        var (service, transactions, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 30), transferId: 50),
            Tx(2, Savings, 100m, new DateTime(2026, 4, 2)));

        var result = await service.LinkCounterpart(1, 2, UserId);

        result.IsSuccess.Should().BeTrue();
        transactions.Items.Single(t => t.Id == 2).Date.Should().Be(new DateTime(2026, 3, 30));
    }

    [Fact]
    public async Task GetCounterpartCandidates_ExcludesTransactionsOfCompleteTransfers_KeepsOrphanLegs()
    {
        var (service, _, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 12), transferId: 60),
            Tx(3, Checking, -100m, new DateTime(2026, 3, 12), transferId: 60), // 2 e 3: trasferimento completo
            Tx(4, Savings, 100m, new DateTime(2026, 3, 15), transferId: 70));  // ramo orfano

        var result = await service.GetCounterpartCandidates(1, UserId);

        result.Value.Select(t => t.Id).Should().Equal(4);
    }

    [Fact]
    public async Task GetCounterpartCandidates_FailsWhenTransferAlreadyHasBothLegs()
    {
        var (service, _, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 10), transferId: 50));

        var result = await service.GetCounterpartCandidates(1, UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictResultError>();
    }

    [Fact]
    public async Task GetCounterpartCandidates_FailsWhenTransactionIsNotInATransfer()
    {
        var (service, _, _) = Create(Tx(1, Checking, -100m, new DateTime(2026, 3, 10)));

        var result = await service.GetCounterpartCandidates(1, UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
    }

    [Fact]
    public async Task LinkCounterpart_JoinsTheTransfer_AndAlignsTheDateToTheSource()
    {
        var (service, transactions, transfers) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 13)));

        var result = await service.LinkCounterpart(1, 2, UserId);

        result.IsSuccess.Should().BeTrue();
        var counterpart = transactions.Items.Single(t => t.Id == 2);
        counterpart.TransferId.Should().Be(50);
        counterpart.Date.Should().Be(new DateTime(2026, 3, 10));
        counterpart.UpdatedOn.Should().NotBeNull();
        transfers.Deleted.Should().BeEmpty();
    }

    [Fact]
    public async Task LinkCounterpart_DeletesThePreviousTransferLeftEmpty()
    {
        var (service, transactions, transfers) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 13), transferId: 70));

        var result = await service.LinkCounterpart(1, 2, UserId);

        result.IsSuccess.Should().BeTrue();
        transactions.Items.Single(t => t.Id == 2).TransferId.Should().Be(50);
        transfers.Deleted.Should().Equal(70);
    }

    [Fact]
    public async Task LinkCounterpart_RejectsAnAmountThatIsNotTheOpposite()
    {
        var (service, transactions, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(2, Savings, -100m, new DateTime(2026, 3, 10)));

        var result = await service.LinkCounterpart(1, 2, UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
        transactions.Items.Single(t => t.Id == 2).TransferId.Should().BeNull();
    }

    [Fact]
    public async Task LinkCounterpart_RejectsATransactionOfAnotherUser()
    {
        var (service, _, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50),
            Tx(8, Foreign, 100m, new DateTime(2026, 3, 10)));

        var result = await service.LinkCounterpart(1, 8, UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundResultError>();
    }

    [Fact]
    public async Task GetCounterpartCandidates_AcceptsAMoneyTransferWithoutTransfer()
    {
        var (service, _, _) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), categoryId: MoneyTransfers),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 12)),
            Tx(3, Savings, 100m, new DateTime(2026, 3, 20), transferId: 70));

        var result = await service.GetCounterpartCandidates(1, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(t => t.Id).Should().Equal(2, 3);
    }

    [Fact]
    public async Task GetCounterpartCandidates_RejectsATransactionWithoutTransferOutsideMoneyTransfers()
    {
        var (service, _, _) = Create(Tx(1, Checking, -100m, new DateTime(2026, 3, 10), categoryId: (int)CategoryEnum.FoodAndBeverage));

        var result = await service.GetCounterpartCandidates(1, UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
    }

    [Fact]
    public async Task LinkCounterpart_CreatesTheTransfer_WhenNeitherTransactionHasOne()
    {
        var (service, transactions, transfers) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), categoryId: MoneyTransfers),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 12)));

        var result = await service.LinkCounterpart(1, 2, UserId);

        result.IsSuccess.Should().BeTrue();
        var created = transfers.Added.Should().ContainSingle().Subject;
        transactions.Items.Single(t => t.Id == 1).TransferNavigation.Should().BeSameAs(created);
        var counterpart = transactions.Items.Single(t => t.Id == 2);
        counterpart.TransferNavigation.Should().BeSameAs(created);
        counterpart.Date.Should().Be(new DateTime(2026, 3, 10));
    }

    [Fact]
    public async Task LinkCounterpart_JoinsTheCounterpartTransfer_WhenOnlyTheCounterpartHasOne()
    {
        var (service, transactions, transfers) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), categoryId: MoneyTransfers),
            Tx(2, Savings, 100m, new DateTime(2026, 3, 12), transferId: 70));

        var result = await service.LinkCounterpart(1, 2, UserId);

        result.IsSuccess.Should().BeTrue();
        transfers.Added.Should().BeEmpty();
        transfers.Deleted.Should().BeEmpty();
        transactions.Items.Single(t => t.Id == 1).TransferId.Should().Be(70);
        transactions.Items.Single(t => t.Id == 2).Date.Should().Be(new DateTime(2026, 3, 10));
    }

    [Fact]
    public async Task CreateCounterpart_AddsTheOppositeLegToTheExistingTransfer()
    {
        var (service, transactions, transfers) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50));

        var result = await service.CreateCounterpart(1, new NewTransferCounterpart { AccountId = Savings.Id, Description = " Giroconto " }, UserId);

        result.IsSuccess.Should().BeTrue();
        transfers.Added.Should().BeEmpty();
        var created = transactions.Items.Should().ContainSingle(t => t.Id != 1).Subject;
        created.AccountId.Should().Be(Savings.Id);
        created.Amount.Should().Be(100m);
        created.Date.Should().Be(new DateTime(2026, 3, 10));
        created.Description.Should().Be("Giroconto");
        created.CategoryId.Should().Be(MoneyTransfers);
        created.TransferId.Should().Be(50);
    }

    [Fact]
    public async Task CreateCounterpart_CreatesTheTransfer_ForAMoneyTransferWithoutOne()
    {
        var (service, transactions, transfers) = Create(
            Tx(1, Checking, -100m, new DateTime(2026, 3, 10), categoryId: MoneyTransfers));

        var result = await service.CreateCounterpart(1, new NewTransferCounterpart { AccountId = Savings.Id }, UserId);

        result.IsSuccess.Should().BeTrue();
        var transfer = transfers.Added.Should().ContainSingle().Subject;
        transactions.Items.Should().HaveCount(2).And.OnlyContain(t => t.TransferNavigation == transfer);
        transactions.Items.Single(t => t.Id != 1).Description.Should().Be("Movimento 1");
    }

    [Theory]
    [InlineData(1)] // stesso conto del movimento di partenza
    [InlineData(3)] // conto di un altro utente
    public async Task CreateCounterpart_RejectsSameOrForeignAccount(int accountId)
    {
        var (service, transactions, _) = Create(Tx(1, Checking, -100m, new DateTime(2026, 3, 10), transferId: 50));

        var result = await service.CreateCounterpart(1, new NewTransferCounterpart { AccountId = accountId }, UserId);

        result.IsFailed.Should().BeTrue();
        transactions.Items.Should().ContainSingle();
    }

    private sealed class FakeTransactionRepository(IEnumerable<Transaction> seed) : ITransactionRepository
    {
        public List<Transaction> Items { get; } = [.. seed];

        private IEnumerable<Transaction> ForUser(int userId) => Items.Where(t => t.AccountNavigation.UserId == userId);

        public Task<Transaction?> GetTransaction(int id, int userId) => Task.FromResult(ForUser(userId).FirstOrDefault(t => t.Id == id));
        public Task<List<Transaction>> GetTransactionsByIds(IEnumerable<int> ids, int userId)
            => Task.FromResult(ForUser(userId).Where(t => ids.Contains(t.Id)).ToList());
        public Task<List<Transaction>> GetTransactionsInRange(int userId, DateTime startDate, DateTime endDate)
            => Task.FromResult(ForUser(userId).Where(t => t.Date >= startDate.Date && t.Date < endDate.Date.AddDays(1)).ToList());
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task<IEnumerable<Transaction>> GetTransactions(int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedTransactions(TransactionQueryRequest request, int userId) => throw new NotImplementedException();
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
        public Task<List<Transaction>> GetLegacyMatchedTransactions(int userId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains) => throw new NotImplementedException();
        public Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            Items.AddRange(transactions);
            return Task.CompletedTask;
        }
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public void RemoveTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
        public Task DeleteAllTransactions(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions, int userId) => throw new NotImplementedException();
    }

    // I trasferimenti sono ricavati dai TransferId dei movimenti, come per il repository reale.
    private sealed class FakeTransferRepository(FakeTransactionRepository transactions) : ITransferRepository
    {
        public List<int> Deleted { get; } = [];
        public List<Transfer> Added { get; } = [];

        public Task AddTransfer(Transfer transfer)
        {
            Added.Add(transfer);
            return Task.CompletedTask;
        }

        public Task<Transfer?> GetTransfer(int id, int userId)
        {
            var legs = transactions.Items.Where(t => t.TransferId == id).ToList();
            return Task.FromResult(legs.Any(t => t.AccountNavigation.UserId == userId)
                ? new Transfer { Id = id, Transactions = legs }
                : null);
        }

        public Task DeleteTransfer(int id)
        {
            Deleted.Add(id);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task<Transfer?> GetTransferByExternalId(string externalId, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers(int userId) => throw new NotImplementedException();
        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date) => throw new NotImplementedException();
        public Task UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public Task DeleteTransfers(IEnumerable<int> transferIds) => throw new NotImplementedException();
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> accounts) : IAccountRepository
    {
        public Task<Account?> GetAccount(int id, int userId) => Task.FromResult(accounts.FirstOrDefault(a => a.Id == id && a.UserId == userId));
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAccounts(IEnumerable<Account> accounts) => throw new NotImplementedException();
        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task DeleteAccounts(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<IEnumerable<Account>> GetAccounts(int userId) => throw new NotImplementedException();
        public Task<Account?> GetAccountByImportSource(int userId, Domain.Enums.BankFileSource source) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }
}
