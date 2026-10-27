using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Mappings;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class CategoryServiceTests
{
    private const int UserId = 1;
    private const int GroceriesId = (int)CategoryEnum.FoodAndBeverage;
    private const int EntertainmentId = (int)CategoryEnum.Entertainment;
    private const int DefaultId = (int)CategoryEnum.Default;

    private static readonly Category[] Categories =
    [
        new() { Id = DefaultId, Name = "Default", Priority = 0, IsDefault = true, Tags = "", UserId = UserId },
        new() { Id = GroceriesId, Name = "Food", Priority = 1, Tags = "esselunga,amazon fresh", UserId = UserId },
        new() { Id = EntertainmentId, Name = "Fun", Priority = 2, Tags = "amazon,netflix", UserId = UserId },
        // categoria di un altro utente: i suoi tag non devono essere applicati
        new() { Id = 99, Name = "Altrui", Priority = -1, Tags = "esselunga", UserId = UserId + 1 },
    ];

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    private static CategoryService CreateService(FakeTransactionRepository transactions)
        => new(new FakeCategoryRepository(Categories), transactions, CreateMapper());

    private static CategorySyncRequest Range(DateTime start, DateTime end) => new() { StartDate = start, EndDate = end };

    [Fact]
    public async Task SyncTransactionCategories_AssignsMatchingCategory_HighestPriorityWins()
    {
        var spesa = Transaction(1, "ESSELUNGA MILANO", new DateTime(2026, 3, 2), DefaultId);
        var doppioMatch = Transaction(2, "AMAZON FRESH ordine", new DateTime(2026, 3, 5), DefaultId);
        var film = Transaction(3, "Netflix.com", new DateTime(2026, 3, 9), DefaultId);
        var repository = new FakeTransactionRepository([spesa, doppioMatch, film]);

        var result = await CreateService(repository).SyncTransactionCategories(Range(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)), UserId);

        result.IsSuccess.Should().BeTrue();
        spesa.CategoryId.Should().Be(GroceriesId);
        doppioMatch.CategoryId.Should().Be(GroceriesId, "'amazon fresh' (priorità 1) batte 'amazon' (priorità 2)");
        film.CategoryId.Should().Be(EntertainmentId);
        result.Value.UpdatedTransactions.Should().Be(3);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task SyncTransactionCategories_LeavesUnmatchedTransactionsUntouched()
    {
        var manuale = Transaction(1, "Bonifico affitto", new DateTime(2026, 3, 2), (int)CategoryEnum.Housing);
        var repository = new FakeTransactionRepository([manuale]);

        var result = await CreateService(repository).SyncTransactionCategories(Range(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)), UserId);

        manuale.CategoryId.Should().Be((int)CategoryEnum.Housing);
        manuale.UpdatedOn.Should().BeNull();
        result.Value.UnmatchedTransactions.Should().Be(1);
        result.Value.UpdatedTransactions.Should().Be(0);
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task SyncTransactionCategories_SkipsTransfersAndTransactionsOutsideRange()
    {
        var trasferimento = Transaction(1, "Giroconto esselunga", new DateTime(2026, 3, 2), DefaultId, transferId: 10);
        var moneyTransfer = Transaction(2, "Ricarica esselunga", new DateTime(2026, 3, 2), (int)CategoryEnum.MoneyTransfers);
        var fuoriRange = Transaction(3, "ESSELUNGA", new DateTime(2026, 4, 1), DefaultId);
        var ultimoGiorno = Transaction(4, "ESSELUNGA", new DateTime(2026, 3, 31, 18, 30, 0), DefaultId);
        var repository = new FakeTransactionRepository([trasferimento, moneyTransfer, fuoriRange, ultimoGiorno]);

        var result = await CreateService(repository).SyncTransactionCategories(Range(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)), UserId);

        trasferimento.CategoryId.Should().Be(DefaultId);
        moneyTransfer.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
        fuoriRange.CategoryId.Should().Be(DefaultId);
        ultimoGiorno.CategoryId.Should().Be(GroceriesId, "il giorno finale del range è incluso");
        result.Value.ExaminedTransactions.Should().Be(1);
    }

    [Fact]
    public async Task SyncTransactionCategories_FailsWhenRangeIsInvalid()
    {
        var service = CreateService(new FakeTransactionRepository([]));

        (await service.SyncTransactionCategories(new CategorySyncRequest { EndDate = new DateTime(2026, 3, 1) }, UserId))
            .IsFailed.Should().BeTrue();
        (await service.SyncTransactionCategories(Range(new DateTime(2026, 3, 2), new DateTime(2026, 3, 1)), UserId))
            .IsFailed.Should().BeTrue();
    }

    private static Transaction Transaction(int id, string description, DateTime date, int categoryId, int? transferId = null)
        => new() { Id = id, Description = description, Date = date, CategoryId = categoryId, TransferId = transferId, AccountId = 1 };

    // Replica il filtro della query reale: range di date (estremi inclusi) ed esclusione dei trasferimenti.
    private sealed class FakeTransactionRepository(IEnumerable<Transaction> seed) : ITransactionRepository
    {
        private readonly List<Transaction> _transactions = [.. seed];

        public int SaveCount { get; private set; }

        public Task<List<Transaction>> GetCategorizableTransactions(int userId, DateTime startDate, DateTime endDate)
            => Task.FromResult(_transactions
                .Where(t => t.TransferId == null && t.CategoryId != (int)CategoryEnum.MoneyTransfers)
                .Where(t => t.Date != null && t.Date >= startDate.Date && t.Date < endDate.Date.AddDays(1))
                .ToList());

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(0);
        }

        public Task AddTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
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
        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains) => throw new NotImplementedException();
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task DeleteAllTransactions(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions, int userId) => throw new NotImplementedException();
    }

}
