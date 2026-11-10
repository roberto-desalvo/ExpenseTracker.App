using AutoMapper;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Mappings;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Text;

namespace RDS.ExpenseTracker.Tests;

public class ImportJobServiceTests
{
    private const int UserId = 7;
    private const int DemoUserId = 8;
    private const int OtherUserId = 9;

    private readonly FakeImportLogRepository _logs = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeBankFileImportService _bankFileImport = new();
    private readonly FakeAccountRepository _accounts = new([
        TestAccounts.Create(1, "BBVA", UserId),
        TestAccounts.Create(2, "Revolut", UserId),
        TestAccounts.Create(3, "Conto senza tipo", UserId),
        TestAccounts.Create(4, "Sella", OtherUserId),
        TestAccounts.Create(5, "BBVA", DemoUserId),
    ]);

    private ImportJobService CreateService() => new(
        _logs,
        _accounts,
        new FakeUserRepository([new User(UserId, "user@test.local"), new User(DemoUserId, "demo@test.local", isDemo: true), new User(OtherUserId, "other@test.local")]),
        _storage,
        _bankFileImport,
        new MapperConfiguration(cfg => cfg.AddProfile<ExpenseTrackerProfile>(), NullLoggerFactory.Instance).CreateMapper(),
        NullLogger<ImportJobService>.Instance);

