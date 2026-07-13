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

public class SellaCsvImportServiceTests
{
    private const int TestUserId = 1;

    private static readonly TransferMatchRule SellaSatispayRule = new()
    {
        AccountName1 = "Sella",
        DescriptionPattern1 = "Satispay Europe",
        DescriptionMatchMode1 = DescriptionMatchMode.StartsWith,
        AccountName2 = "Satispay",
        DescriptionPattern2 = "Ricarica Satispay",
    };


    [Fact]
    public async Task ImportFromCsvAsync_ThrowsError_WhenCodiceIdentificativoMissing()
    {
        var service = BuildService(
            out var transactionRepository,
            out _,
            new FakeSellaCsvOptions());

        const string csv = "\"Codice identificativo\",\"Data operazione\",\"Data valuta\",\"Descrizione\",\"Divisa\",\"Debito\",\"Credito\",\"Categoria\",\"Sottocategoria\",\"Etichette\",\"Note\",\n"
            + "\"\",\"10/06/2026\",\"10/06/2026\",\"Pagamento prova\",\"EUR\",\"-12,00\",\"\",\"Altre spese\",\"Varie\",\"Bonifico\",\"\",\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "sella.csv", TestUserId);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("Codice identificativo", StringComparison.OrdinalIgnoreCase));
        transactionRepository.AddedTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportFromCsvAsync_MapsIdentifierToExternalId_AndSkipsBalanceRow()
    {
        var service = BuildService(
            out var transactionRepository,
            out _,
            new FakeSellaCsvOptions());

        const string csv = "\"Codice identificativo\",\"Data operazione\",\"Data valuta\",\"Descrizione\",\"Divisa\",\"Debito\",\"Credito\",\"Categoria\",\"Sottocategoria\",\"Etichette\",\"Note\",\n"
            + "\"SEL-1001\",\"09/06/2026\",\"09/06/2026\",\"Caffetteria centro\",\"EUR\",\"-4,50\",\"\",\"Bar e ristoranti\",\"Bar\",\"Pagamento POS\",\"\",\n"
            + "\"SEL-1002\",\"10/06/2026\",\"10/06/2026\",\"Rimborso palestra\",\"EUR\",\"\",\"+25,00\",\"Entrate varie\",\"-\",\"Bonifico\",\"\",\n"
            + "\"\",\"\",\"\",\"Saldo al 10/06/2026 10:00:00\",\"0,00\",\"\",\"\",\"\",\"\",\"\",\"\",\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "sella.csv", TestUserId);

        result.IsSuccess.Should().BeTrue(string.Join(",", result.Errors.Select(e => e.Message)));
        result.Value.Should().Be(2);

        transactionRepository.AddedTransactions.Should().HaveCount(2);
        transactionRepository.AddedTransactions.Select(t => t.ExternalId)
            .Should().BeEquivalentTo(["SEL-1001", "SEL-1002"]);

        transactionRepository.AddedTransactions.Should().ContainSingle(t => t.ExternalId == "SEL-1001" && t.Amount == -4.50m);
        transactionRepository.AddedTransactions.Should().ContainSingle(t => t.ExternalId == "SEL-1002" && t.Amount == 25.00m);
    }

    [Fact]
    public async Task ImportFromCsvAsync_DeduplicatesByExternalId_WhenImportAllFalse()
    {
        var service = BuildService(
            out var transactionRepository,
            out _,
            new FakeSellaCsvOptions(),
            existingExternalIds: ["SEL-2001"]);

        const string csv = "\"Codice identificativo\",\"Data operazione\",\"Data valuta\",\"Descrizione\",\"Divisa\",\"Debito\",\"Credito\",\"Categoria\",\"Sottocategoria\",\"Etichette\",\"Note\",\n"
            + "\"SEL-2001\",\"11/06/2026\",\"11/06/2026\",\"Spesa già importata\",\"EUR\",\"-10,00\",\"\",\"Altre spese\",\"Varie\",\"Bonifico\",\"\",\n"
            + "\"SEL-2002\",\"11/06/2026\",\"11/06/2026\",\"Spesa nuova\",\"EUR\",\"-20,00\",\"\",\"Altre spese\",\"Varie\",\"Bonifico\",\"\",\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "sella.csv", TestUserId, importAll: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        transactionRepository.AddedTransactions.Should().ContainSingle();
        transactionRepository.AddedTransactions.Single().ExternalId.Should().Be("SEL-2002");
    }

    [Fact]
    public async Task ImportFromCsvAsync_SkipsDuplicateIdentifiers_InSameFile()
    {
        var service = BuildService(
            out var transactionRepository,
            out _,
            new FakeSellaCsvOptions());

        const string csv = "\"Codice identificativo\",\"Data operazione\",\"Data valuta\",\"Descrizione\",\"Divisa\",\"Debito\",\"Credito\",\"Categoria\",\"Sottocategoria\",\"Etichette\",\"Note\",\n"
            + "\"SEL-DUP-1\",\"11/06/2026\",\"11/06/2026\",\"Prima riga\",\"EUR\",\"-10,00\",\"\",\"Altre spese\",\"Varie\",\"Bonifico\",\"\",\n"
            + "\"SEL-DUP-1\",\"11/06/2026\",\"11/06/2026\",\"Seconda riga duplicata\",\"EUR\",\"-2,00\",\"\",\"Tasse\",\"Commissioni\",\"Commissioni\",\"\",\n"
            + "\"SEL-DUP-2\",\"11/06/2026\",\"11/06/2026\",\"Riga diversa\",\"EUR\",\"-5,00\",\"\",\"Altre spese\",\"Varie\",\"Bonifico\",\"\",\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "sella.csv", TestUserId, importAll: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        transactionRepository.AddedTransactions.Should().HaveCount(2);
        transactionRepository.AddedTransactions.Select(t => t.ExternalId)
            .Should().BeEquivalentTo(["SEL-DUP-1", "SEL-DUP-2"]);
    }

    [Fact]
    public async Task ImportFromCsvAsync_CreatesTransfer_WhenMappedIbanFoundInDescription()
    {
        var options = new FakeSellaCsvOptions
        {
            IbanToAccountMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["IT21W0367401600000921182111"] = "Satispay",
            },
        };

        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            options,
            accounts: [new Account(1, "Sella", TestUserId), new Account(2, "Satispay", TestUserId)]);

