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

public class TransactionServiceTests
{
    private const int AccountId = 1;

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    private static TransactionService CreateService(FakeTransactionRepository transactionRepository)
        => new(transactionRepository, new FakeAccountRepository(), new FakeCategoryRepository(), CreateMapper());

    [Fact]
    public async Task GetStock_AveragesRunningBalance_AcrossTransactionsInSameBucket()
    {
        // Gennaio: due transazioni. Saldo dopo la prima = 100, dopo la seconda = 150.
        // Media attesa per il bucket di gennaio = (100 + 150) / 2 = 125.
        var transactionRepository = new FakeTransactionRepository(
        [
            Transaction(new DateTime(2026, 1, 5), 100m),
            Transaction(new DateTime(2026, 1, 20), 50m),
        ]);

        var service = CreateService(transactionRepository);

        var result = await service.GetStock(new TimeSeriesRequestDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [AccountId],
            IdCategories = [],
        });

        result.IsSuccess.Should().BeTrue();
        var points = result.Value.Series.Single().Values;
        points.Should().ContainSingle(p => p.Period == "2026-01" && p.Amount == 125m);
    }

    [Fact]
    public async Task GetStock_CarriesForwardLastKnownBalance_ForPeriodsWithoutTransactions()
    {
        // Unica transazione a gennaio (saldo 100). Febbraio e marzo non hanno transazioni:
        // devono riportare il saldo di gennaio, non essere omessi.
        var transactionRepository = new FakeTransactionRepository(
        [
            Transaction(new DateTime(2026, 1, 10), 100m),
        ]);

        var service = CreateService(transactionRepository);

        var result = await service.GetStock(new TimeSeriesRequestDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31),
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [AccountId],
            IdCategories = [],
        });

        result.IsSuccess.Should().BeTrue();
        var points = result.Value.Series.Single().Values;
        points.Select(p => p.Period).Should().Equal("2026-01", "2026-02", "2026-03");
        points.Should().AllSatisfy(p => p.Amount.Should().Be(100m));
    }

    [Fact]
    public async Task GetStock_SeedsBaselineBalance_FromTransactionsBeforeStartDate()
    {
        // Transazione di dicembre (saldo 200) precedente al range richiesto: deve
        // diventare il saldo di partenza (riportato) di gennaio, che non ha transazioni proprie.
        var transactionRepository = new FakeTransactionRepository(
        [
            Transaction(new DateTime(2025, 12, 15), 200m),
        ]);

        var service = CreateService(transactionRepository);

        var result = await service.GetStock(new TimeSeriesRequestDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [AccountId],
            IdCategories = [],
        });

        result.IsSuccess.Should().BeTrue();
        var points = result.Value.Series.Single().Values;
        points.Should().ContainSingle(p => p.Period == "2026-01" && p.Amount == 200m);
    }

    [Fact]
    public async Task GetStock_ReturnsFlatZeroSeries_WhenAccountHasNoTransactions()
    {
        var transactionRepository = new FakeTransactionRepository([]);

        var service = CreateService(transactionRepository);

        var result = await service.GetStock(new TimeSeriesRequestDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31),
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [AccountId],
            IdCategories = [],
        });

        result.IsSuccess.Should().BeTrue();
        var points = result.Value.Series.Single().Values;
        points.Select(p => p.Period).Should().Equal("2026-01", "2026-02", "2026-03");
        points.Should().AllSatisfy(p => p.Amount.Should().Be(0m));
    }

    [Fact]
    public async Task GetStock_AlwaysIncludesTransfers_RegardlessOfExcludeTransfersFlag()
    {
        // Un giroconto e' un movimento di denaro reale per il singolo conto (anche se si annulla
        // a livello di patrimonio totale): il saldo calcolato da GetStock deve includerlo sempre,
        // anche quando il chiamante chiede ExcludeTransfers = true (usato per i grafici di flusso).
        const int OtherAccountId = 2;
        var transactionRepository = new FakeTransactionRepository(
        [
            Transaction(new DateTime(2026, 1, 5), 1000m), // stipendio su AccountId
            new Transaction { AccountId = AccountId, Date = new DateTime(2026, 2, 10), Amount = -300m, TransferId = 1 },
            new Transaction { AccountId = OtherAccountId, Date = new DateTime(2026, 2, 10), Amount = 300m, TransferId = 1 },
        ]);

        var service = CreateService(transactionRepository);

        var result = await service.GetStock(new TimeSeriesRequestDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 2, 28),
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [AccountId, OtherAccountId],
            IdCategories = [],
            ExcludeTransfers = true,
        });

        result.IsSuccess.Should().BeTrue();
        var accountSeries = result.Value.Series.Single(s => s.Dimensions.Any(d => d.Value == AccountId.ToString()));
        var otherAccountSeries = result.Value.Series.Single(s => s.Dimensions.Any(d => d.Value == OtherAccountId.ToString()));

        accountSeries.Values.Should().ContainSingle(p => p.Period == "2026-02" && p.Amount == 700m);
        otherAccountSeries.Values.Should().ContainSingle(p => p.Period == "2026-02" && p.Amount == 300m);
    }

    [Fact]
    public async Task GetTimeSeries_ComputesEarnedAndSpent_SeparatelyFromNetAmount()
    {
        // Gennaio: entrata 500 e due uscite (-120, -30) nello stesso periodo.
        // Amount netto = 500 - 120 - 30 = 350, ma Earned/Spent devono restare separati e in valore assoluto.
        var transactionRepository = new FakeTransactionRepository(
        [
            Transaction(new DateTime(2026, 1, 5), 500m),
            Transaction(new DateTime(2026, 1, 10), -120m),
            Transaction(new DateTime(2026, 1, 20), -30m),
        ]);

        var service = CreateService(transactionRepository);

        var result = await service.GetTimeSeries(new TimeSeriesRequestDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [AccountId],
            IdCategories = [],
        });

        result.IsSuccess.Should().BeTrue();
        var point = result.Value.Series.Single().Values.Single(p => p.Period == "2026-01");
        point.Amount.Should().Be(350m);
        point.Earned.Should().Be(500m);
        point.Spent.Should().Be(150m);
    }

    private static Transaction Transaction(DateTime date, decimal amount) => new()
    {
        AccountId = AccountId,
        Date = date,
        Amount = amount,
    };

    private sealed class FakeTransactionRepository(IEnumerable<Transaction>? seedTransactions = null) : ITransactionRepository
    {
        private readonly List<Transaction> _transactions = [.. seedTransactions ?? []];

        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request)
        {
            var query = _transactions.Where(t => t.Date.HasValue && t.Date <= request.EndDate);

            if (request.ExcludeTransfers)
                query = query.Where(t => t.TransferId == null);

            if (request.IdAccounts.Any())
                query = query.Where(t => request.IdAccounts.Contains(t.AccountId));

            if (request.IdCategories.Any())
                query = query.Where(t => t.CategoryId.HasValue && request.IdCategories.Contains(t.CategoryId.Value));

            var result = query
                .OrderBy(t => t.Date)
                .Select(t => (t.Date!.Value, t.Amount, t.AccountId, t.CategoryId))
                .ToList();

            return Task.FromResult<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>>(result);
        }

        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request)
        {
            var query = _transactions.Where(t => t.Date.HasValue && t.Date >= request.StartDate && t.Date <= request.EndDate);

            if (request.ExcludeTransfers)
                query = query.Where(t => t.TransferId == null);

            if (request.IdAccounts.Any())
                query = query.Where(t => request.IdAccounts.Contains(t.AccountId));

            if (request.IdCategories.Any())
                query = query.Where(t => t.CategoryId.HasValue && request.IdCategories.Contains(t.CategoryId.Value));

            var result = query
                .OrderBy(t => t.Date)
                .Select(t => (t.Date!.Value, t.Amount, t.AccountId, t.CategoryId))
                .ToList();

            return Task.FromResult<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>>(result);
        }

        public Task AddTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Transaction?> GetTransaction(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactions() => throw new NotImplementedException();
        public Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Spent, decimal Earned)>> GetAccountMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers) => throw new NotImplementedException();
        public Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers) => throw new NotImplementedException();
        public Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges() => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId) => throw new NotImplementedException();
        public Task<Transaction> GetLatestTransaction() => throw new NotImplementedException();
        public Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds) => throw new NotImplementedException();
        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains) => throw new NotImplementedException();
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task DeleteAllTransactions() => throw new NotImplementedException();
        public Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        public Task<IEnumerable<Account>> GetAccounts() => Task.FromResult<IEnumerable<Account>>([]);
        public Task<IEnumerable<Account>> GetAccounts(int userId) => Task.FromResult<IEnumerable<Account>>([]);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAccounts(IEnumerable<Account> accounts) => throw new NotImplementedException();
        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task DeleteAccounts(IEnumerable<int> accountIds) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id, int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<IEnumerable<Category>> GetCategories(string? name = null) => Task.FromResult<IEnumerable<Category>>([]);
        public Task<Category?> GetDefaultCategory() => Task.FromResult<Category?>(null);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Category?> GetCategory(int id) => throw new NotImplementedException();
        public Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategories(CategoryQueryRequest request) => throw new NotImplementedException();
        public Task AddCategories(IEnumerable<Category> categories) => throw new NotImplementedException();
        public Task UpdateCategory(Category category) => throw new NotImplementedException();
        public Task RemoveCategory(int id) => throw new NotImplementedException();
        public Task RemoveCategory(Category category) => throw new NotImplementedException();
        public Task ReassignTransactionsToCategory(int sourceCategoryId, int targetCategoryId) => throw new NotImplementedException();
    }
}