    private static MemoryStream Csv(string content = "a,b\n1,2\n") => new(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task EnqueueAsync_StoresTheFile_AndRecordsAQueuedImport()
    {
        var result = await CreateService().EnqueueAsync(UserId, 1, "estratto.csv", Csv());

        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.Message)));
        var log = _logs.Logs.Should().ContainSingle().Subject;
        log.Status.Should().Be(ImportStatus.Queued);
        log.UserId.Should().Be(UserId);
        log.AccountId.Should().Be(1);
        log.Source.Should().Be("BBVA CSV");
        log.FileName.Should().Be("estratto.csv");
        log.BlobName.Should().StartWith($"{UserId}/").And.EndWith(".csv");
        _storage.Files.Should().ContainKey(log.BlobName!);

        result.Value.Id.Should().Be(log.Id);
        result.Value.AccountName.Should().Be("BBVA");
        result.Value.Status.Should().Be(ImportStatus.Queued);
    }

    [Fact]
    public async Task EnqueueAsync_RejectsTheDemoUser()
    {
        var result = await CreateService().EnqueueAsync(DemoUserId, 5, "estratto.csv", Csv());

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        _logs.Logs.Should().BeEmpty();
        _storage.Files.Should().BeEmpty();
    }

    [Theory]
    [InlineData(2)] // tipo senza import
    [InlineData(3)] // conto senza tipo
    public async Task EnqueueAsync_RejectsAccountsThatDoNotSupportImports(int accountId)
    {
        var result = await CreateService().EnqueueAsync(UserId, accountId, "estratto.csv", Csv());

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
        _logs.Logs.Should().BeEmpty();
    }

    [Fact]
    public async Task EnqueueAsync_RejectsAnotherUsersAccount()
    {
        var result = await CreateService().EnqueueAsync(UserId, 4, "sella.csv", Csv());

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundResultError>();
        _storage.Files.Should().BeEmpty();
    }

    [Theory]
    [InlineData("estratto.pdf")]
    [InlineData("estratto")]
    public async Task EnqueueAsync_RejectsUnsupportedFileTypes(string fileName)
    {
        var result = await CreateService().EnqueueAsync(UserId, 1, fileName, Csv());

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
    }

    [Fact]
    public async Task EnqueueAsync_RejectsFilesOverTheSizeLimit()
    {
        using var big = new MemoryStream(new byte[IImportJobService.MaxFileSizeBytes + 1]);

        var result = await CreateService().EnqueueAsync(UserId, 1, "estratto.csv", big);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
    }

    [Fact]
    public async Task ProcessAsync_ImportsTheStoredFileIntoTheAccount_AndCompletesTheLog()
    {
        var service = CreateService();
        var queued = (await service.EnqueueAsync(UserId, 1, "estratto.xlsx", Csv("xlsx-bytes"))).Value;
        _bankFileImport.OnImport = statistics =>
        {
            statistics.NewTransactionsCount = 4;
            statistics.SkippedDuplicatesCount = 1;
            return Result.Ok(4);
        };

        await service.ProcessAsync(queued.Id);

        var call = _bankFileImport.Calls.Should().ContainSingle().Subject;
        call.Should().Be((BankFileSource.Bbva, BankFileFormat.Xlsx, "estratto.xlsx", UserId, (int?)1, "xlsx-bytes"));

        var log = _logs.Logs.Single();
        log.Status.Should().Be(ImportStatus.Completed);
        log.StartedOn.Should().NotBeNull();
        log.CompletedOn.Should().NotBeNull();
        log.NewTransactionsCount.Should().Be(4);
        log.SkippedDuplicatesCount.Should().Be(1);
        log.ErrorMessage.Should().BeNull();
        _logs.StatusHistory.Should().Equal(ImportStatus.Queued, ImportStatus.Processing, ImportStatus.Completed);
    }

    [Fact]
    public async Task ProcessAsync_MarksTheLogFailed_WhenTheImportFails()
    {
        var service = CreateService();
        var queued = (await service.EnqueueAsync(UserId, 1, "estratto.csv", Csv())).Value;
        _bankFileImport.OnImport = _ => Result.Fail("Invalid header");

        await service.ProcessAsync(queued.Id);

        var log = _logs.Logs.Single();
        log.Status.Should().Be(ImportStatus.Failed);
        log.ErrorMessage.Should().Be("Invalid header");
    }

    [Fact]
    public async Task ProcessAsync_MarksTheLogFailed_WithoutThrowing_WhenTheImportThrows()
    {
        var service = CreateService();
        var queued = (await service.EnqueueAsync(UserId, 1, "estratto.csv", Csv())).Value;
        _bankFileImport.OnImport = _ => throw new InvalidOperationException("boom");

        var act = () => service.ProcessAsync(queued.Id);

        await act.Should().NotThrowAsync();
        var log = _logs.Logs.Single();
        log.Status.Should().Be(ImportStatus.Failed);
        log.ErrorMessage.Should().Be("boom");
    }

    [Fact]
    public async Task ProcessAsync_Fails_WhenTheAccountNoLongerSupportsImports()
    {
        var service = CreateService();
        var queued = (await service.EnqueueAsync(UserId, 1, "estratto.csv", Csv())).Value;
        _accounts.Accounts.Single(a => a.Id == 1).WithType(AccountType.RevolutId);

        await service.ProcessAsync(queued.Id);

        _bankFileImport.Calls.Should().BeEmpty();
        _logs.Logs.Single().Status.Should().Be(ImportStatus.Failed);
    }

    [Theory]
    [InlineData(ImportStatus.Completed)]
    [InlineData(ImportStatus.Failed)]
    public async Task ProcessAsync_IgnoresImportsAlreadyConcluded(ImportStatus status)
    {
        var service = CreateService();
        var queued = (await service.EnqueueAsync(UserId, 1, "estratto.csv", Csv())).Value;
        _logs.Logs.Single().Status = status;

        await service.ProcessAsync(queued.Id);

        _bankFileImport.Calls.Should().BeEmpty();
        _logs.Logs.Single().Status.Should().Be(status);
    }

    [Fact]
    public async Task GetImportLogs_RejectsInvalidPaging()
    {
        var result = await CreateService().GetImportLogs(new ImportLogQueryRequest { PageSize = 0 }, UserId);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetImportLogs_IsForbidden_ForTheDemoUser()
    {
        var result = await CreateService().GetImportLogs(new ImportLogQueryRequest { PageSize = 20 }, DemoUserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
    }

    private sealed class FakeImportLogRepository : IImportLogRepository
    {
        private int _nextId = 1;

        public List<ImportLog> Logs { get; } = [];
        public List<ImportStatus> StatusHistory { get; } = [];

        public Task SaveImportLog(ImportLog importLog)
        {
            importLog.Id = _nextId++;
            Logs.Add(Copy(importLog));
            StatusHistory.Add(importLog.Status);
            return Task.CompletedTask;
        }

        public Task UpdateImportLog(ImportLog importLog)
        {
            var index = Logs.FindIndex(l => l.Id == importLog.Id);
            Logs[index] = Copy(importLog);
            StatusHistory.Add(importLog.Status);
            return Task.CompletedTask;
        }

        public Task<ImportLog?> GetImportLog(int id)
            => Task.FromResult(Logs.Where(l => l.Id == id).Select(Copy).FirstOrDefault());

        public Task<(IEnumerable<ImportLog> Items, int TotalCount)> GetPagedImportLogs(ImportLogQueryRequest request, int userId)
        {
            var items = Logs.Where(l => l.UserId == userId).ToList();
            return Task.FromResult<(IEnumerable<ImportLog>, int)>((items, items.Count));
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        // il servizio lavora su istanze staccate, come con EF (AsNoTracking)
        private static ImportLog Copy(ImportLog l) => new()
        {
            Id = l.Id, ImportedOn = l.ImportedOn, UserId = l.UserId, Status = l.Status, StartedOn = l.StartedOn,
            CompletedOn = l.CompletedOn, BlobName = l.BlobName, Source = l.Source, FileName = l.FileName,
            AccountId = l.AccountId, MinTransactionDate = l.MinTransactionDate, MaxTransactionDate = l.MaxTransactionDate,
            NewTransactionsCount = l.NewTransactionsCount, NewTransfersCount = l.NewTransfersCount,
            SkippedDuplicatesCount = l.SkippedDuplicatesCount, ErrorMessage = l.ErrorMessage,
        };
    }

    private sealed class FakeFileStorage : IImportFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public async Task SaveAsync(string name, Stream content, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Files[name] = buffer.ToArray();
        }

        public Task<Stream> OpenReadAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream(Files[name]));
    }

    private sealed class FakeBankFileImportService : IBankFileImportService
    {
        public List<(BankFileSource Source, BankFileFormat Format, string FileName, int UserId, int? AccountId, string Content)> Calls { get; } = [];
        public Func<ImportStatistics, Result<int>> OnImport { get; set; } = _ => Result.Ok(0);

        public Task<Result<int>> ImportWithoutLogAsync(BankFileSource source, BankFileFormat format, Stream content, string fileName, int userId, bool importAll, ImportStatistics statistics, int? accountId = null)
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            Calls.Add((source, format, fileName, userId, accountId, reader.ReadToEnd()));
            return Task.FromResult(OnImport(statistics));
        }

        public Task<Result<int>> ImportAsync(BankFileSource source, BankFileFormat format, Stream content, string fileName, int userId, bool importAll = false, string? logSource = null)
            => throw new NotSupportedException();

        public Task<Result<int>> RunTrackedAsync(string logSource, string? fileName, int userId, Func<ImportStatistics, Task<Result<int>>> import)
            => throw new NotSupportedException();
    }

    private sealed class FakeUserRepository(IEnumerable<User> users) : IUserRepository
    {
        private readonly List<User> _users = [.. users];

        public Task<User?> GetById(int id) => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByAzureOid(string azureOid) => throw new NotSupportedException();
        public Task<User?> GetByAppOid(string appOid) => throw new NotSupportedException();
        public Task<User?> GetByEmail(string email) => throw new NotSupportedException();
        public Task<User> GetOrCreateUserAsync(string azureOid, string email) => throw new NotSupportedException();
        public Task AddUser(User user) => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeAccountRepository(IEnumerable<Account> seedAccounts) : IAccountRepository
    {
        public List<Account> Accounts { get; } = [.. seedAccounts];

        public Task<Account?> GetAccount(int id, int userId)
            => Task.FromResult(Accounts.FirstOrDefault(a => a.Id == id && a.UserId == userId));

        public Task<IEnumerable<Account>> GetAccounts(int userId)
            => Task.FromResult<IEnumerable<Account>>(Accounts.Where(a => a.UserId == userId));

        public Task<Account?> GetAccountByImportSource(int userId, BankFileSource source)
            => Task.FromResult(Accounts.FirstOrDefault(a => a.UserId == userId && a.AccountTypeNavigation?.ImportSource == source));

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task AddAccounts(IEnumerable<Account> accounts) => throw new NotSupportedException();
        public Task UpdateAccount(Account account) => throw new NotSupportedException();
        public Task DeleteAccounts(IEnumerable<int> accountIds) => throw new NotSupportedException();
        public Task<(IEnumerable<Account> Items, int TotalCount)> GetPagedAccounts(AccountQueryRequest request, int userId) => throw new NotSupportedException();
        public Task<bool> UpdateAvailability(int accountId, decimal amount, bool saveChanges) => throw new NotSupportedException();
        public Task<decimal> GetAvailability(int accountId, int userId) => throw new NotSupportedException();
        public Task CalculateAvailabilities(IEnumerable<Transaction> transactions) => throw new NotSupportedException();
    }
}
