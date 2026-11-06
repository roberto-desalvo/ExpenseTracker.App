using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Mappings;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class TransactionServiceTests
{
    private const int AccountId = 1;
    private const int UserId = 1;

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance);
        return configuration.CreateMapper();
    }

    private static TransactionService CreateService(FakeTransactionRepository transactionRepository)
        => new(transactionRepository, new FakeAccountRepository(), FakeCategoryRepository.WithDefaults(UserId), CreateMapper());

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
        }, UserId);

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
        }, UserId);

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
        }, UserId);

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
        }, UserId);

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
        }, UserId);

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
        }, UserId);

        result.IsSuccess.Should().BeTrue();
        var point = result.Value.Series.Single().Values.Single(p => p.Period == "2026-01");
        point.Amount.Should().Be(350m);
        point.Earned.Should().Be(500m);
        point.Spent.Should().Be(150m);
    }

    [Fact]
    public async Task GetTimeSeries_Invested_CountsOnlyOutflowsOfInvestmentsCategory()
    {
        // Gennaio: acquisto ETF (-200) e dividendo (+15) in Investimenti, spesa (-80) in altra categoria.
        var investmentsId = (int)CategoryEnum.SavingsAndInvestments;
        var transactionRepository = new FakeTransactionRepository(
        [
            Transaction(new DateTime(2026, 1, 5), -200m, investmentsId),
            Transaction(new DateTime(2026, 1, 10), 15m, investmentsId),
            Transaction(new DateTime(2026, 1, 20), -80m, (int)CategoryEnum.FoodAndBeverage),
        ]);

        var service = CreateService(transactionRepository);

        var result = await service.GetTimeSeries(new TimeSeriesRequestDto
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [],
            IdCategories = [],
        }, UserId);

        result.IsSuccess.Should().BeTrue();
        var point = result.Value.Series.Single().Values.Single(p => p.Period == "2026-01");
        point.Spent.Should().Be(280m);
        point.Invested.Should().Be(200m);
    }

    [Fact]
    public async Task UpdateTransaction_PreservesImportFields_WhenEditingDescriptionAndCategory()
    {
        var createdOn = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc);
        var transactionRepository = new FakeTransactionRepository(
        [
            new Transaction
            {
                Id = 42,
                AccountId = AccountId,
                Date = new DateTime(2026, 1, 5),
                Amount = -12.5m,
                Description = "SATISPAY*BAR",
                ExternalId = "sat-123",
                TransferId = 7,
                CreatedOn = createdOn,
            },
        ]);
        var service = CreateService(transactionRepository);

        var result = await service.UpdateTransaction(new TransactionDto
        {
            Id = 42,
            AccountId = AccountId,
            Date = new DateTime(2026, 1, 5),
            Amount = -12.5m,
            Description = "Colazione",
            CategoryId = 0,
        }, UserId);

        result.IsSuccess.Should().BeTrue();
        var updated = transactionRepository.UpdatedTransactions.Single();
        updated.Description.Should().Be("Colazione");
        updated.ExternalId.Should().Be("sat-123");
        updated.TransferId.Should().Be(7);
        updated.CreatedOn.Should().Be(createdOn);
        updated.UpdatedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteTransactions_RemovesAllRequested_WhenTheyBelongToUser()
    {
        var transactionRepository = new FakeTransactionRepository(
        [
            new Transaction { Id = 1, AccountId = AccountId, Amount = -10m },
            new Transaction { Id = 2, AccountId = AccountId, Amount = -20m },
            new Transaction { Id = 3, AccountId = AccountId, Amount = -30m },
        ]);
        var service = CreateService(transactionRepository);

        var result = await service.DeleteTransactions(new TransactionBulkRequest { Ids = [1, 3, 3] }, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        transactionRepository.RemovedTransactions.Select(t => t.Id).Should().BeEquivalentTo([1, 3]);
        transactionRepository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task DeleteTransactions_FailsWithoutChanges_WhenAnIdIsNotFound()
    {
        var transactionRepository = new FakeTransactionRepository(
        [
            new Transaction { Id = 1, AccountId = AccountId, Amount = -10m },
        ]);
        var service = CreateService(transactionRepository);

        var result = await service.DeleteTransactions(new TransactionBulkRequest { Ids = [1, 99] }, UserId);

        result.IsFailed.Should().BeTrue();
        transactionRepository.RemovedTransactions.Should().BeEmpty();
        transactionRepository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task DeleteTransactions_Fails_WhenNoIds()
    {
        var service = CreateService(new FakeTransactionRepository([]));

        var result = await service.DeleteTransactions(new TransactionBulkRequest(), UserId);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task AssignCategory_SetsCategoryOnAllRequested()
    {
        var categoryId = (await FakeCategoryRepository.WithDefaults(UserId).GetCategories(UserId)).First().Id;
        var transactionRepository = new FakeTransactionRepository(
        [
            new Transaction { Id = 1, AccountId = AccountId, Amount = -10m },
            new Transaction { Id = 2, AccountId = AccountId, Amount = -20m, CategoryId = null },
            new Transaction { Id = 3, AccountId = AccountId, Amount = -30m },
        ]);
        var service = CreateService(transactionRepository);

        var result = await service.AssignCategory(new TransactionBulkCategoryRequest { Ids = [1, 2], CategoryId = categoryId }, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        var byId = (await transactionRepository.GetTransactionsByIds([1, 2, 3], UserId)).ToDictionary(t => t.Id);
        byId[1].CategoryId.Should().Be(categoryId);
        byId[2].CategoryId.Should().Be(categoryId);
        byId[1].UpdatedOn.Should().NotBeNull();
        byId[3].CategoryId.Should().NotBe(categoryId);
        transactionRepository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task AssignCategory_ClearsCategory_WhenCategoryIdIsZero()
    {
        var categoryId = (await FakeCategoryRepository.WithDefaults(UserId).GetCategories(UserId)).First().Id;
        var transactionRepository = new FakeTransactionRepository(
        [
            new Transaction { Id = 1, AccountId = AccountId, Amount = -10m, CategoryId = categoryId },
        ]);
        var service = CreateService(transactionRepository);

        var result = await service.AssignCategory(new TransactionBulkCategoryRequest { Ids = [1], CategoryId = 0 }, UserId);

        result.IsSuccess.Should().BeTrue();
        (await transactionRepository.GetTransaction(1, UserId))!.CategoryId.Should().BeNull();
    }

    [Fact]
    public async Task AssignCategory_FailsWithoutChanges_WhenCategoryIsNotOfUser()
    {
        var transactionRepository = new FakeTransactionRepository(
        [
            new Transaction { Id = 1, AccountId = AccountId, Amount = -10m },
        ]);
        var service = CreateService(transactionRepository);

        var result = await service.AssignCategory(new TransactionBulkCategoryRequest { Ids = [1], CategoryId = 987654 }, UserId);

        result.IsFailed.Should().BeTrue();
        (await transactionRepository.GetTransaction(1, UserId))!.CategoryId.Should().BeNull();
        transactionRepository.SaveChangesCalls.Should().Be(0);
    }

    private static TransactionSplitRequest Split(params decimal[] amounts) => new() { Amounts = [.. amounts] };

    [Fact]
    public async Task SplitTransaction_CreatesPartsWithSameData_KeepingFirstOnOriginal()
    {
        var date = new DateTime(2026, 3, 10);
        var original = new Transaction
        {
            Id = 1, AccountId = AccountId, Amount = -100m, Date = date, Description = "Cena", CategoryId = 7,
            ExternalId = "ext-1", Source = TransactionSource.FileImport,
        };
        var transactionRepository = new FakeTransactionRepository([original]);
        var service = CreateService(transactionRepository);

        var result = await service.SplitTransaction(1, Split(-50.50m, -30m, -19.50m), UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        original.Amount.Should().Be(-50.50m);
        original.ExternalId.Should().Be("ext-1");
        var copies = transactionRepository.AddedTransactions;
        copies.Select(c => c.Amount).Should().Equal(-30m, -19.50m);
        copies.Should().AllSatisfy(copy =>
        {
            copy.Description.Should().Be("Cena");
            copy.Date.Should().Be(date);
            copy.AccountId.Should().Be(AccountId);
            copy.CategoryId.Should().Be(7);
            copy.ExternalId.Should().BeNull();
            copy.TransferId.Should().BeNull();
            copy.Source.Should().Be(TransactionSource.UserCreated);
        });
        transactionRepository.SaveChangesCalls.Should().Be(1);
    }

    public static TheoryData<decimal[]> InvalidSplits => new()
    {
        new[] { -100m },                 // una sola voce
        new[] { -60m, -30m },            // somma diversa
        new[] { -110m, 10m },            // segno diverso
        new[] { -100m, 0m },             // voce a zero
        new[] { -50.005m, -49.995m },    // più di 2 decimali
    };

    [Theory]
    [MemberData(nameof(InvalidSplits))]
    public async Task SplitTransaction_FailsForInvalidAmounts(decimal[] amounts)
    {
        var original = new Transaction { Id = 1, AccountId = AccountId, Amount = -100m };
        var transactionRepository = new FakeTransactionRepository([original]);
        var service = CreateService(transactionRepository);

        var result = await service.SplitTransaction(1, Split(amounts), UserId);

        result.IsFailed.Should().BeTrue();
        original.Amount.Should().Be(-100m);
        transactionRepository.AddedTransactions.Should().BeEmpty();
        transactionRepository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task SplitTransaction_FailsForCompleteTransferLeg()
    {
        var transactionRepository = new FakeTransactionRepository(
        [
            new Transaction { Id = 1, AccountId = AccountId, Amount = -100m, TransferId = 5 },
            new Transaction { Id = 2, AccountId = 2, Amount = 100m, TransferId = 5 },
        ]);
        var service = CreateService(transactionRepository);

        var result = await service.SplitTransaction(1, Split(-50m, -50m), UserId);

        result.IsFailed.Should().BeTrue();
        transactionRepository.AddedTransactions.Should().BeEmpty();
        transactionRepository.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task SplitTransaction_AllowsIncompleteTransferLeg_KeepingCopiesOutOfTransfer()
    {
        var original = new Transaction { Id = 1, AccountId = AccountId, Amount = -100m, TransferId = 5 };
        var transactionRepository = new FakeTransactionRepository([original]);
        var service = CreateService(transactionRepository);

        var result = await service.SplitTransaction(1, Split(-70m, -20m, -10m), UserId);

        result.IsSuccess.Should().BeTrue();
        original.Amount.Should().Be(-70m);
        original.TransferId.Should().Be(5);
        transactionRepository.AddedTransactions.Should().HaveCount(2).And.OnlyContain(c => c.TransferId == null);
    }

    [Fact]
    public async Task SplitTransaction_FailsWhenNotFound()
    {
        var transactionRepository = new FakeTransactionRepository([]);
        var service = CreateService(transactionRepository);

        (await service.SplitTransaction(99, Split(-50m, -50m), UserId)).IsFailed.Should().BeTrue();
        transactionRepository.SaveChangesCalls.Should().Be(0);
    }

    private static Transaction Transaction(DateTime date, decimal amount, int categoryId) => new()
    {
        AccountId = AccountId,
        Date = date,
        Amount = amount,
        CategoryId = categoryId,
    };

    private static Transaction Transaction(DateTime date, decimal amount) => new()
    {
        AccountId = AccountId,
        Date = date,
        Amount = amount,
    };

    private sealed class FakeTransactionRepository(IEnumerable<Transaction>? seedTransactions = null) : ITransactionRepository
    {
        private readonly List<Transaction> _transactions = [.. seedTransactions ?? []];

        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request, int userId)
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

        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request, int userId)
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

        public List<Transaction> AddedTransactions { get; } = [];
        public Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            AddedTransactions.AddRange(transactions);
            return Task.CompletedTask;
        }
        public List<Transaction> UpdatedTransactions { get; } = [];

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(UpdatedTransactions.Count);
        }
        public Task<Transaction?> GetTransaction(int id, int userId) => Task.FromResult(_transactions.FirstOrDefault(t => t.Id == id));
        public Task<IEnumerable<Transaction>> GetTransactions(int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedTransactions(TransactionQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Spent, decimal Earned)>> GetAccountMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId) => throw new NotImplementedException();
        public Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate, bool excludeTransfers, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId)
            => Task.FromResult<IEnumerable<Transaction>>(_transactions.Where(t => t.TransferId == transferId).ToList());
        public Task<Transaction> GetLatestTransaction(int userId) => throw new NotImplementedException();
        public Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds, int userId) => throw new NotImplementedException();
        public Task<List<Transaction>> GetCategorizableTransactions(int userId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<List<Transaction>> GetTransactionsInRange(int userId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<List<Transaction>> GetTransactionsByIds(IEnumerable<int> ids, int userId)
            => Task.FromResult(_transactions.Where(t => ids.Contains(t.Id)).ToList());
        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains) => throw new NotImplementedException();
        public Task UpdateTransaction(Transaction transaction)
        {
            UpdatedTransactions.Add(transaction);
            return Task.CompletedTask;
        }
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task<List<Transaction>> GetLegacyMatchedTransactions(int userId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public List<Transaction> RemovedTransactions { get; } = [];
        public int SaveChangesCalls { get; private set; }
        public void RemoveTransactions(IEnumerable<Transaction> transactions) => RemovedTransactions.AddRange(transactions);
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
