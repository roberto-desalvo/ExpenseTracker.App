using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;
using System.Text;

namespace RDS.ExpenseTracker.Tests;

public class BbvaCsvImportServiceTests
{
    [Fact]
    public async Task ImportFromCsvAsync_ImportsRowsFromRepositoryBbvaCsvFile()
    {
        var transactionRepository = new FakeTransactionRepository();
        var categoryRepository = new FakeCategoryRepository();
        var accountRepository = new FakeAccountRepository([
            new Account(1, "BBVA"),
        ]);

        var service = new BbvaCsvImportService(
            transactionRepository,
            categoryRepository,
            accountRepository,
            new FakeBbvaCsvOptions(),
            NullLogger<BbvaCsvImportService>.Instance);

        var filePath = FindFileUpwards("transazioni-bbva.csv");
        filePath.Should().NotBeNull("the repository sample file should exist for this regression test");

        using var stream = File.OpenRead(filePath!);

        var result = await service.ImportFromCsvAsync(stream, "transazioni-bbva.csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        transactionRepository.AddedTransactions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ImportFromCsvAsync_ImportsRowsFromBbvaSampleCsv()
    {
        var transactionRepository = new FakeTransactionRepository();
        var categoryRepository = new FakeCategoryRepository();
        var accountRepository = new FakeAccountRepository([
            new Account(1, "BBVA"),
        ]);

        var service = new BbvaCsvImportService(
            transactionRepository,
            categoryRepository,
            accountRepository,
            new FakeBbvaCsvOptions(),
            NullLogger<BbvaCsvImportService>.Instance);

        const string csv = """
;;;;;;;
;;Ultime transazioni;;;;;;
;;Data di generazione del rapporto: 13/06/2026;;;;;;
;;;;;;;
Data valuta;Data;Parola chiave;Movimento;Importo;Valuta;Disponibile;Valuta;Osservazioni
01/06/2026;02/06/2026;Liquidazione interessi-commissioni-spese;;0,32;EUR;0,32;EUR;ACCREDITO  0,50% A FRONTE DEL SALDO MENSILE
11/05/2026;11/05/2026;Bonifico eseguito;Trasferimento su conto personale;-33,27;EUR;0;EUR;Trasferimento su conto personale
11/05/2026;11/05/2026;Bonifico eseguito;Trasferimento su conto personale - 3;-1000;EUR;33,27;EUR;Trasferimento su conto personale - 3
11/05/2026;11/05/2026;Bonifico eseguito;Trasferimento su conto personale;-1000;EUR;1033,27;EUR;Trasferimento su conto personale
11/05/2026;11/05/2026;Bonifico eseguito;Trasferimento su conto personale;-1000;EUR;2033,27;EUR;Trasferimento su conto personale
10/05/2026;11/05/2026;Carrefour 0613;Pagamento con carta;-3,29;EUR;3033,27;EUR;5179090009681631 CARREFOUR 0613           TORINO       IT
09/05/2026;11/05/2026;Www.amazon.* nh2qs1g04;Pagamento con carta;-25,47;EUR;3036,56;EUR;5179090009681631 WWW.AMAZON.IT            LUXEMBOURG   LU
09/05/2026;11/05/2026;Glovo;Pagamento con carta;-14,35;EUR;3062,03;EUR;5179090009681631 Glovo                    MILANO       IT
09/05/2026;11/05/2026;Supermercato crai code;Pagamento con carta;-5,6;EUR;3076,38;EUR;5179090009681631 SUPERMERCATO CRAI CODE   TORINO       IT
07/05/2026;07/05/2026;Supermercato crai code;Pagamento con carta;-14,36;EUR;3081,98;EUR;5179090009681631 SUPERMERCATO CRAI CODE   TORINO       IT
05/05/2026;05/05/2026;Sumup  *non solo led m;Pagamento con carta;-13;EUR;3096,34;EUR;5179090009681631 SumUp  *NON SOLO LED M   TORINO       IT
05/05/2026;05/05/2026;Amazon* n603t0qq4;Pagamento con carta;-75,89;EUR;3109,34;EUR;5179090009681631 AMAZON                   LUXEMBOURG   LU
05/05/2026;05/05/2026;Busso bruno;Pagamento con carta;-8,6;EUR;3185,23;EUR;5179090009681631 BUSSO BRUNO              TORINO       IT
05/05/2026;05/05/2026;Supermercato crai code;Pagamento con carta;-20,55;EUR;3193,83;EUR;5179090009681631 SUPERMERCATO CRAI CODE   TORINO       IT
01/05/2026;05/05/2026;Liquidazione interessi-commissioni-spese;;4,52;EUR;3214,38;EUR;ACCREDITO  2,00% A FRONTE DEL SALDO MENSILE
04/05/2026;04/05/2026;Esselunga torino porta;Pagamento con carta;-8,86;EUR;3209,86;EUR;5179090009681631 ESSELUNGA TORINO PORTA   TORINO       IT
04/05/2026;04/05/2026;Vinted;Pagamento con carta;-37,89;EUR;3218,72;EUR;5179090009681631 Vinted                   Vilnius      LT
03/05/2026;04/05/2026;Glovo;Pagamento con carta;-12;EUR;3256,61;EUR;5179090009681631 Glovo                    MILANO       IT
02/05/2026;04/05/2026;Hotel del peso;Pagamento con carta;-170;EUR;3268,61;EUR;5179090009681631 HOTEL DEL PESO           SAN MICHELE MIT
01/05/2026;04/05/2026;Tabaccaio n vendita 5.;Pagamento con carta;-6,3;EUR;3438,61;EUR;5179090009681631 TABACCAIO N VENDITA 5.   TORINO       IT
01/05/2026;04/05/2026;Carrefour 0755;Pagamento con carta;-5,33;EUR;3444,91;EUR;5179090009681631 CARREFOUR 0755           TORINO       IT
29/04/2026;29/04/2026;Ficini srl;Pagamento con carta;-5,12;EUR;3450,24;EUR;5179090009681631 FICINI SRL               TORINO       IT
29/04/2026;29/04/2026;Il corallo 2 snc;Pagamento con carta;-4,5;EUR;3455,36;EUR;5179090009681631 IL CORALLO 2 SNC         TORINO       IT
29/04/2026;29/04/2026;Tosco ortofrutta srls;Pagamento con carta;-20,5;EUR;3459,86;EUR;5179090009681631 TOSCO ORTOFRUTTA SRLS    TORINO       IT
27/04/2026;28/04/2026;Farmacia comunale n. 4;Pagamento con carta;-26,8;EUR;3480,36;EUR;5179090009681631 FARMACIA COMUNALE N. 4   TORINO       IT
27/04/2026;27/04/2026;Poke wood;Pagamento con carta;-6;EUR;3507,16;EUR;5179090009681631 POKE WOOD                TORINO       IT
26/04/2026;27/04/2026;Il rospetto;Pagamento con carta;-22;EUR;3513,16;EUR;5179090009681631 IL ROSPETTO              TORINO       IT
23/04/2026;24/04/2026;La bomba;Pagamento con carta;-2,5;EUR;3535,16;EUR;5179090009681631 LA BOMBA                 TORINO       IT
23/04/2026;24/04/2026;Madama cristina;Pagamento con carta;-7,5;EUR;3537,66;EUR;5179090009681631 MADAMA CRISTINA          TORINO       IT
21/04/2026;22/04/2026;Carrefour 0613;Pagamento con carta;-4,26;EUR;3545,16;EUR;5179090009681631 CARREFOUR 0613           TORINO       IT
20/04/2026;21/04/2026;Bella instanbul;Pagamento con carta;-7,5;EUR;3549,42;EUR;5179090009681631 BELLA INSTANBUL          TORINO       IT
20/04/2026;20/04/2026;Sumup  *barber shop by;Pagamento con carta;-15;EUR;3556,92;EUR;5179090009681631 SumUp  *Barber Shop by   Torino       IT
20/04/2026;20/04/2026;Decathlon 00001530;Pagamento con carta;-13,99;EUR;3571,92;EUR;5179090009681631 DECATHLON 00001530       TORINO       IT
19/04/2026;20/04/2026;Glovo;Pagamento con carta;-10,5;EUR;3585,91;EUR;5179090009681631 Glovo                    MILANO       IT
19/04/2026;20/04/2026;Progetto montagna snc;Pagamento con carta;-23;EUR;3596,41;EUR;5179090009681631 PROGETTO MONTAGNA SNC    BALME        IT
19/04/2026;20/04/2026;Amore;Pagamento con carta;-17,8;EUR;3619,41;EUR;5179090009681631 AMORE                    TORINO       IT
19/04/2026;20/04/2026;The indipendent;Pagamento con carta;-10;EUR;3637,21;EUR;5179090009681631 THE INDIPENDENT          TORINO       IT
18/04/2026;20/04/2026;Circolo sud aps;Pagamento con carta;-3;EUR;3647,21;EUR;5179090009681631 CIRCOLO SUD APS          TORINO       IT
18/04/2026;20/04/2026;Circolo sud aps;Pagamento con carta;-3;EUR;3650,21;EUR;5179090009681631 CIRCOLO SUD APS          TORINO       IT
18/04/2026;20/04/2026;Sumup  *gruppo pension;Pagamento con carta;-24;EUR;3653,21;EUR;5179090009681631 SumUp  *Gruppo Pension   TORINO       IT
""";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-bbva.csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(40);
        transactionRepository.AddedTransactions.Should().HaveCount(40);
        transactionRepository.AddedTransactions.Should().OnlyContain(t => t.AccountId == 1);
        transactionRepository.AddedTransactions
            .Select(t => t.ExternalId)
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ImportFromCsvAsync_GeneratesUniqueExternalIds_WhenRowsHaveSameDateAmountButDifferentAvailable()
    {
        var transactionRepository = new FakeTransactionRepository();
        var categoryRepository = new FakeCategoryRepository();
        var accountRepository = new FakeAccountRepository([
            new Account(1, "BBVA"),
        ]);

        var service = new BbvaCsvImportService(
            transactionRepository,
            categoryRepository,
            accountRepository,
            new FakeBbvaCsvOptions(),
            NullLogger<BbvaCsvImportService>.Instance);

        const string csv = """
;;;;;;;
Data valuta;Data;Parola chiave;Movimento;Importo;Valuta;Disponibile;Valuta;Osservazioni
11/05/2026;11/05/2026;Bonifico eseguito;Trasferimento su conto personale;-1000;EUR;1033,27;EUR;Trasferimento su conto personale
11/05/2026;11/05/2026;Bonifico eseguito;Trasferimento su conto personale;-1000;EUR;2033,27;EUR;Trasferimento su conto personale
""";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsvAsync(stream, "transazioni-bbva.csv");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        transactionRepository.AddedTransactions.Should().HaveCount(2);
        transactionRepository.AddedTransactions
            .Select(t => t.ExternalId)
            .Should()
            .OnlyHaveUniqueItems();

        transactionRepository.AddedTransactions
            .Select(t => t.ExternalId)
            .Should()
            .OnlyContain(id => id != null && id.StartsWith("BBVA:v3:", StringComparison.Ordinal));
    }

    private sealed class FakeBbvaCsvOptions : IBbvaCsvOptions
    {
        public string DefaultAccountName => "BBVA";
    }

    private static string? FindFileUpwards(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return null;
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
