using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Text;

namespace RDS.ExpenseTracker.Tests;

public class SatisPayCsvImportServiceTests
{
    private const int TestUserId = 1;

    private static readonly TransferMatchRule TradeRepublicRule = new()
    {
        AccountName1 = "Trade Republic",
        DescriptionPattern1 = "Sepa Direct Debit transfer to Satispay Europe S.A.",
        AccountName2 = "Satispay",
        DescriptionPattern2 = "Ricarica Satispay",
    };

    [Fact]
    public async Task ImportFromCsvAsync_KeepsNegativeAmount_WhenSatispayCsvContainsNegativeValue()
    {
        var service = BuildService(
            out var transactionRepository,
            out _,
            out _,
            accounts: [new Account(3, "Satispay", TestUserId)]);

        const string csv = "Data;Nome;Descrizione;Importo;Tipo;Stato;Disponibilità;Disponibilità dopo la transazione;ID (Comunicalo all'Assistenza Clienti in caso di problemi)\n"
            + "12/06/2026 22:38;Il Quadrifoglio;;-€4,50;Pagamento;Approvato;-€4,50;€244,30;019ebd8e-81bd-7a5d-acbc-577b728ba080\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-satispay.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        transactionRepository.AddedTransactions.Should().ContainSingle();

        var importedTransaction = transactionRepository.AddedTransactions.Single();
        importedTransaction.AccountId.Should().Be(3);
        importedTransaction.Amount.Should().Be(-4.50m);
        importedTransaction.ExternalId.Should().Be("019ebd8e-81bd-7a5d-acbc-577b728ba080");
        importedTransaction.CategoryId.Should().Be((int)CategoryEnum.Default);
    }

