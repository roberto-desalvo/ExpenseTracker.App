using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
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

    public const string ReconnectMessage = "L'accesso a OneDrive è scaduto o è stato revocato: ricollega OneDrive.";
    public const string SubscriptionFailedMessage =
        "Non è stato possibile attivare l'import automatico: verrà ritentato. Nel frattempo puoi usare \"Sincronizza ora\".";

    private readonly IOneDriveClient _oneDriveClient;
    private readonly IOneDriveConnectionRepository _connectionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBankFileImportService _bankFileImportService;
    private readonly IOneDriveImportOptions _options;
    private readonly ILogger<OneDriveImportService> _logger;
    private readonly TimeProvider _timeProvider;

    public OneDriveImportService(
        IOneDriveClient oneDriveClient,
        IOneDriveConnectionRepository connectionRepository,
        IUserRepository userRepository,
        IBankFileImportService bankFileImportService,
        IOneDriveImportOptions options,
        ILogger<OneDriveImportService> logger,
        TimeProvider? timeProvider = null)
    {
        _oneDriveClient        = oneDriveClient        ?? throw new ArgumentNullException(nameof(oneDriveClient));
        _connectionRepository  = connectionRepository  ?? throw new ArgumentNullException(nameof(connectionRepository));
        _userRepository        = userRepository        ?? throw new ArgumentNullException(nameof(userRepository));
        _bankFileImportService = bankFileImportService ?? throw new ArgumentNullException(nameof(bankFileImportService));
        _options               = options               ?? throw new ArgumentNullException(nameof(options));
        _logger                = logger                ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider          = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<int>> ResolveNotifiedUsersAsync(
        IEnumerable<OneDriveNotification> notifications, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var userIds = new List<int>();

        foreach (var group in notifications.Where(n => !string.IsNullOrEmpty(n.SubscriptionId)).GroupBy(n => n.SubscriptionId!))
        {
            var connection = await _connectionRepository.GetBySubscriptionIdAsync(group.Key, cancellationToken);

            if (connection is not { Status: OneDriveConnectionStatus.Connected }
                || !group.Any(n => IsValidClientState(connection, n.ClientState)))
            {
                _logger.LogWarning("Discarded OneDrive notification for unknown subscription {SubscriptionId} or with an invalid clientState", group.Key);
                continue;
            }

            if (!userIds.Contains(connection.UserId))
                userIds.Add(connection.UserId);
        }

        return userIds;
    }

    private static bool IsValidClientState(OneDriveConnection connection, string? clientState)
    {
        if (string.IsNullOrEmpty(connection.ClientState) || string.IsNullOrEmpty(clientState))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(clientState),
            Encoding.UTF8.GetBytes(connection.ClientState));
    }

    public async Task<Result<OneDriveSyncSummary>> SyncAsync(int userId, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (connection is not { Status: OneDriveConnectionStatus.Connected, FolderId: not null })
        {
            // Collegamento rimosso (o da rifare) dopo l'accodamento della sincronizzazione.
            _logger.LogInformation("OneDrive sync skipped: user {UserId} has no active connection", userId);
            return new OneDriveSyncSummary(BaselineCreated: false, 0, 0, 0);
        }

        if (!await _connectionRepository.TryAcquireLockAsync(connection.Id, LockDuration, cancellationToken))
            return Result.Fail(OneDriveErrors.SyncInProgress());

        try
        {
            return await SyncLockedAsync(connection, cancellationToken);
        }
        catch (OneDriveAuthorizationException ex)
        {
            await MarkReconnectRequiredAsync(connection, ex);
            return new OneDriveSyncSummary(BaselineCreated: false, 0, 0, 0);
        }
        finally
        {
            // CancellationToken.None: il lock va rilasciato anche se l'esecuzione è stata annullata.
            await _connectionRepository.ReleaseLockAsync(connection.Id, CancellationToken.None);
        }
    }

    private async Task<Result<OneDriveSyncSummary>> SyncLockedAsync(OneDriveConnection connection, CancellationToken cancellationToken)
    {
        var userId = connection.UserId;
        var folderId = connection.FolderId!;

        if (connection.DeltaLink is null)
        {
            await CreateBaselineAsync(connection, cancellationToken);
            return new OneDriveSyncSummary(BaselineCreated: true, 0, 0, 0);
        }

        OneDriveDeltaResult delta;
        try
        {
            delta = await _oneDriveClient.GetDeltaAsync(userId, folderId, connection.DeltaLink, cancellationToken);
        }
        catch (OneDriveDeltaExpiredException ex)
        {
            // Graph chiede una risincronizzazione completa: si riparte da una nuova baseline. I file caricati
            // nel frattempo non vengono importati automaticamente (vanno ricaricati).
            _logger.LogWarning(ex, "OneDrive delta link of user {UserId} expired: creating a new baseline", userId);
            await CreateBaselineAsync(connection, cancellationToken);
            return new OneDriveSyncSummary(BaselineCreated: true, 0, 0, 0);
        }

        var files = delta.Items.Where(i => i.IsFile && !i.IsDeleted).ToList();
        var ignored = delta.Items.Count - files.Count;
        var imported = 0;
        var failed = 0;

        if (files.Count > 0)
        {
            var user = await _userRepository.GetById(userId);

            // Senza utente il deltaLink non avanza: i file verranno elaborati alla prossima esecuzione.
            if (user is null)
                return Result.Fail(DomainErrors.NotFound("User", userId));

            var bankFolders = await GetBankFoldersAsync(userId, folderId, allowLegacyExcel: user.IsAdmin, cancellationToken);
            var folderCache = new Dictionary<(string ParentId, string Name), string>();

            foreach (var file in files)
            {
                if (file.ParentId is null || !bankFolders.TryGetValue(file.ParentId, out var source))
                {
                    // Radice, Importati/Errori, cartelle non previste.
                    ignored++;
                    continue;
                }

                var succeeded = await ProcessFileAsync(file, source, userId, folderCache, cancellationToken);
                if (succeeded) imported++; else failed++;
            }
        }

        await _connectionRepository.UpdateDeltaLinkAsync(connection.Id, delta.DeltaLink, UtcNow, cancellationToken);

        var summary = new OneDriveSyncSummary(BaselineCreated: false, imported, failed, ignored);
        _logger.LogInformation("OneDrive sync of user {UserId} completed: {@Summary}", userId, summary);
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
            await MoveToAsync(userId, file, bankFolderId, _options.FailedFolderName, folderCache, cancellationToken);
            return false;
        }

        _logger.LogInformation("Importing OneDrive file {FileName} ({ItemId}) of user {UserId} as {Source} {Format}",
            file.Name, file.Id, userId, source, format);

        // Un errore di download (Graph) interrompe la sincronizzazione senza spostare il file:
        // il deltaLink non avanza e il messaggio in coda viene ritentato.
        await using var content = await _oneDriveClient.DownloadAsync(userId, file.Id, cancellationToken);

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

        await MoveToAsync(userId, file, bankFolderId,
            succeeded ? _options.ImportedFolderName : _options.FailedFolderName, folderCache, cancellationToken);
        return succeeded;
    }

    private async Task MoveToAsync(
        int userId,
        OneDriveItem file,
        string bankFolderId,
        string folderName,
        Dictionary<(string ParentId, string Name), string> folderCache,
        CancellationToken cancellationToken)
    {
        if (!folderCache.TryGetValue((bankFolderId, folderName), out var targetId))
        {
            targetId = await _oneDriveClient.EnsureChildFolderAsync(userId, bankFolderId, folderName, cancellationToken);
            folderCache[(bankFolderId, folderName)] = targetId;
        }

        await _oneDriveClient.MoveAsync(userId, file.Id, targetId, cancellationToken);
    }

    private async Task<Dictionary<string, BankFileSource>> GetBankFoldersAsync(
        int userId, string folderId, bool allowLegacyExcel, CancellationToken cancellationToken)
    {
        var folders = await _oneDriveClient.GetChildFoldersAsync(userId, folderId, cancellationToken);
        var result = new Dictionary<string, BankFileSource>();

        foreach (var folder in folders)
        {
            if (OneDriveImportPathResolver.ResolveBankFolder(folder.Name, allowLegacyExcel) is { } source)
                result[folder.Id] = source;
            else
                _logger.LogDebug("OneDrive folder {FolderName} is not a bank folder: ignored", folder.Name);
        }

        return result;
    }

    private async Task CreateBaselineAsync(OneDriveConnection connection, CancellationToken cancellationToken)
    {
        var baseline = await _oneDriveClient.GetDeltaAsync(connection.UserId, connection.FolderId!, deltaLink: null, cancellationToken);
        await _connectionRepository.UpdateDeltaLinkAsync(connection.Id, baseline.DeltaLink, UtcNow, cancellationToken);
        connection.DeltaLink = baseline.DeltaLink;
        _logger.LogInformation("OneDrive delta baseline of user {UserId} created: only files uploaded from now on will be imported",
            connection.UserId);
    }

    public async Task<Result<OneDriveSubscriptionStatus>> EnsureSubscriptionAsync(OneDriveConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(_options.NotificationUrl))
            return Result.Fail(DomainErrors.Required("OneDriveImport:NotificationUrl"));
        if (string.IsNullOrWhiteSpace(connection.FolderId) || string.IsNullOrWhiteSpace(connection.ClientState))
            return Result.Fail(DomainErrors.BadRequest("The OneDrive connection is not complete."));

        var userId = connection.UserId;
        var now = _timeProvider.GetUtcNow();

        // La baseline va fatta prima che arrivino notifiche: altrimenti il primo file caricato
        // verrebbe "consumato" dalla baseline senza essere importato.
        if (connection.DeltaLink is null)
            await CreateBaselineAsync(connection, cancellationToken);

        if (connection.SubscriptionId is not null && connection.SubscriptionExpiresAt is { } currentExpiry
            && new DateTimeOffset(DateTime.SpecifyKind(currentExpiry, DateTimeKind.Utc)) > now + RenewalThreshold)
        {
            return new OneDriveSubscriptionStatus(connection.SubscriptionId, DateTime.SpecifyKind(currentExpiry, DateTimeKind.Utc), "unchanged");
        }

        var expiresAt = now + SubscriptionLifetime;

        if (connection.SubscriptionId is not null)
        {
            var renewed = await _oneDriveClient.RenewSubscriptionAsync(userId, connection.SubscriptionId, expiresAt, cancellationToken);
            if (renewed is not null)
            {
                await SaveSubscriptionAsync(connection, renewed, cancellationToken);
                _logger.LogInformation("OneDrive subscription {SubscriptionId} of user {UserId} renewed until {ExpiresAt:o}",
                    renewed.Id, userId, renewed.ExpiresAt);
                return new OneDriveSubscriptionStatus(renewed.Id, renewed.ExpiresAt, "renewed");
            }

            _logger.LogWarning("OneDrive subscription {SubscriptionId} no longer exists: creating a new one", connection.SubscriptionId);
        }

        var resource = $"/me/drive/items/{connection.FolderId}";
        var created = await _oneDriveClient.CreateSubscriptionAsync(
            userId, resource, _options.NotificationUrl, connection.ClientState, expiresAt, cancellationToken);
        await SaveSubscriptionAsync(connection, created, cancellationToken);
        _logger.LogInformation("OneDrive subscription {SubscriptionId} of user {UserId} created on {Resource} until {ExpiresAt:o}",
            created.Id, userId, resource, created.ExpiresAt);
        return new OneDriveSubscriptionStatus(created.Id, created.ExpiresAt, "created");
    }

    private async Task SaveSubscriptionAsync(OneDriveConnection connection, OneDriveSubscription subscription, CancellationToken cancellationToken)
    {
        await _connectionRepository.UpdateSubscriptionAsync(connection.Id, subscription.Id, subscription.ExpiresAt.UtcDateTime, cancellationToken);
        connection.SubscriptionId = subscription.Id;
        connection.SubscriptionExpiresAt = subscription.ExpiresAt.UtcDateTime;
        connection.LastError = null;
    }

    public async Task<OneDriveRenewalSummary> RenewSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var connections = await _connectionRepository.GetConnectedAsync(cancellationToken);
        int created = 0, renewed = 0, failed = 0;

        foreach (var connection in connections)
        {
            try
            {
                var result = await EnsureSubscriptionAsync(connection, cancellationToken);
                if (result.IsFailed)
                {
                    failed++;
                    _logger.LogWarning("OneDrive subscription of user {UserId} not renewed: {Errors}",
                        connection.UserId, string.Join("; ", result.Errors.Select(e => e.Message)));
                }
                else if (result.Value.Action == "created") created++;
                else if (result.Value.Action == "renewed") renewed++;
            }
            catch (OneDriveAuthorizationException ex)
            {
                failed++;
                await MarkReconnectRequiredAsync(connection, ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogError(ex, "OneDrive subscription renewal of user {UserId} failed", connection.UserId);
                await _connectionRepository.UpdateStatusAsync(
                    connection.Id, OneDriveConnectionStatus.Connected, SubscriptionFailedMessage, CancellationToken.None);
            }
        }

        return new OneDriveRenewalSummary(connections.Count, created, renewed, failed);
    }

    private async Task MarkReconnectRequiredAsync(OneDriveConnection connection, OneDriveAuthorizationException ex)
    {
        _logger.LogWarning(ex, "OneDrive authorization of user {UserId} is no longer valid: reconnection required", connection.UserId);
        await _connectionRepository.UpdateStatusAsync(
            connection.Id, OneDriveConnectionStatus.ReconnectRequired, ReconnectMessage, CancellationToken.None);
    }
}
