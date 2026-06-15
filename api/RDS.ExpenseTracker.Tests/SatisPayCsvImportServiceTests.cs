using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using System.Text;

namespace RDS.ExpenseTracker.Tests;

public class SatisPayCsvImportServiceTests
{
    [Fact]
    public async Task ImportFromCsvAsync_KeepsNegativeAmount_WhenSatispayCsvContainsNegativeValue()
    {
        var transactionRepository = new FakeTransactionRepository();
        var transferRepository = new FakeTransferRepository();
        var categoryRepository = new FakeCategoryRepository();
        var accountRepository = new FakeAccountRepository([
            new Account(3, "Satispay"),
        ]);

        var service = new SatisPayCsvImportService(
            transactionRepository,
            transferRepository,
            categoryRepository,
            accountRepository,
            new FakeSatisPayCsvOptions(),
            NullLogger<SatisPayCsvImportService>.Instance);

        const string csv = "Data;Nome;Descrizione;Importo;Tipo;Stato;Disponibilità;Disponibilità dopo la transazione;ID (Comunicalo all'Assistenza Clienti in caso di problemi)\n"
            + "12/06/2026 22:38;Il Quadrifoglio;;-€4,50;Pagamento;Approvato;-€4,50;€244,30;019ebd8e-81bd-7a5d-acbc-577b728ba080\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-satispay.csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        transactionRepository.AddedTransactions.Should().ContainSingle();

        var importedTransaction = transactionRepository.AddedTransactions.Single();
        importedTransaction.AccountId.Should().Be(3);
        importedTransaction.Amount.Should().Be(-4.50m);
        importedTransaction.ExternalId.Should().Be("019ebd8e-81bd-7a5d-acbc-577b728ba080");
        importedTransaction.CategoryId.Should().Be((int)CategoryEnum.Default);
    }

    private sealed class FakeSatisPayCsvOptions : ISatisPayCsvOptions
    {
        public string DefaultAccountName => "Satispay";
        public string? BankAccountName => null;
        public Dictionary<string, string> IbanToAccountMap => [];
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> AddedTransactions { get; } = [];

        public Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            AddedTransactions.AddRange(transactions);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds)
            => Task.FromResult(Enumerable.Empty<string>());

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transaction?> GetTransaction(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactions() => throw new NotImplementedException();
        public Task<(IEnumerable<Transaction> Items, int TotalCount, decimal TotalIncomes, decimal TotalOutcomes, decimal TotalNet)> GetPagedTransactions(TransactionQueryRequest request) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactions(TimeSeriesRequestDto request) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime Date, decimal Amount, int AccountId, int? CategoryId)>> GetTimeSeriesTransactionsUntilDate(TimeSeriesRequestDto request) => throw new NotImplementedException();
        public Task<IEnumerable<(int AccountId, decimal Balance)>> GetAccountBalances(DateTime asOfDate) => throw new NotImplementedException();
        public Task<IEnumerable<(int CategoryId, decimal Spent, decimal Earned)>> GetCategoryMonthTotals(DateTime monthStart, DateTime asOfDate) => throw new NotImplementedException();
        public Task<(decimal Spent, decimal Earned)> GetMonthTotals(DateTime monthStart, DateTime asOfDate) => throw new NotImplementedException();
        public Task<IEnumerable<(DateTime StartDate, DateTime EndDate)>> GetAvailableMonthRanges() => throw new NotImplementedException();
        public Task<IEnumerable<Transaction>> GetTransactionsByTransferId(int transferId) => throw new NotImplementedException();
        public Task<Transaction> GetLatestTransaction() => throw new NotImplementedException();
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task DeleteAllTransactions() => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }

    private sealed class FakeTransferRepository : ITransferRepository
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transfer?> GetTransfer(int id) => throw new NotImplementedException();
        public Task<Transfer?> GetTransferByExternalId(string externalId) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers() => throw new NotImplementedException();
        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date) => Task.FromResult<Transfer?>(null);
        public Task AddTransfer(Transfer transfer) => Task.CompletedTask;
        public Task UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public Task DeleteTransfer(int id) => throw new NotImplementedException();
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<IEnumerable<Category>> GetCategories(string? name = null)
            => Task.FromResult<IEnumerable<Category>>([]);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Category?> GetCategory(int id) => throw new NotImplementedException();
        public Task<Category?> GetDefaultCategory() => throw new NotImplementedException();
        public Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedCategories(CategoryQueryRequest request) => throw new NotImplementedException();
        public Task AddCategories(IEnumerable<Category> categories) => throw new NotImplementedException();
        public Task UpdateCategory(Category category) => throw new NotImplementedException();
        public Task RemoveCategory(int id) => throw new NotImplementedException();
        public Task RemoveCategory(Category category) => throw new NotImplementedException();
        public Task ReassignTransactionsToCategory(int sourceCategoryId, int targetCategoryId) => throw new NotImplementedException();
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> seedAccounts) : IAccountRepository
    {
        private readonly List<Account> _accounts = [.. seedAccounts];

        public Task AddAccounts(IEnumerable<Account> accounts)
        {
            _accounts.AddRange(accounts);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Account>> GetAccounts()
            => Task.FromResult<IEnumerable<Account>>(_accounts);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }
}