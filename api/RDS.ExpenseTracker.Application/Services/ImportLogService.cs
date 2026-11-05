using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class ImportLogService : IImportLogService
{
    internal const int SourceMaxLength = 50;
    internal const int FileNameMaxLength = 260;
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

        var now = DateTime.UtcNow;
        var importLog = new ImportLog
        {
            ImportedOn = now,
            CompletedOn = now,
            UserId = userId,
            Source = Truncate(source, SourceMaxLength) ?? string.Empty,
            FileName = Truncate(fileName, FileNameMaxLength),
        };
        ApplyOutcome(importLog, statistics, errors);

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

    /// <summary>
    /// Copia nel log statistiche ed errori dell'import e imposta lo stato finale:
    /// <see cref="ImportStatus.Failed"/> se ci sono errori, altrimenti <see cref="ImportStatus.Completed"/>.
    /// </summary>
    internal static void ApplyOutcome(ImportLog importLog, ImportStatistics statistics, IEnumerable<string>? errors)
    {
        var errorMessages = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];

        importLog.AccountId = statistics.AccountId ?? importLog.AccountId;
        importLog.MinTransactionDate = statistics.MinTransactionDate;
        importLog.MaxTransactionDate = statistics.MaxTransactionDate;
        importLog.NewTransactionsCount = statistics.NewTransactionsCount;
        importLog.NewTransfersCount = statistics.NewTransfersCount;
        importLog.SkippedDuplicatesCount = statistics.SkippedDuplicatesCount;
        importLog.ErrorMessage = errorMessages.Count > 0
            ? Truncate(string.Join(" | ", errorMessages), ErrorMessageMaxLength)
            : null;
        importLog.Status = errorMessages.Count > 0 ? ImportStatus.Failed : ImportStatus.Completed;
    }

    internal static string? Truncate(string? value, int maxLength)
        => value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;
}
