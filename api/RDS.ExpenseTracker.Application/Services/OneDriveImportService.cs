using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.OneDrive;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Security.Cryptography;
using System.Text;

namespace RDS.ExpenseTracker.Application.Services;

public class OneDriveImportService : IOneDriveImportService
{
    // Graph accetta al massimo 42300 minuti per le subscription driveItem: si resta un po' sotto.
    public static readonly TimeSpan SubscriptionLifetime = TimeSpan.FromMinutes(41000);
    public static readonly TimeSpan RenewalThreshold = TimeSpan.FromDays(5);
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(10);

    private readonly IOneDriveClient _oneDriveClient;
    private readonly IOneDriveSyncStateRepository _stateRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBankFileImportService _bankFileImportService;
    private readonly IOneDriveImportOptions _options;
    private readonly ILogger<OneDriveImportService> _logger;
    private readonly TimeProvider _timeProvider;

    public OneDriveImportService(
        IOneDriveClient oneDriveClient,
        IOneDriveSyncStateRepository stateRepository,
        IUserRepository userRepository,
        IBankFileImportService bankFileImportService,
        IOneDriveImportOptions options,
        ILogger<OneDriveImportService> logger,
        TimeProvider? timeProvider = null)
    {
        _oneDriveClient        = oneDriveClient        ?? throw new ArgumentNullException(nameof(oneDriveClient));
        _stateRepository       = stateRepository       ?? throw new ArgumentNullException(nameof(stateRepository));
        _userRepository        = userRepository        ?? throw new ArgumentNullException(nameof(userRepository));
        _bankFileImportService = bankFileImportService ?? throw new ArgumentNullException(nameof(bankFileImportService));
        _options               = options               ?? throw new ArgumentNullException(nameof(options));
        _logger                = logger                ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider          = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public bool IsValidClientState(string? clientState)
    {
        if (string.IsNullOrEmpty(_options.ClientState) || string.IsNullOrEmpty(clientState))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(clientState),
            Encoding.UTF8.GetBytes(_options.ClientState));
    }

