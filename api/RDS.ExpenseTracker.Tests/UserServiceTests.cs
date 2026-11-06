using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Mappings;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class UserServiceTests
{
    private const int UserId = 7;

    private static UserService CreateService(FakeUserRepository repository)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance).CreateMapper();
        return new UserService(repository, mapper);
    }

    private static AnalyticsLayoutDto Layout(params (string Id, string Width)[] cards)
        => new() { Cards = [.. cards.Select(c => new AnalyticsCardLayoutDto { Id = c.Id, Width = c.Width })] };

    [Fact]
    public async Task GetAnalyticsLayout_ReturnsNull_WhenNeverSaved()
    {
        var service = CreateService(new FakeUserRepository(new User(UserId, "a@b.c")));

        var result = await service.GetAnalyticsLayoutAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task SaveAnalyticsLayout_ThenGet_RoundTripsOrderAndWidths()
    {
        var repository = new FakeUserRepository(new User(UserId, "a@b.c"));
        var service = CreateService(repository);

        var saved = await service.SaveAnalyticsLayoutAsync(UserId, Layout(("cash-flow", "full"), ("category-rank", "third")));
        var loaded = await service.GetAnalyticsLayoutAsync(UserId);

        saved.IsSuccess.Should().BeTrue();
        repository.SaveCount.Should().Be(1);
        loaded.Value!.Cards.Select(c => (c.Id, c.Width)).Should().Equal(("cash-flow", "full"), ("category-rank", "third"));
    }

    [Fact]
    public async Task SaveAnalyticsLayout_AcceptsEmptyLayout()
    {
        var service = CreateService(new FakeUserRepository(new User(UserId, "a@b.c")));

        var result = await service.SaveAnalyticsLayoutAsync(UserId, Layout());

        result.IsSuccess.Should().BeTrue();
        (await service.GetAnalyticsLayoutAsync(UserId)).Value!.Cards.Should().BeEmpty();
    }

    [Theory]
    [InlineData("summary", "quarter")]
    [InlineData("", "full")]
    public async Task SaveAnalyticsLayout_RejectsInvalidCards(string id, string width)
    {
        var repository = new FakeUserRepository(new User(UserId, "a@b.c"));

        var result = await CreateService(repository).SaveAnalyticsLayoutAsync(UserId, Layout((id, width)));

        result.IsFailed.Should().BeTrue();
        repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveAnalyticsLayout_RejectsDuplicateIds()
    {
        var repository = new FakeUserRepository(new User(UserId, "a@b.c"));

        var result = await CreateService(repository).SaveAnalyticsLayoutAsync(UserId, Layout(("summary", "full"), ("summary", "half")));

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetAnalyticsLayout_ReturnsNull_WhenStoredJsonIsCorrupted()
    {
        var service = CreateService(new FakeUserRepository(new User(UserId, "a@b.c") { AnalyticsLayout = "{not json" }));

        var result = await service.GetAnalyticsLayoutAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    internal sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public int SaveCount { get; private set; }

        public Task<User?> GetById(int id) => Task.FromResult(users.FirstOrDefault(u => u.Id == id));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<User?> GetByAzureOid(string azureOid) => throw new NotSupportedException();
        public Task<User?> GetByAppOid(string appOid) => throw new NotSupportedException();
        public Task<User?> GetByEmail(string email) => throw new NotSupportedException();
        public Task<User> GetOrCreateUserAsync(string azureOid, string email) => throw new NotSupportedException();
        public Task AddUser(User user) => throw new NotSupportedException();
    }
}
