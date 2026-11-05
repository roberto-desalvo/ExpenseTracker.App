using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Mappings;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class AccountServiceTests
{
    private const int UserId = 3;

    private readonly FakeAccountRepository _accounts = new([
        TestAccounts.Create(1, "BBVA", UserId),
        TestAccounts.Create(2, "Spese", UserId),
    ]);

    private AccountService CreateService() => new(
        _accounts,
        new FakeAccountTypeRepository(),
        new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance).CreateMapper());

    [Fact]
    public async Task UpdateAccount_AssignsAFreeAccountType()
    {
        var result = await CreateService().UpdateAccount(new AccountDto { Id = 2, Name = "Spese", AccountTypeId = AccountType.SellaId }, UserId);

        result.IsSuccess.Should().BeTrue();
        _accounts.Updated.Should().ContainSingle().Which.AccountTypeId.Should().Be(AccountType.SellaId);
    }

    [Fact]
    public async Task UpdateAccount_Conflicts_WhenTheTypeIsAlreadyAssignedToAnotherAccount()
    {
        var result = await CreateService().UpdateAccount(new AccountDto { Id = 2, Name = "Spese", AccountTypeId = AccountType.BbvaId }, UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictResultError>();
        _accounts.Updated.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAccount_KeepsItsOwnType()
    {
        var result = await CreateService().UpdateAccount(new AccountDto { Id = 1, Name = "BBVA personale", AccountTypeId = AccountType.BbvaId }, UserId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddAccounts_Conflicts_WhenTwoNewAccountsHaveTheSameType()
    {
        var result = await CreateService().AddAccounts(
        [
            new AccountDto { Name = "Hype 1", AccountTypeId = AccountType.HypeId },
            new AccountDto { Name = "Hype 2", AccountTypeId = AccountType.HypeId },
        ], UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictResultError>();
        _accounts.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAccounts_RejectsUnknownTypes()
    {
        var result = await CreateService().AddAccounts([new AccountDto { Name = "X", AccountTypeId = 999 }], UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
    }

    private sealed class FakeAccountTypeRepository : IAccountTypeRepository
    {
        public Task<IEnumerable<AccountType>> GetAccountTypes() => Task.FromResult<IEnumerable<AccountType>>(TestAccounts.Types);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> seedAccounts) : IAccountRepository
    {
        private readonly List<Account> _accounts = [.. seedAccounts];

        public List<Account> Added { get; } = [];
        public List<Account> Updated { get; } = [];

        public Task AddAccounts(IEnumerable<Account> accounts)
        {
            Added.AddRange(accounts);
            return Task.CompletedTask;
        }

        public Task UpdateAccount(Account account)
        {
            Updated.Add(account);
            return Task.CompletedTask;
        }

        public Task<Account?> GetAccount(int id, int userId)
            => Task.FromResult(_accounts.FirstOrDefault(a => a.Id == id && a.UserId == userId));

        public Task<IEnumerable<Account>> GetAccounts(int userId)
            => Task.FromResult<IEnumerable<Account>>(_accounts.Where(a => a.UserId == userId));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<Account?> GetAccountByImportSource(int userId, BankFileSource source) => throw new NotSupportedException();
        public Task DeleteAccounts(IEnumerable<int> accountIds) => throw new NotSupportedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotSupportedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotSupportedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotSupportedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotSupportedException();
    }
}
