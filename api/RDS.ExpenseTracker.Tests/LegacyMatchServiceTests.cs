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

public class LegacyMatchServiceTests
{
    private const int AdminId = 1;
    private const int UserId = 2;

    private static Transaction Tx(int id, int userId, string description = "BONIFICO")
        => new()
        {
            Id = id,
            Amount = -10m,
            Description = description,
            Date = new DateTime(2025, 10, 5),
            AccountId = userId * 10,
            AccountNavigation = new Account(userId * 10, "BBVA", userId),
        };

    private static (LegacyMatchService Service, FakeTransactionRepository Transactions) Create(params Transaction[] transactions)
    {
        var users = new UserServiceTests.FakeUserRepository(
            new User(AdminId, "admin@test") { IsAdmin = true },
            new User(UserId, "user@test"));
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var categories = new FakeCategoryRepository(
        [
            new Category { Id = 100, Name = "Spesa", UserId = AdminId },
            new Category { Id = 200, Name = "Spesa", UserId = UserId },
        ]);
        var repository = new FakeTransactionRepository(transactions);
        return (new LegacyMatchService(new UserService(users, mapper), repository, new FakeAccountRepository(), categories), repository);
    }

    private static LegacyMatchApplyRequest Request(params (int Id, string Description, int? CategoryId)[] items)
        => new() { Items = [.. items.Select(i => new LegacyMatchApplyItem { TransactionId = i.Id, Description = i.Description, CategoryId = i.CategoryId })] };

    [Fact]
    public async Task Methods_FailWithForbidden_ForNonAdmin()
    {
        var (service, repository) = Create(Tx(1, UserId));

        var apply = await service.ApplyAsync(Request((1, "x", null)), UserId);
        var sheets = await service.GetSheetsAsync(new MemoryStream(), UserId);

        apply.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        sheets.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Apply_UpdatesDescriptionCategoryAndMarksMatched()
    {
        var transaction = Tx(1, AdminId);
        var (service, repository) = Create(transaction);

        var result = await service.ApplyAsync(Request((1, "  BONIFICO - spesa conad ", 100)), AdminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        transaction.Description.Should().Be("BONIFICO - spesa conad");
        transaction.CategoryId.Should().Be(100);
        transaction.LegacyMatchedAt.Should().NotBeNull();
        transaction.UpdatedOn.Should().Be(transaction.LegacyMatchedAt);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Apply_Fails_ForTransactionOfAnotherUser()
    {
        var (service, repository) = Create(Tx(1, UserId));

        var result = await service.ApplyAsync(Request((1, "x", null)), AdminId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundResultError>();
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Apply_Fails_ForCategoryOfAnotherUser()
    {
        var (service, repository) = Create(Tx(1, AdminId));

        var result = await service.ApplyAsync(Request((1, "x", 200)), AdminId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundResultError>();
        repository.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Apply_RejectsEmptyDescription(string description)
    {
        var (service, _) = Create(Tx(1, AdminId));

        var result = await service.ApplyAsync(Request((1, description, null)), AdminId);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task Apply_RejectsDuplicatedTransactions()
    {
        var (service, _) = Create(Tx(1, AdminId));

        var result = await service.ApplyAsync(Request((1, "a", null), (1, "b", null)), AdminId);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetMonth_ReturnsOnlyTransactionsOfTheSheetMonth()
    {
        Transaction On(int id, DateTime date)
        {
            var t = Tx(id, AdminId);
            t.Date = date;
            return t;
        }

        var (service, _) = Create(
            On(1, new DateTime(2025, 9, 30)),
            On(2, new DateTime(2025, 10, 1)),
            On(3, new DateTime(2025, 10, 31)),
            On(4, new DateTime(2025, 11, 1)));
        using var file = LegacySpreadsheetReaderTests.Workbook(("Ottobre2025", [("A2", new DateTime(2025, 10, 5).ToOADate(), null), ("B2", "spesa", null), ("C2", 10d, null)]));

        var result = await service.GetMonthAsync(file, "Ottobre2025", AdminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.DbTransactions.Select(t => t.Id).Should().Equal(2, 3);
    }

    [Fact]
    public async Task GetSheets_FailsWithBadRequest_ForInvalidFile()
    {
        var (service, _) = Create();

        var result = await service.GetSheetsAsync(new MemoryStream([1, 2, 3]), AdminId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
    }

    private sealed class FakeTransactionRepository(IEnumerable<Transaction> transactions) : ITransactionRepository
    {
        private readonly List<Transaction> _transactions = [.. transactions];
        public int SaveCount { get; private set; }

        public Task<List<Transaction>> GetTransactionsByIds(IEnumerable<int> ids, int userId)
            => Task.FromResult(_transactions.Where(t => ids.Contains(t.Id) && t.AccountNavigation.UserId == userId).ToList());

        public Task<List<Transaction>> GetTransactionsInRange(int userId, DateTime startDate, DateTime endDate)
            => Task.FromResult(_transactions.Where(t => t.AccountNavigation.UserId == userId && t.Date >= startDate && t.Date <= endDate).ToList());

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<Transaction?> GetTransaction(int id, int userId) => throw new NotImplementedException();
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

    private sealed class FakeAccountRepository : IAccountRepository
    {
        public Task<IEnumerable<Account>> GetAccounts(int userId) => Task.FromResult<IEnumerable<Account>>([]);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAccounts(IEnumerable<Account> accounts) => throw new NotImplementedException();
        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task DeleteAccounts(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id, int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
        public Task<Account?> GetAccountByImportSource(int userId, BankFileSource source) => throw new NotImplementedException();
    }
}