    public async Task<Result<OneDriveSyncSummary>> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _stateRepository.TryAcquireLockAsync(LockDuration, cancellationToken))
            return Result.Fail(OneDriveErrors.SyncInProgress());

        try
        {
            return await SyncLockedAsync(cancellationToken);
        }
        finally
        {
            // CancellationToken.None: il lock va rilasciato anche se l'esecuzione è stata annullata.
            await _stateRepository.ReleaseLockAsync(CancellationToken.None);
        }
    }

    private async Task<Result<OneDriveSyncSummary>> SyncLockedAsync(CancellationToken cancellationToken)
    {
        var state = await _stateRepository.GetAsync(cancellationToken);

        if (state.DeltaLink is null)
        {
            await CreateBaselineAsync(cancellationToken);
            return new OneDriveSyncSummary(BaselineCreated: true, 0, 0, 0);
        }

        OneDriveDeltaResult delta;
        try
        {
            delta = await _oneDriveClient.GetDeltaAsync(_options.FolderId, state.DeltaLink, cancellationToken);
        }
        catch (OneDriveDeltaExpiredException ex)
        {
            // Graph chiede una risincronizzazione completa: si riparte da una nuova baseline. I file caricati
            // nel frattempo non vengono importati automaticamente (vanno ricaricati).
            _logger.LogWarning(ex, "OneDrive delta link expired: creating a new baseline");
            await CreateBaselineAsync(cancellationToken);
            return new OneDriveSyncSummary(BaselineCreated: true, 0, 0, 0);
        }

        var files = delta.Items.Where(i => i.IsFile && !i.IsDeleted).ToList();
        var ignored = delta.Items.Count - files.Count;
        var imported = 0;
        var failed = 0;

        if (files.Count > 0)
        {
            var user = string.IsNullOrWhiteSpace(_options.TargetUserEmail)
                ? null
                : await _userRepository.GetByEmail(_options.TargetUserEmail);

            // Senza utente il deltaLink non avanza: i file verranno elaborati alla prossima esecuzione.
            if (user is null)
                return Result.Fail(DomainErrors.NotFound($"OneDrive import target user '{_options.TargetUserEmail}'"));

            var bankFolders = await GetBankFoldersAsync(cancellationToken);
            var folderCache = new Dictionary<(string ParentId, string Name), string>();

            foreach (var file in files)
            {
                if (file.ParentId is null || !bankFolders.TryGetValue(file.ParentId, out var source))
                {
                    // Radice, Importati/Errori, cartelle non previste.
                    ignored++;
                    continue;
                }

                var succeeded = await ProcessFileAsync(file, source, user.Id, folderCache, cancellationToken);
                if (succeeded) imported++; else failed++;
            }
        }

        await _stateRepository.UpdateDeltaLinkAsync(delta.DeltaLink, UtcNow, cancellationToken);

        var summary = new OneDriveSyncSummary(BaselineCreated: false, imported, failed, ignored);
        _logger.LogInformation("OneDrive sync completed: {@Summary}", summary);
        return summary;
    }

    private async Task<bool> ProcessFileAsync(
        OneDriveItem file,
        BankFileSource source,
        int userId,
        Dictionary<(string ParentId, string Name), string> folderCache,
        CancellationToken cancellationToken)
    {
        var bankFolderId = file.ParentId!;
        var format = OneDriveImportPathResolver.ResolveFormat(source, file.Name);

        if (format is null)
        {
            _logger.LogWarning("OneDrive file {FileName} in {Source} folder has an unsupported extension: moved to {Folder}",
                file.Name, source, _options.FailedFolderName);
            await MoveToAsync(file, bankFolderId, _options.FailedFolderName, folderCache, cancellationToken);
            return false;
        }

        _logger.LogInformation("Importing OneDrive file {FileName} ({ItemId}) as {Source} {Format}", file.Name, file.Id, source, format);

        // Un errore di download (Graph) interrompe la sincronizzazione senza spostare il file:
        // il deltaLink non avanza e il messaggio in coda viene ritentato.
        await using var content = await _oneDriveClient.DownloadAsync(file.Id, cancellationToken);

        bool succeeded;
        try
        {
            var logSource = $"{IBankFileImportService.DefaultLogSource(source, format.Value)} (OneDrive)";
            var result = await _bankFileImportService.ImportAsync(source, format.Value, content, file.Name, userId, importAll: false, logSource);
            succeeded = result.IsSuccess;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Già registrato in ImportLogs da IBankFileImportService.
            _logger.LogError(ex, "Import of OneDrive file {FileName} threw an exception", file.Name);
            succeeded = false;
        }

        await MoveToAsync(file, bankFolderId,
            succeeded ? _options.ImportedFolderName : _options.FailedFolderName, folderCache, cancellationToken);
        return succeeded;
    }

    private async Task MoveToAsync(
        OneDriveItem file,
        string bankFolderId,
        string folderName,
        Dictionary<(string ParentId, string Name), string> folderCache,
        CancellationToken cancellationToken)
    {
        if (!folderCache.TryGetValue((bankFolderId, folderName), out var targetId))
        {
            targetId = await _oneDriveClient.EnsureChildFolderAsync(bankFolderId, folderName, cancellationToken);
            folderCache[(bankFolderId, folderName)] = targetId;
        }

        await _oneDriveClient.MoveAsync(file.Id, targetId, cancellationToken);
    }

    private async Task<Dictionary<string, BankFileSource>> GetBankFoldersAsync(CancellationToken cancellationToken)
    {
        var folders = await _oneDriveClient.GetChildFoldersAsync(_options.FolderId, cancellationToken);
        var result = new Dictionary<string, BankFileSource>();

        foreach (var folder in folders)
        {
            if (OneDriveImportPathResolver.ResolveBankFolder(folder.Name) is { } source)
                result[folder.Id] = source;
            else
                _logger.LogDebug("OneDrive folder {FolderName} is not a bank folder: ignored", folder.Name);
        }

        return result;
    }

    private async Task CreateBaselineAsync(CancellationToken cancellationToken)
    {
        var baseline = await _oneDriveClient.GetDeltaAsync(_options.FolderId, deltaLink: null, cancellationToken);
        await _stateRepository.UpdateDeltaLinkAsync(baseline.DeltaLink, UtcNow, cancellationToken);
        _logger.LogInformation("OneDrive delta baseline created: only files uploaded from now on will be imported");
    }

    public async Task<Result<OneDriveSubscriptionStatus>> EnsureSubscriptionAsync(string notificationUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notificationUrl))
            return Result.Fail(DomainErrors.Required(nameof(notificationUrl)));
        if (string.IsNullOrWhiteSpace(_options.ClientState))
            return Result.Fail(DomainErrors.Required("OneDriveImport:ClientState"));

        var state = await _stateRepository.GetAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();

        // La baseline va fatta prima che arrivino notifiche: altrimenti il primo file caricato
        // verrebbe "consumato" dalla baseline senza essere importato.
        if (state.DeltaLink is null)
            await CreateBaselineAsync(cancellationToken);

        if (state.SubscriptionId is not null && state.SubscriptionExpiresAt is { } currentExpiry
            && new DateTimeOffset(DateTime.SpecifyKind(currentExpiry, DateTimeKind.Utc)) > now + RenewalThreshold)
        {
            return new OneDriveSubscriptionStatus(state.SubscriptionId, DateTime.SpecifyKind(currentExpiry, DateTimeKind.Utc), "unchanged");
        }

        var expiresAt = now + SubscriptionLifetime;

        if (state.SubscriptionId is not null)
        {
            var renewed = await _oneDriveClient.RenewSubscriptionAsync(state.SubscriptionId, expiresAt, cancellationToken);
            if (renewed is not null)
            {
                await _stateRepository.UpdateSubscriptionAsync(renewed.Id, renewed.ExpiresAt.UtcDateTime, cancellationToken);
                _logger.LogInformation("OneDrive subscription {SubscriptionId} renewed until {ExpiresAt:o}", renewed.Id, renewed.ExpiresAt);
                return new OneDriveSubscriptionStatus(renewed.Id, renewed.ExpiresAt, "renewed");
            }

            _logger.LogWarning("OneDrive subscription {SubscriptionId} no longer exists: creating a new one", state.SubscriptionId);
        }

        var resource = string.IsNullOrWhiteSpace(_options.SubscriptionResource)
            ? $"/me/drive/items/{_options.FolderId}"
            : _options.SubscriptionResource;

        var created = await _oneDriveClient.CreateSubscriptionAsync(resource, notificationUrl, _options.ClientState, expiresAt, cancellationToken);
        await _stateRepository.UpdateSubscriptionAsync(created.Id, created.ExpiresAt.UtcDateTime, cancellationToken);
        _logger.LogInformation("OneDrive subscription {SubscriptionId} created on {Resource} until {ExpiresAt:o}",
            created.Id, resource, created.ExpiresAt);
        return new OneDriveSubscriptionStatus(created.Id, created.ExpiresAt, "created");
    }
}
