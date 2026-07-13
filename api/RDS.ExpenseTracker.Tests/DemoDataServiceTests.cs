using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class DemoDataServiceTests
{
    private const int DemoUserId = 1;
    private const int OtherUserId = 2;

    private static readonly CategoryEnum[] AllowedCategories =
    [
        CategoryEnum.WorkIncomes,
        CategoryEnum.Housing,
        CategoryEnum.MoneyTransfers,
        CategoryEnum.Gifts,
        CategoryEnum.FoodAndBeverage,
        CategoryEnum.Transportation,
        CategoryEnum.Entertainment,
        CategoryEnum.Clothes,
        CategoryEnum.HealthAndFitness,
    ];

    [Fact]
    public async Task GenerateDemoDataAsync_ReturnsForbidden_WhenUserIsNotDemo()
    {
        var userRepository = new FakeUserRepository([new User(DemoUserId, "not-demo@test.com", isDemo: false)]);
        var accountRepository = new FakeAccountRepository([]);
        var transactionRepository = new FakeTransactionRepository();
        var transferRepository = new FakeTransferRepository();

        var service = new DemoDataService(
            userRepository, accountRepository, transactionRepository, transferRepository, NullLogger<DemoDataService>.Instance);

        var result = await service.GenerateDemoDataAsync(DemoUserId);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<DomainResultError>().Should().ContainSingle(e => e.Kind == DomainErrorKind.Forbidden);
        accountRepository.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateDemoDataAsync_ResetsOnlyDemoUserData_AndGeneratesNewAccounts()
    {
        var userRepository = new FakeUserRepository([new User(DemoUserId, "demo@test.com", isDemo: true)]);

        var oldDemoAccount = new Account(100, "Vecchio conto", DemoUserId);
        var otherUserAccount = new Account(200, "Conto altro utente", OtherUserId);
        var accountRepository = new FakeAccountRepository([oldDemoAccount, otherUserAccount]);

        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(new Transaction
        {
            Id = 1000,
            AccountId = oldDemoAccount.Id,
            Amount = -10m,
            Description = "Vecchia transazione",
            TransferId = 50,
        });
        transactionRepository.Transactions.Add(new Transaction
        {
            Id = 1001,
            AccountId = otherUserAccount.Id,
            Amount = -20m,
            Description = "Transazione di un altro utente",
        });

        var transferRepository = new FakeTransferRepository();
        transferRepository.Transfers.Add(new Transfer { Id = 50 });

        var service = new DemoDataService(
            userRepository, accountRepository, transactionRepository, transferRepository, NullLogger<DemoDataService>.Instance);

        var result = await service.GenerateDemoDataAsync(DemoUserId);

        result.IsSuccess.Should().BeTrue();

        accountRepository.Accounts.Should().NotContain(a => a.Id == oldDemoAccount.Id);
        transferRepository.Transfers.Should().NotContain(t => t.Id == 50);
        transactionRepository.Transactions.Should().NotContain(t => t.Id == 1000);

        accountRepository.Accounts.Should().Contain(a => a.Id == otherUserAccount.Id);
        transactionRepository.Transactions.Should().Contain(t => t.Id == 1001);

        var newDemoAccounts = accountRepository.Accounts.Where(a => a.UserId == DemoUserId).ToList();
        newDemoAccounts.Should().HaveCount(3);
        newDemoAccounts.Select(a => a.Name).Should().BeEquivalentTo(["Conto Corrente", "Carta di Credito", "Risparmio"]);

        var newDemoAccountIds = newDemoAccounts.Select(a => a.Id).ToHashSet();
        var generatedTransactions = transactionRepository.Transactions
            .Where(t => t.Id != 1001)
            .ToList();

        generatedTransactions.Should().NotBeEmpty();
        generatedTransactions.Should().OnlyContain(t => newDemoAccountIds.Contains(t.AccountId));
        generatedTransactions.Should().OnlyContain(t => t.CategoryId.HasValue && AllowedCategories.Contains((CategoryEnum)t.CategoryId!.Value));
        generatedTransactions.Should().Contain(t => t.CategoryId == (int)CategoryEnum.WorkIncomes);
        generatedTransactions.Should().Contain(t => t.CategoryId == (int)CategoryEnum.Housing);
    }

    [Fact]
    public async Task GenerateDemoDataAsync_GeneratesLinkedTransferTransactionsWithOppositeSigns()
    {
        var userRepository = new FakeUserRepository([new User(DemoUserId, "demo@test.com", isDemo: true)]);
        var accountRepository = new FakeAccountRepository([]);
        var transactionRepository = new FakeTransactionRepository();
        var transferRepository = new FakeTransferRepository();

        var service = new DemoDataService(
            userRepository, accountRepository, transactionRepository, transferRepository, NullLogger<DemoDataService>.Instance);

        var result = await service.GenerateDemoDataAsync(DemoUserId);

        result.IsSuccess.Should().BeTrue();
        transferRepository.Transfers.Should().NotBeEmpty();

        foreach (var transfer in transferRepository.Transfers)
        {
            var legs = transactionRepository.Transactions.Where(t => ReferenceEquals(t.TransferNavigation, transfer)).ToList();
            legs.Should().HaveCount(2);
            legs.Should().Contain(t => t.Amount < 0m);
            legs.Should().Contain(t => t.Amount > 0m);
            legs.Sum(t => t.Amount).Should().Be(0m);
            legs.Should().OnlyContain(t => t.CategoryId == (int)CategoryEnum.MoneyTransfers);
        }
    }

    private sealed class FakeUserRepository(IEnumerable<User> seedUsers) : IUserRepository
    {
        private readonly List<User> _users = [.. seedUsers];

        public Task<User?> GetById(int id)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<User?> GetByAzureOid(string azureOid) => throw new NotImplementedException();
        public Task<User?> GetByAppOid(string appOid) => throw new NotImplementedException();
        public Task<User> GetOrCreateUserAsync(string azureOid, string email) => throw new NotImplementedException();
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> seedAccounts) : IAccountRepository
    {
        private int _nextId = seedAccounts.Any() ? seedAccounts.Max(a => a.Id) + 1 : 1;

        public List<Account> Accounts { get; } = [.. seedAccounts];

        public Task AddAccounts(IEnumerable<Account> accounts)
        {
            foreach (var account in accounts)
            {
                account.Id = _nextId++;
                Accounts.Add(account);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAccounts(IEnumerable<int> accountIds)
        {
            var ids = accountIds.ToList();
            Accounts.RemoveAll(a => ids.Contains(a.Id));
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Account>> GetAccounts(int userId)
            => Task.FromResult<IEnumerable<Account>>(Accounts.Where(a => a.UserId == userId).ToList());

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Account>> GetAccounts() => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id, int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Transactions { get; } = [];

        public Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            Transactions.AddRange(transactions);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<int>> DeleteTransactionsByAccountIds(IEnumerable<int> accountIds)
        {
            var ids = accountIds.ToList();
            var toRemove = Transactions.Where(t => ids.Contains(t.AccountId)).ToList();
            var transferIds = toRemove
                .Where(t => t.TransferId.HasValue)
                .Select(t => t.TransferId!.Value)
                .Distinct()
                .ToList();

            foreach (var transaction in toRemove)
                Transactions.Remove(transaction);

            return Task.FromResult<IEnumerable<int>>(transferIds);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transaction?> GetTransaction(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactions() => throw new NotImplementedException();
        public Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request) => throw new NotImplementedException();
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
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }

    private sealed class FakeTransferRepository : ITransferRepository
    {
        public List<Transfer> Transfers { get; } = [];

        public Task AddTransfer(Transfer transfer)
        {
            Transfers.Add(transfer);
            return Task.CompletedTask;
        }

        public Task DeleteTransfers(IEnumerable<int> transferIds)
        {
            var ids = transferIds.ToList();
            Transfers.RemoveAll(t => ids.Contains(t.Id));
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transfer?> GetTransfer(int id) => throw new NotImplementedException();
        public Task<Transfer?> GetTransferByExternalId(string externalId) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers() => throw new NotImplementedException();
        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date) => throw new NotImplementedException();
        public Task UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public Task DeleteTransfer(int id) => throw new NotImplementedException();
    }
}