    [Fact]
    public async Task ImportFromCsvAsync_RecordsRicaricaSatispayAsUnlinkedTransferLeg_WhenNoTradeRepublicCounterpartYet()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _,
            rules: [TradeRepublicRule],
            accounts: [new Account(3, "Satispay", TestUserId)]);

        const string csv = "Data;Nome;Descrizione;Importo;Tipo;Stato;Disponibilità;Disponibilità dopo la transazione;ID (Comunicalo all'Assistenza Clienti in caso di problemi)\n"
            + "16/06/2026 10:00:00;;Ricarica Satispay;€50,00;BANK_RECHARGE;Approvato;€50,00;€50,00;sat-recharge-1\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-satispay.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        transferRepository.AddedTransfers.Should().BeEmpty();

        var importedTransaction = transactionRepository.AddedTransactions.Should().ContainSingle().Subject;
        importedTransaction.AccountId.Should().Be(3);
        importedTransaction.Amount.Should().Be(50.00m);
        importedTransaction.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
        importedTransaction.TransferNavigation.Should().BeNull();
    }

    [Fact]
    public async Task ImportFromCsvAsync_LinksTransfer_WhenUnlinkedTradeRepublicTransactionIsOneDayLater()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _,
            rules: [TradeRepublicRule],
            accounts: [new Account(1, "Trade Republic", TestUserId), new Account(3, "Satispay", TestUserId)]);

        // Pre-existing unlinked TR transaction, dated one day after the Satispay recharge.
        await transactionRepository.AddTransactions([
            new Transaction
            {
                AccountId = 1,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = -50.00m,
                Description = "Sepa Direct Debit transfer to Satispay Europe S.A.",
                Date = new DateTime(2026, 6, 17, 8, 0, 0),
                ExternalId = "tr-debit-1",
            },
        ]);

        const string csv = "Data;Nome;Descrizione;Importo;Tipo;Stato;Disponibilità;Disponibilità dopo la transazione;ID (Comunicalo all'Assistenza Clienti in caso di problemi)\n"
            + "16/06/2026 10:00:00;;Ricarica Satispay;€50,00;BANK_RECHARGE;Approvato;€50,00;€50,00;sat-recharge-1\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-satispay.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        transferRepository.AddedTransfers.Should().ContainSingle();

        var trCandidate = transactionRepository.AddedTransactions.Single(t => t.AccountId == 1);
        var satispayLeg = transactionRepository.AddedTransactions.Single(t => t.AccountId == 3);

        trCandidate.TransferNavigation.Should().NotBeNull();
        satispayLeg.TransferNavigation.Should().BeSameAs(trCandidate.TransferNavigation);
        trCandidate.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
        satispayLeg.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
    }

    [Fact]
    public async Task ImportFromCsvAsync_DoesNotLink_WhenDateGapIsNotExactlyOneDay()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _,
            rules: [TradeRepublicRule],
            accounts: [new Account(1, "Trade Republic", TestUserId), new Account(3, "Satispay", TestUserId)]);

        // TR transaction dated two days after the Satispay recharge - not a valid pair.
        await transactionRepository.AddTransactions([
            new Transaction
            {
                AccountId = 1,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = -50.00m,
                Description = "Sepa Direct Debit transfer to Satispay Europe S.A.",
                Date = new DateTime(2026, 6, 18, 8, 0, 0),
                ExternalId = "tr-debit-2",
            },
        ]);

        const string csv = "Data;Nome;Descrizione;Importo;Tipo;Stato;Disponibilità;Disponibilità dopo la transazione;ID (Comunicalo all'Assistenza Clienti in caso di problemi)\n"
            + "16/06/2026 10:00:00;;Ricarica Satispay;€50,00;BANK_RECHARGE;Approvato;€50,00;€50,00;sat-recharge-2\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-satispay.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        transferRepository.AddedTransfers.Should().BeEmpty();

        var satispayLeg = transactionRepository.AddedTransactions.Single(t => t.AccountId == 3);
        satispayLeg.TransferNavigation.Should().BeNull();
        satispayLeg.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
    }

    [Fact]
    public async Task ImportFromCsvAsync_ExcludesAlreadyLinkedCandidate_AndClaimsOldestUnlinkedOne()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            out _,
            rules: [TradeRepublicRule],
            accounts: [new Account(1, "Trade Republic", TestUserId), new Account(3, "Satispay", TestUserId)]);

        var alreadyLinkedTransfer = new Transfer { CreatedOn = DateTime.UtcNow };

        await transactionRepository.AddTransactions([
            // Already linked to another transfer - must be excluded from matching.
            new Transaction
            {
                AccountId = 1,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = -50.00m,
                Description = "Sepa Direct Debit transfer to Satispay Europe S.A.",
                Date = new DateTime(2026, 6, 17, 8, 0, 0),
                ExternalId = "tr-debit-linked",
                TransferNavigation = alreadyLinkedTransfer,
            },
            // Still unlinked - this is the one that should be claimed.
            new Transaction
            {
                AccountId = 1,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = -50.00m,
                Description = "Sepa Direct Debit transfer to Satispay Europe S.A.",
                Date = new DateTime(2026, 6, 17, 9, 0, 0),
                ExternalId = "tr-debit-unlinked",
            },
        ]);

        const string csv = "Data;Nome;Descrizione;Importo;Tipo;Stato;Disponibilità;Disponibilità dopo la transazione;ID (Comunicalo all'Assistenza Clienti in caso di problemi)\n"
            + "16/06/2026 10:00:00;;Ricarica Satispay;€50,00;BANK_RECHARGE;Approvato;€50,00;€50,00;sat-recharge-3\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-satispay.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        transferRepository.AddedTransfers.Should().ContainSingle();

        var claimedCandidate = transactionRepository.AddedTransactions.Single(t => t.ExternalId == "tr-debit-unlinked");
        var satispayLeg = transactionRepository.AddedTransactions.Single(t => t.AccountId == 3);

        satispayLeg.TransferNavigation.Should().BeSameAs(claimedCandidate.TransferNavigation);
        claimedCandidate.TransferNavigation.Should().NotBeSameAs(alreadyLinkedTransfer);
    }

    private static SatisPayCsvImportService BuildService(
        out FakeTransactionRepository transactionRepository,
        out FakeTransferRepository transferRepository,
        out FakeAccountRepository accountRepository,
        IEnumerable<TransferMatchRule>? rules = null,
        IEnumerable<Account>? accounts = null)
    {
        transactionRepository = new FakeTransactionRepository();
        transferRepository = new FakeTransferRepository();
        accountRepository = new FakeAccountRepository(accounts ?? [new Account(3, "Satispay", TestUserId)]);

        var categoryRepository = new FakeCategoryRepository();
        var transferMatchingOptions = new FakeTransferMatchingOptions { Rules = rules?.ToList() ?? [] };
        var transferMatchingService = new TransferMatchingService(transactionRepository, accountRepository, transferMatchingOptions);

        return new SatisPayCsvImportService(
            transactionRepository,
            transferRepository,
            categoryRepository,
            accountRepository,
            new FakeSatisPayCsvOptions(),
            transferMatchingService,
            NullLogger<SatisPayCsvImportService>.Instance);
    }

    private sealed class FakeSatisPayCsvOptions : ISatisPayCsvOptions
    {
        public string DefaultAccountName => "Satispay";
        public string? BankAccountName => null;
        public Dictionary<string, string> IbanToAccountMap => [];
    }

    private sealed class FakeTransferMatchingOptions : ITransferMatchingOptions
    {
        public List<TransferMatchRule> Rules { get; set; } = [];
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private int _nextId = 1;

        public List<Transaction> AddedTransactions { get; } = [];

        public Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            var txs = transactions.ToList();
            foreach (var tx in txs)
            {
                if (tx.Id == 0)
                    tx.Id = _nextId++;
            }

            AddedTransactions.AddRange(txs);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds)
            => Task.FromResult(Enumerable.Empty<string>());

        public Task<List<Transaction>> GetUnlinkedTransferCandidates(int accountId, decimal amount, DateTime date, string descriptionContains)
        {
            var targetDate = date.Date;

            var result = AddedTransactions
                .Where(t =>
                    t.AccountId == accountId &&
                    t.TransferNavigation == null &&
                    t.Amount == amount &&
                    t.Date.HasValue && t.Date.Value.Date == targetDate &&
                    t.Description.Contains(descriptionContains, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .ToList();

            return Task.FromResult(result);
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
        public Task UpdateTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task DeleteTransaction(int id) => throw new NotImplementedException();
        public Task DeleteAllTransactions() => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction) => throw new NotImplementedException();
        public Task<int> AddTransaction(Transaction transaction, bool saveChanges) => throw new NotImplementedException();
        public Task ResetTransactions(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }

    private sealed class FakeTransferRepository : ITransferRepository
    {
        public List<Transfer> AddedTransfers { get; } = [];

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transfer?> GetTransfer(int id) => throw new NotImplementedException();
        public Task<Transfer?> GetTransferByExternalId(string externalId) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers() => throw new NotImplementedException();
        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date) => Task.FromResult<Transfer?>(null);
        public Task AddTransfer(Transfer transfer)
        {
            AddedTransfers.Add(transfer);
            return Task.CompletedTask;
        }
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

        public Task<IEnumerable<Account>> GetAccounts(int userId)
            => Task.FromResult<IEnumerable<Account>>(_accounts.Where(a => a.UserId == userId));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task UpdateAccount(Account account) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id) => throw new NotImplementedException();
        public Task<Account?> GetAccount(int id, int userId) => throw new NotImplementedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotImplementedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotImplementedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotImplementedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotImplementedException();
    }
}