        const string csv = "\"Codice identificativo\",\"Data operazione\",\"Data valuta\",\"Descrizione\",\"Divisa\",\"Debito\",\"Credito\",\"Categoria\",\"Sottocategoria\",\"Etichette\",\"Note\",\n"
            + "\"SEL-TR-1\",\"12/06/2026\",\"12/06/2026\",\"Ricarica verso wallet (IT21W0367401600000921182111)\",\"EUR\",\"-50,00\",\"\",\"Trasferimenti\",\"Varie\",\"Bonifico\",\"\",\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "sella.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        transferRepository.AddedTransfers.Should().HaveCount(1);
        transferRepository.AddedTransfers.Single().ExternalId.Should().Be("SEL-TR-1");

        transactionRepository.AddedTransactions.Should().HaveCount(2);
        transactionRepository.AddedTransactions.Should().Contain(t => t.AccountId == 1 && t.Amount == -50m && t.ExternalId == "SEL-TR-1");
        transactionRepository.AddedTransactions.Should().Contain(t => t.AccountId == 2 && t.Amount == 50m && t.ExternalId == null);
        transactionRepository.AddedTransactions.Should().OnlyContain(t => t.CategoryId == (int)CategoryEnum.MoneyTransfers);
    }

    [Fact]
    public async Task ImportFromCsvAsync_RecordsSatispayEuropeRowAsUnlinkedTransferLeg_WhenNoSatispayCounterpartYet()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            new FakeSellaCsvOptions(),
            rules: [SellaSatispayRule]);

        const string csv = "\"Codice identificativo\",\"Data operazione\",\"Data valuta\",\"Descrizione\",\"Divisa\",\"Debito\",\"Credito\",\"Categoria\",\"Sottocategoria\",\"Etichette\",\"Note\",\n"
            + "\"SEL-SAT-1\",\"17/06/2026\",\"17/06/2026\",\"Satispay Europe S.A. - Ricarica wallet\",\"EUR\",\"-50,00\",\"\",\"Trasferimenti\",\"Varie\",\"Bonifico\",\"\",\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "sella.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        transferRepository.AddedTransfers.Should().BeEmpty();

        var importedTransaction = transactionRepository.AddedTransactions.Should().ContainSingle().Subject;
        importedTransaction.Amount.Should().Be(-50.00m);
        importedTransaction.CategoryId.Should().Be((int)CategoryEnum.MoneyTransfers);
        importedTransaction.TransferNavigation.Should().BeNull();
    }

    [Fact]
    public async Task ImportFromCsvAsync_LinksTransfer_WhenUnlinkedSatispayTransactionIsOneDayEarlier()
    {
        var service = BuildService(
            out var transactionRepository,
            out var transferRepository,
            new FakeSellaCsvOptions(),
            rules: [SellaSatispayRule],
            accounts: [new Account(1, "Sella", TestUserId), new Account(2, "Satispay", TestUserId)]);

        // Pre-existing unlinked Satispay recharge, dated one day before the Sella debit.
        await transactionRepository.AddTransactions([
            new Transaction
            {
                AccountId = 2,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = 50.00m,
                Description = "Ricarica Satispay",
                Date = new DateTime(2026, 6, 16, 9, 0, 0),
                ExternalId = "sat-recharge-9",
            },
        ]);

        const string csv = "\"Codice identificativo\",\"Data operazione\",\"Data valuta\",\"Descrizione\",\"Divisa\",\"Debito\",\"Credito\",\"Categoria\",\"Sottocategoria\",\"Etichette\",\"Note\",\n"
            + "\"SEL-SAT-2\",\"17/06/2026\",\"17/06/2026\",\"Satispay Europe S.A. - Ricarica wallet\",\"EUR\",\"-50,00\",\"\",\"Trasferimenti\",\"Varie\",\"Bonifico\",\"\",\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await service.ImportFromCsvAsync(stream, "sella.csv", TestUserId);

        result.IsSuccess.Should().BeTrue();
        transferRepository.AddedTransfers.Should().ContainSingle();

        var satispayCandidate = transactionRepository.AddedTransactions.Single(t => t.AccountId == 2);
        var sellaLeg = transactionRepository.AddedTransactions.Single(t => t.AccountId == 1);

        satispayCandidate.TransferNavigation.Should().NotBeNull();
        sellaLeg.TransferNavigation.Should().BeSameAs(satispayCandidate.TransferNavigation);
    }

    private static SellaCsvImportService BuildService(
        out FakeTransactionRepository transactionRepository,
        out FakeTransferRepository transferRepository,
        FakeSellaCsvOptions options,
        IEnumerable<string>? existingExternalIds = null,
        IEnumerable<Account>? accounts = null,
        IEnumerable<TransferMatchRule>? rules = null)
    {
        transactionRepository = new FakeTransactionRepository(existingExternalIds ?? []);
        transferRepository = new FakeTransferRepository();

        var categoryRepository = new FakeCategoryRepository();
        var accountRepository = new FakeAccountRepository(accounts ?? [new Account(1, "Sella", TestUserId)]);

        var transferMatchingOptions = new FakeTransferMatchingOptions { Rules = rules?.ToList() ?? [] };
        var transferMatchingService = new TransferMatchingService(transactionRepository, accountRepository, transferMatchingOptions);

        return new SellaCsvImportService(
            transactionRepository,
            transferRepository,
            categoryRepository,
            accountRepository,
            options,
            transferMatchingService,
            NullLogger<SellaCsvImportService>.Instance);
    }

    private sealed class FakeSellaCsvOptions : ISellaCsvOptions
    {
        public string DefaultAccountName { get; set; } = "Sella";
        public Dictionary<string, string> IbanToAccountMap { get; set; } = [];
    }

    private sealed class FakeTransferMatchingOptions : ITransferMatchingOptions
    {
        public List<TransferMatchRule> Rules { get; set; } = [];
    }

    private sealed class FakeTransactionRepository(IEnumerable<string> existingExternalIds) : ITransactionRepository
    {
        private readonly HashSet<string> _existingExternalIds = existingExternalIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
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

            foreach (var externalId in txs.Where(t => !string.IsNullOrWhiteSpace(t.ExternalId)).Select(t => t.ExternalId!))
                _existingExternalIds.Add(externalId);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetExistingExternalIds(IEnumerable<string> externalIds)
            => Task.FromResult(externalIds.Where(id => _existingExternalIds.Contains(id)));

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
        public List<(int FromAccountId, int ToAccountId, decimal Amount, DateTime? Date)> ExistingTransferChecks { get; } = [];

        public Task<Transfer?> GetTransferByExternalId(string externalId)
            => Task.FromResult(AddedTransfers.FirstOrDefault(t =>
                string.Equals(t.ExternalId, externalId, StringComparison.OrdinalIgnoreCase)));

        public Task<Transfer?> GetExistingTransfer(int fromAccountId, int toAccountId, decimal amount, DateTime? date)
        {
            ExistingTransferChecks.Add((fromAccountId, toAccountId, amount, date));
            return Task.FromResult<Transfer?>(null);
        }

        public Task AddTransfer(Transfer transfer)
        {
            AddedTransfers.Add(transfer);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Transfer?> GetTransfer(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Transfer>> GetTransfers() => throw new NotImplementedException();
        public Task UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public Task DeleteTransfer(int id) => throw new NotImplementedException();
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public Task<IEnumerable<Category>> GetCategories(string? name = null)
            => Task.FromResult<IEnumerable<Category>>([]);

        public Task<Category?> GetDefaultCategory()
            => Task.FromResult<Category?>(null);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<Category?> GetCategory(int id) => throw new NotImplementedException();
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
