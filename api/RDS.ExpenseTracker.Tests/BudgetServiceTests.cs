using FluentAssertions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using Xunit;

namespace RDS.ExpenseTracker.Tests;

public class BudgetServiceTests
{
    private const int UserId = 1;
    private static readonly DateTime Today = new(2026, 11, 8);
    private const int Groceries = (int)CategoryEnum.FoodAndBeverage;
    private const int Restaurants = (int)CategoryEnum.Entertainment;

    private static BudgetService CreateService(FakeBudgetRepository repository, FakeCategoryRepository? categories = null)
        => new(repository, categories ?? FakeCategoryRepository.WithDefaults(UserId), new FixedTimeProvider(Today));

    [Fact]
    public async Task GetMonth_Future_IsEditableWithoutActuals()
    {
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(new CategoryBudget { CategoryId = Groceries, Month = new DateTime(2026, 12, 1), Amount = 400m });

        var result = await CreateService(repository).GetMonth(new DateTime(2026, 12, 15), UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Month.Should().Be(new DateTime(2026, 12, 1));
        result.Value.IsEditable.Should().BeTrue();
        result.Value.HasActuals.Should().BeFalse();
        result.Value.TotalPlanned.Should().Be(400m);
        result.Value.TotalSpent.Should().BeNull();
        result.Value.Lines.Single(l => l.CategoryId == Groceries).Planned.Should().Be(400m);
        result.Value.Lines.Should().OnlyContain(l => l.Spent == null);
    }

    [Fact]
    public async Task GetTotals_SumsMonthsTouchedByRange()
    {
        var repository = new FakeBudgetRepository();
        repository.Budgets.AddRange([
            new CategoryBudget { CategoryId = Groceries, Month = new DateTime(2026, 8, 1), Amount = 100m },
            new CategoryBudget { CategoryId = Groceries, Month = new DateTime(2026, 9, 1), Amount = 400m },
            new CategoryBudget { CategoryId = Restaurants, Month = new DateTime(2026, 9, 1), Amount = 50m },
            new CategoryBudget { CategoryId = Groceries, Month = new DateTime(2026, 11, 1), Amount = 300m },
            new CategoryBudget { CategoryId = Groceries, Month = new DateTime(2026, 12, 1), Amount = 999m },
        ]);

        var result = await CreateService(repository).GetTotals(new DateTime(2026, 9, 15), new DateTime(2026, 11, 8, 23, 59, 59), UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(t => (t.Month, t.Planned)).Should().Equal(
            (new DateTime(2026, 9, 1), 450m),
            (new DateTime(2026, 11, 1), 300m));
    }

    [Fact]
    public async Task GetTotals_InvalidRange_Fails()
    {
        var result = await CreateService(new FakeBudgetRepository()).GetTotals(new DateTime(2026, 11, 1), new DateTime(2026, 10, 1), UserId);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetMonth_ExcludesMoneyTransfersCategory()
    {
        var result = await CreateService(new FakeBudgetRepository()).GetMonth(new DateTime(2026, 12, 1), UserId);

        result.Value.Lines.Should().NotContain(l => l.CategoryId == (int)CategoryEnum.MoneyTransfers);
    }

    [Fact]
    public async Task GetMonth_Current_IsEditableWithActualsUntilToday()
    {
        var repository = new FakeBudgetRepository();
        repository.Spent.Add((Groceries, 120m));

        var result = await CreateService(repository).GetMonth(new DateTime(2026, 11, 1), UserId);

        result.Value.IsEditable.Should().BeTrue();
        result.Value.HasActuals.Should().BeTrue();
        repository.LastSpentRange.Should().Be((new DateTime(2026, 11, 1), Today.AddDays(1).AddTicks(-1)));
        result.Value.Lines.Single(l => l.CategoryId == Groceries).Spent.Should().Be(120m);
    }

    [Fact]
    public async Task GetMonth_Past_IsReadOnlyAndComputesUnplannedSpending()
    {
        var month = new DateTime(2026, 10, 1);
        var repository = new FakeBudgetRepository();
        repository.Budgets.Add(new CategoryBudget { CategoryId = Groceries, Month = month, Amount = 400m });
        repository.Spent.AddRange([(Groceries, 450m), (Restaurants, 80m), (null, 20m)]);

        var result = await CreateService(repository).GetMonth(month, UserId);

        result.Value.IsEditable.Should().BeFalse();
        result.Value.HasActuals.Should().BeTrue();
        repository.LastSpentRange.Should().Be((month, new DateTime(2026, 11, 1).AddTicks(-1)));
        result.Value.TotalPlanned.Should().Be(400m);
        result.Value.TotalSpent.Should().Be(550m);
        result.Value.UnplannedSpent.Should().Be(100m);
        result.Value.Lines.Single(l => l.CategoryId == Restaurants).Planned.Should().BeNull();
    }

    [Fact]
    public async Task SaveMonth_ReplacesBudgetsAndDropsEmptyAmounts()
    {
        var repository = new FakeBudgetRepository();
        var request = new BudgetMonthUpdateRequest
        {
            Month = new DateTime(2026, 12, 20),
            Lines = [new() { CategoryId = Groceries, Amount = 400m }, new() { CategoryId = Restaurants, Amount = 0m }],
        };

        var result = await CreateService(repository).SaveMonth(request, UserId);

        result.IsSuccess.Should().BeTrue();
        repository.SaveCount.Should().Be(1);
        repository.Budgets.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { CategoryId = Groceries, Month = new DateTime(2026, 12, 1), Amount = 400m });
    }

    [Fact]
    public async Task SaveMonth_CurrentMonth_IsAllowed()
    {
        var request = new BudgetMonthUpdateRequest { Month = new DateTime(2026, 11, 1), Lines = [new() { CategoryId = Groceries, Amount = 1m }] };

        var result = await CreateService(new FakeBudgetRepository()).SaveMonth(request, UserId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveMonth_PastMonth_Fails()
    {
        var repository = new FakeBudgetRepository();
        var request = new BudgetMonthUpdateRequest { Month = new DateTime(2026, 10, 1), Lines = [new() { CategoryId = Groceries, Amount = 1m }] };

        var result = await CreateService(repository).SaveMonth(request, UserId);

        result.IsFailed.Should().BeTrue();
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveMonth_NegativeAmount_Fails()
    {
        var request = new BudgetMonthUpdateRequest { Month = new DateTime(2026, 12, 1), Lines = [new() { CategoryId = Groceries, Amount = -5m }] };

        var result = await CreateService(new FakeBudgetRepository()).SaveMonth(request, UserId);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task SaveMonth_CategoryOfAnotherUser_Fails()
    {
        var categories = FakeCategoryRepository.WithDefaults(UserId);
        categories.Categories.Add(new Category { Id = 500, Name = "Altrui", UserId = 2 });
        var request = new BudgetMonthUpdateRequest { Month = new DateTime(2026, 12, 1), Lines = [new() { CategoryId = 500, Amount = 10m }] };

        var result = await CreateService(new FakeBudgetRepository(), categories).SaveMonth(request, UserId);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task SaveMonth_MoneyTransfersCategory_Fails()
    {
        var request = new BudgetMonthUpdateRequest
        {
            Month = new DateTime(2026, 12, 1),
            Lines = [new() { CategoryId = (int)CategoryEnum.MoneyTransfers, Amount = 10m }],
        };

        var result = await CreateService(new FakeBudgetRepository()).SaveMonth(request, UserId);

        result.IsFailed.Should().BeTrue();
    }

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(localNow, TimeSpan.Zero);
    }

    private sealed class FakeBudgetRepository : IBudgetRepository
    {
        public List<CategoryBudget> Budgets { get; } = [];
        public List<(int? CategoryId, decimal Spent)> Spent { get; } = [];
        public (DateTime Start, DateTime End)? LastSpentRange { get; private set; }
        public int SaveCount { get; private set; }

        public Task<IEnumerable<CategoryBudget>> GetBudgets(DateTime monthStart, int userId)
            => Task.FromResult<IEnumerable<CategoryBudget>>(Budgets.Where(b => b.Month == monthStart).ToList());

        public Task<IEnumerable<(DateTime Month, decimal Planned)>> GetPlannedTotals(DateTime firstMonth, DateTime lastMonth, int userId)
            => Task.FromResult<IEnumerable<(DateTime Month, decimal Planned)>>(Budgets
                .Where(b => b.Month >= firstMonth && b.Month <= lastMonth)
                .GroupBy(b => b.Month)
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.Sum(b => b.Amount)))
                .ToList());

        public Task ReplaceMonthBudgets(DateTime monthStart, IEnumerable<CategoryBudget> budgets, int userId)
        {
            Budgets.RemoveAll(b => b.Month == monthStart);
            Budgets.AddRange(budgets);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<(int? CategoryId, decimal Spent)>> GetSpentByCategory(DateTime monthStart, DateTime endDate, int userId)
        {
            LastSpentRange = (monthStart, endDate);
            return Task.FromResult<IEnumerable<(int? CategoryId, decimal Spent)>>(Spent);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
