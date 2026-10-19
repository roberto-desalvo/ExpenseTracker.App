using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class TestSeedServiceTests
{
    private const int TestUserId = 10;
    private const int OtherUserId = 20;

    private static readonly string[] ExpectedAccountNames =
    [
        TestSeedService.CheckingAccountName,
        TestSeedService.CreditCardAccountName,
        TestSeedService.SavingsAccountName,
        TestSeedService.BrokerAccountName,
        TestSeedService.CashAccountName,
    ];

    [Fact]
    public async Task ResetTestUserDataAsync_CreatesTestUser_WhenMissing()
    {
        var userRepository = new FakeUserRepository([new User(OtherUserId, "someone@test.com")]);
        var accountRepository = new FakeAccountRepository([]);
        var service = CreateService(userRepository, accountRepository, new FakeTransactionRepository(), new FakeTransferRepository());

        var result = await service.ResetTestUserDataAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.UserCreated.Should().BeTrue();
        result.Value.Email.Should().Be(TestSeedService.TestUserEmail);

        var created = userRepository.Users.Should().ContainSingle(u => u.Email == TestSeedService.TestUserEmail).Subject;
        created.AzureOid.Should().BeNull();
        created.IsDemo.Should().BeFalse();
        result.Value.UserId.Should().Be(created.Id);
        accountRepository.Accounts.Where(a => a.UserId == created.Id).Should().HaveCount(5);
    }

    [Fact]
    public async Task ResetTestUserDataAsync_ReplacesOnlyTestUserData_WhenUserExists()
    {
        var userRepository = new FakeUserRepository(
        [
            new User(TestUserId, TestSeedService.TestUserEmail),
            new User(OtherUserId, "someone@test.com"),
        ]);

        var oldTestAccount = new Account(100, "Vecchio conto", TestUserId);
        var otherUserAccount = new Account(200, "Conto altro utente", OtherUserId);
        var accountRepository = new FakeAccountRepository([oldTestAccount, otherUserAccount]);

        var transactionRepository = new FakeTransactionRepository();
        transactionRepository.Transactions.Add(new Transaction { Id = 1000, AccountId = oldTestAccount.Id, Amount = -10m, TransferId = 50 });
        transactionRepository.Transactions.Add(new Transaction { Id = 1001, AccountId = otherUserAccount.Id, Amount = -20m });

        var transferRepository = new FakeTransferRepository();
        transferRepository.Transfers.Add(new Transfer { Id = 50 });

        var service = CreateService(userRepository, accountRepository, transactionRepository, transferRepository);

        var result = await service.ResetTestUserDataAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.UserCreated.Should().BeFalse();
        result.Value.UserId.Should().Be(TestUserId);
        userRepository.Users.Should().HaveCount(2);

        accountRepository.Accounts.Should().NotContain(a => a.Id == oldTestAccount.Id);
        transactionRepository.Transactions.Should().NotContain(t => t.Id == 1000);
        transferRepository.Transfers.Should().NotContain(t => t.Id == 50);

        accountRepository.Accounts.Should().Contain(a => a.Id == otherUserAccount.Id);
        transactionRepository.Transactions.Should().Contain(t => t.Id == 1001);

        var testAccounts = accountRepository.Accounts.Where(a => a.UserId == TestUserId).ToList();
        testAccounts.Select(a => a.Name).Should().BeEquivalentTo(ExpectedAccountNames);

        var testAccountIds = testAccounts.Select(a => a.Id).ToHashSet();
        transactionRepository.Transactions
            .Where(t => t.Id != 1001)
            .Should().OnlyContain(t => testAccountIds.Contains(t.AccountId));
    }

    [Fact]
    public async Task ResetTestUserDataAsync_CoversFiveYears_WithMonthlySalaryAndActivityOnEveryAccount()
    {
        var userRepository = new FakeUserRepository([new User(TestUserId, TestSeedService.TestUserEmail)]);
        var accountRepository = new FakeAccountRepository([]);
        var transactionRepository = new FakeTransactionRepository();
        var service = CreateService(userRepository, accountRepository, transactionRepository, new FakeTransferRepository());

        var result = await service.ResetTestUserDataAsync();

        result.IsSuccess.Should().BeTrue();
        var today = DateTime.UtcNow.Date;
        var transactions = transactionRepository.Transactions;

        result.Value.TransactionCount.Should().Be(transactions.Count);
        transactions.Should().OnlyContain(t => t.Date.HasValue && t.Date.Value <= today);
        transactions.Min(t => t.Date!.Value).Should().BeOnOrBefore(today.AddYears(-TestSeedService.YearsOfHistory));
        transactions.Should().OnlyContain(t => t.CategoryId.HasValue && Enum.IsDefined((CategoryEnum)t.CategoryId!.Value));
        transactions.Should().OnlyContain(t => t.Amount != 0m && !string.IsNullOrWhiteSpace(t.Description));

        // Ogni mese concluso ha almeno uno stipendio accreditato sul conto corrente.
        var checkingId = accountRepository.Accounts.Single(a => a.Name == TestSeedService.CheckingAccountName).Id;
        var salaryMonths = transactions
            .Where(t => t.AccountId == checkingId && t.CategoryId == (int)CategoryEnum.WorkIncomes)
            .Select(t => new DateTime(t.Date!.Value.Year, t.Date.Value.Month, 1))
            .ToHashSet();

        var firstMonth = new DateTime(today.Year - TestSeedService.YearsOfHistory, today.Month, 1);
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        for (var month = firstMonth; month < currentMonth; month = month.AddMonths(1))
            salaryMonths.Should().Contain(month);

        // Tutti e cinque i conti hanno movimenti.
        accountRepository.Accounts.Should().OnlyContain(a => transactions.Any(t => t.AccountId == a.Id));
    }

    [Fact]
    public async Task ResetTestUserDataAsync_GeneratesBalancedTransferPairsBetweenDifferentAccounts()
    {
        var userRepository = new FakeUserRepository([new User(TestUserId, TestSeedService.TestUserEmail)]);
        var transactionRepository = new FakeTransactionRepository();
        var transferRepository = new FakeTransferRepository();
        var service = CreateService(userRepository, new FakeAccountRepository([]), transactionRepository, transferRepository);

        var result = await service.ResetTestUserDataAsync();

        result.IsSuccess.Should().BeTrue();
        transferRepository.Transfers.Should().NotBeEmpty();
        result.Value.TransferCount.Should().Be(transferRepository.Transfers.Count);

        var legsByTransfer = transactionRepository.Transactions
            .Where(t => t.TransferNavigation is not null)
            .GroupBy(t => t.TransferNavigation!)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var transfer in transferRepository.Transfers)
        {
            var legs = legsByTransfer[transfer];
            legs.Should().HaveCount(2);
            legs.Sum(t => t.Amount).Should().Be(0m);
            legs.Should().Contain(t => t.Amount < 0m);
            legs.Select(t => t.AccountId).Distinct().Should().HaveCount(2);
            legs.Select(t => t.Date).Distinct().Should().HaveCount(1);
            legs.Should().OnlyContain(t => t.CategoryId == (int)CategoryEnum.MoneyTransfers);
        }

        transactionRepository.Transactions
            .Where(t => t.CategoryId == (int)CategoryEnum.MoneyTransfers)
            .Should().OnlyContain(t => t.TransferNavigation != null);
    }

    [Fact]
    public async Task ResetTestUserDataAsync_KeepsCashBalanceNonNegative()
    {
        var userRepository = new FakeUserRepository([new User(TestUserId, TestSeedService.TestUserEmail)]);
        var accountRepository = new FakeAccountRepository([]);
        var transactionRepository = new FakeTransactionRepository();
        var service = CreateService(userRepository, accountRepository, transactionRepository, new FakeTransferRepository());

        var result = await service.ResetTestUserDataAsync();

        result.IsSuccess.Should().BeTrue();
        var cashId = accountRepository.Accounts.Single(a => a.Name == TestSeedService.CashAccountName).Id;

        // A parità di data il prelievo (importo positivo) precede le spese.
        var runningBalance = 0m;
        foreach (var transaction in transactionRepository.Transactions
                     .Where(t => t.AccountId == cashId)
                     .OrderBy(t => t.Date)
                     .ThenByDescending(t => t.Amount))
        {
            runningBalance += transaction.Amount;
            runningBalance.Should().BeGreaterThanOrEqualTo(0m);
        }
    }

    private static TestSeedService CreateService(
        FakeUserRepository userRepository,
        FakeAccountRepository accountRepository,
        FakeTransactionRepository transactionRepository,
        FakeTransferRepository transferRepository)
        => new(userRepository, accountRepository, transactionRepository, transferRepository, NullLogger<TestSeedService>.Instance);

    private sealed class FakeUserRepository(IEnumerable<User> seedUsers) : IUserRepository
    {
        public List<User> Users { get; } = [.. seedUsers];

        public Task<User?> GetByEmail(string email)
            => Task.FromResult(Users.Where(u => u.Email == email).OrderBy(u => u.Id).FirstOrDefault());

        public Task AddUser(User user)
        {
            user.Id = Users.Count == 0 ? 1 : Users.Max(u => u.Id) + 1;
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<User?> GetById(int id) => throw new NotImplementedException();
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
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions, int userId) => throw new NotImplementedException();
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

        public Task<Transfer?> GetTransfer(int id, int userId) => throw new NotImplementedException();
        public Task<Transfer?> GetTransferByExternalId(string externalId, int userId) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers(int userId) => throw new NotImplementedException();
        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date) => throw new NotImplementedException();
        public Task UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public Task DeleteTransfer(int id) => throw new NotImplementedException();
    }
}
