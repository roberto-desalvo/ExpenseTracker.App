using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Tests;

public class ImportLogServiceTests
{
    private const int TestUserId = 1;

    [Fact]
    public async Task RecordAsync_SavesStatistics_WithoutError_WhenImportSucceeded()
    {
        var repository = new FakeImportLogRepository();
        var service = new ImportLogService(repository, NullLogger<ImportLogService>.Instance);

        var statistics = new ImportStatistics
        {
            AccountId = 7,
            NewTransactionsCount = 5,
            NewTransfersCount = 1,
            SkippedDuplicatesCount = 3,
        };
        statistics.TrackDates([new DateTime(2026, 3, 10), null, new DateTime(2026, 1, 2), new DateTime(2026, 2, 15)]);

        await service.RecordAsync("Sella CSV", "sella.csv", TestUserId, statistics, errors: null);

        var log = repository.SavedLogs.Should().ContainSingle().Subject;
        log.UserId.Should().Be(TestUserId);
        log.Source.Should().Be("Sella CSV");
        log.FileName.Should().Be("sella.csv");
        log.AccountId.Should().Be(7);
        log.MinTransactionDate.Should().Be(new DateTime(2026, 1, 2));
        log.MaxTransactionDate.Should().Be(new DateTime(2026, 3, 10));
        log.NewTransactionsCount.Should().Be(5);
        log.NewTransfersCount.Should().Be(1);
        log.SkippedDuplicatesCount.Should().Be(3);
        log.ErrorMessage.Should().BeNull();
        log.ImportedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task RecordAsync_JoinsAndTruncatesErrors_WhenImportFailed()
    {
        var repository = new FakeImportLogRepository();
        var service = new ImportLogService(repository, NullLogger<ImportLogService>.Instance);

        await service.RecordAsync("BBVA CSV", "bbva.csv", TestUserId, new ImportStatistics(),
            ["Import failed: first", "", new string('x', 5000)]);

        var log = repository.SavedLogs.Should().ContainSingle().Subject;
        log.ErrorMessage.Should().StartWith("Import failed: first | xxx");
        log.ErrorMessage!.Length.Should().Be(4000);
    }

    [Fact]
    public async Task RecordAsync_DoesNotThrow_WhenRepositoryFails()
    {
        var repository = new FakeImportLogRepository { ThrowOnSave = true };
        var service = new ImportLogService(repository, NullLogger<ImportLogService>.Instance);

        var act = () => service.RecordAsync("Excel", "import.xlsx", TestUserId, new ImportStatistics(), null);

        await act.Should().NotThrowAsync();
    }

    private sealed class FakeImportLogRepository : IImportLogRepository
    {
        public List<ImportLog> SavedLogs { get; } = [];
        public bool ThrowOnSave { get; init; }

        public Task SaveImportLog(ImportLog importLog)
        {
            if (ThrowOnSave)
                throw new InvalidOperationException("Database unavailable");

            SavedLogs.Add(importLog);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
