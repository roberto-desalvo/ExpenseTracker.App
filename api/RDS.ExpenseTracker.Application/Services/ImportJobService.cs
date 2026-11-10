using AutoMapper;
using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class ImportJobService : IImportJobService
{
    private const int MaxPageSize = 100;

    private readonly IImportLogRepository _importLogRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUserRepository _userRepository;
    private readonly IImportFileStorage _fileStorage;
    private readonly IBankFileImportService _bankFileImportService;
    private readonly IMapper _mapper;
    private readonly ILogger<ImportJobService> _logger;

    public ImportJobService(
        IImportLogRepository importLogRepository,
        IAccountRepository accountRepository,
        IUserRepository userRepository,
        IImportFileStorage fileStorage,
        IBankFileImportService bankFileImportService,
        IMapper mapper,
        ILogger<ImportJobService> logger)
    {
        _importLogRepository = importLogRepository ?? throw new ArgumentNullException(nameof(importLogRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _bankFileImportService = bankFileImportService ?? throw new ArgumentNullException(nameof(bankFileImportService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ImportLogDto>> EnqueueAsync(int userId, int accountId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var user = await _userRepository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));
        if (user.IsDemo)
            return Result.Fail(DomainErrors.Forbidden("The demo user cannot import files."));

        if (accountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", accountId));

        var format = ResolveFormat(fileName);
        if (format is null)
            return Result.Fail(DomainErrors.BadRequest(
                $"Unsupported file type: only {string.Join(", ", IImportJobService.AllowedExtensions)} files can be imported."));

        if (content.CanSeek && content.Length == 0)
            return Result.Fail(DomainErrors.BadRequest("The file is empty."));
        if (content.CanSeek && content.Length > IImportJobService.MaxFileSizeBytes)
            return Result.Fail(DomainErrors.BadRequest(
                $"The file exceeds the maximum size of {IImportJobService.MaxFileSizeBytes / (1024 * 1024)} MB."));

        var account = await _accountRepository.GetAccount(accountId, userId);
        if (account is null)
            return Result.Fail(DomainErrors.NotFound("Account", accountId));
        if (account.AccountTypeNavigation?.ImportSource is not { } source)
            return Result.Fail(DomainErrors.BadRequest($"Account '{account.Name}' does not support file imports."));

        var blobName = $"{userId}/{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        await _fileStorage.SaveAsync(blobName, content, cancellationToken);

        var importLog = new ImportLog
        {
            ImportedOn = DateTime.UtcNow,
            UserId = userId,
            Status = ImportStatus.Queued,
            Source = ImportLogService.Truncate(IBankFileImportService.DefaultLogSource(source, format.Value), ImportLogService.SourceMaxLength)!,
            FileName = ImportLogService.Truncate(Path.GetFileName(fileName), ImportLogService.FileNameMaxLength),
            AccountId = account.Id,
            BlobName = blobName,
        };
        await _importLogRepository.SaveImportLog(importLog);

        _logger.LogInformation(
            "Queued import {ImportLogId}: {Source} file {FileName} for account {AccountId} of user {UserId}",
            importLog.Id, importLog.Source, importLog.FileName, account.Id, userId);

        var dto = _mapper.Map<ImportLogDto>(importLog);
        dto.AccountName = account.Name;
        return Result.Ok(dto);
    }

    public async Task ProcessAsync(int importLogId, CancellationToken cancellationToken = default)
    {
        var importLog = await _importLogRepository.GetImportLog(importLogId);
        if (importLog is null)
        {
            _logger.LogWarning("Import {ImportLogId} not found: message ignored", importLogId);
            return;
        }

        if (importLog.Status is ImportStatus.Completed or ImportStatus.Failed)
        {
            _logger.LogInformation("Import {ImportLogId} already {Status}: message ignored", importLogId, importLog.Status);
            return;
        }

        importLog.Status = ImportStatus.Processing;
        importLog.StartedOn = DateTime.UtcNow;
        await _importLogRepository.UpdateImportLog(importLog);

        var statistics = new ImportStatistics();
        Result<int> result;

        try
        {
            result = await RunImportAsync(importLog, statistics, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import {ImportLogId} failed with an exception", importLogId);
            result = Result.Fail(ex.Message);
        }

        ImportLogService.ApplyOutcome(importLog, statistics, result.IsFailed ? result.Errors.Select(e => e.Message) : null);
        if (result.IsFailed && importLog.ErrorMessage is null)
        {
            importLog.Status = ImportStatus.Failed;
            importLog.ErrorMessage = "Import failed.";
        }
        importLog.CompletedOn = DateTime.UtcNow;

        // un errore qui viene rilanciato: il messaggio torna in coda e il log, ancora Processing, viene rielaborato
        await _importLogRepository.UpdateImportLog(importLog);

        _logger.LogInformation(
            "Import {ImportLogId} {Status}: {NewTransactions} new transactions, {NewTransfers} transfers, {Skipped} duplicates skipped",
            importLogId, importLog.Status, importLog.NewTransactionsCount, importLog.NewTransfersCount, importLog.SkippedDuplicatesCount);
    }

    public async Task<Result<PagedResult<ImportLogDto>>> GetImportLogs(ImportLogQueryRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Page < 0)
            return Result.Fail(DomainErrors.BadRequest("page must be >= 0"));
        if (request.PageSize <= 0 || request.PageSize > MaxPageSize)
            return Result.Fail(DomainErrors.BadRequest($"pageSize must be between 1 and {MaxPageSize}"));

        var user = await _userRepository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));
        if (user.IsDemo)
            return Result.Fail(DomainErrors.Forbidden("Imports are not available to the demo user."));

        var (items, totalCount) = await _importLogRepository.GetPagedImportLogs(request, userId);
        return Result.Ok(new PagedResult<ImportLogDto>
        {
            Items = _mapper.Map<IEnumerable<ImportLogDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        });
    }

    private async Task<Result<int>> RunImportAsync(ImportLog importLog, ImportStatistics statistics, CancellationToken cancellationToken)
    {
        if (importLog.BlobName is null || importLog.AccountId is not { } accountId)
            return Result.Fail("The import has no file or account to process.");

        var format = ResolveFormat(importLog.BlobName);
        if (format is null)
            return Result.Fail("Unsupported file type.");

        // il tipo del conto può essere cambiato dopo la messa in coda: l'import segue quello attuale
        var account = await _accountRepository.GetAccount(accountId, importLog.UserId);
        if (account?.AccountTypeNavigation?.ImportSource is not { } source)
            return Result.Fail("The account no longer exists or does not support file imports.");

        await using var content = await _fileStorage.OpenReadAsync(importLog.BlobName, cancellationToken);
        return await _bankFileImportService.ImportWithoutLogAsync(
            source, format.Value, content, importLog.FileName ?? importLog.BlobName, importLog.UserId,
            importAll: false, statistics, account.Id);
    }

    private static BankFileFormat? ResolveFormat(string? fileName) => Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
    {
        ".csv" => BankFileFormat.Csv,
        ".xlsx" => BankFileFormat.Xlsx,
        _ => null,
    };
}
