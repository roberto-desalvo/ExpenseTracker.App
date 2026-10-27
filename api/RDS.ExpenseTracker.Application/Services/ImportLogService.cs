using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class ImportLogService : IImportLogService
{
    private const int SourceMaxLength = 50;
    private const int FileNameMaxLength = 260;
    private const int ErrorMessageMaxLength = 4000;

    private readonly IImportLogRepository _importLogRepository;
    private readonly ILogger<ImportLogService> _logger;

    public ImportLogService(IImportLogRepository importLogRepository, ILogger<ImportLogService> logger)
    {
        _importLogRepository = importLogRepository ?? throw new ArgumentNullException(nameof(importLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordAsync(string source, string? fileName, int userId, ImportStatistics statistics, IEnumerable<string>? errors)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var errorMessages = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];

        var importLog = new ImportLog
        {
            ImportedOn = DateTime.UtcNow,
            UserId = userId,
            Source = Truncate(source, SourceMaxLength) ?? string.Empty,
            FileName = Truncate(fileName, FileNameMaxLength),
            AccountId = statistics.AccountId,
            MinTransactionDate = statistics.MinTransactionDate,
            MaxTransactionDate = statistics.MaxTransactionDate,
            NewTransactionsCount = statistics.NewTransactionsCount,
            NewTransfersCount = statistics.NewTransfersCount,
            SkippedDuplicatesCount = statistics.SkippedDuplicatesCount,
            ErrorMessage = errorMessages.Count > 0
                ? Truncate(string.Join(" | ", errorMessages), ErrorMessageMaxLength)
                : null,
        };

        try
        {
            await _importLogRepository.SaveImportLog(importLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record import log. Source={Source}, FileName={FileName}, UserId={UserId}",
                source, fileName, userId);
        }
    }

    private static string? Truncate(string? value, int maxLength)
        => value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;
}
