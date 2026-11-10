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

    // Cartella dell'app: l'unica parte del drive a cui il token dà accesso.
    public const string SubscriptionResource = "/me/drive/special/approot";

    public const string ReconnectMessage = "L'accesso a OneDrive è scaduto o è stato revocato: ricollega OneDrive.";
    public const string SubscriptionFailedMessage =
        "Non è stato possibile attivare l'import immediato: i file vengono importati una volta al giorno, oppure subito con \"Sincronizza ora\".";

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
            return new OneDriveSyncSummary(0, 0);
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
            return new OneDriveSyncSummary(0, 0);
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

        var user = await _userRepository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));

        var bankFolders = await GetBankFoldersAsync(userId, connection.FolderId!, allowLegacyExcel: user.IsAdmin, cancellationToken);
        var folderCache = new Dictionary<(string ParentId, string Name), string>();
        var imported = 0;
        var failed = 0;

        // Tutto ciò che si trova direttamente nella cartella di una banca è da importare: i file elaborati
        // vengono spostati in Importati/Errori. Un errore di Graph interrompe la sincronizzazione (ritentata dalla coda).
        foreach (var (bankFolderId, source) in bankFolders)
        {
            var files = (await _oneDriveClient.GetChildrenAsync(userId, bankFolderId, cancellationToken)).Where(i => i.IsFile);

            foreach (var file in files)
            {
                var succeeded = await ProcessFileAsync(file, bankFolderId, source, userId, folderCache, cancellationToken);
                if (succeeded) imported++; else failed++;
            }
        }

        await _connectionRepository.UpdateLastSyncAsync(connection.Id, UtcNow, cancellationToken);

        var summary = new OneDriveSyncSummary(imported, failed);
        _logger.LogInformation("OneDrive sync of user {UserId} completed: {@Summary}", userId, summary);
        return summary;
    }

    private async Task<bool> ProcessFileAsync(
        OneDriveItem file,
        string bankFolderId,
        BankFileSource source,
        int userId,
        Dictionary<(string ParentId, string Name), string> folderCache,
        CancellationToken cancellationToken)
    {
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
        // il messaggio in coda viene ritentato.
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
        var children = await _oneDriveClient.GetChildrenAsync(userId, folderId, cancellationToken);
        var result = new Dictionary<string, BankFileSource>();

        foreach (var folder in children.Where(i => i.IsFolder))
        {
            if (OneDriveImportPathResolver.ResolveBankFolder(folder.Name, allowLegacyExcel) is { } source)
                result[folder.Id] = source;
            else
                _logger.LogDebug("OneDrive folder {FolderName} is not a bank folder: ignored", folder.Name);
        }

        return result;
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

        var resource = SubscriptionResource;
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
        var withoutSubscription = new List<int>();

        foreach (var connection in connections)
        {
            var reconnectRequired = false;
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
                reconnectRequired = true;
                await MarkReconnectRequiredAsync(connection, ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogError(ex, "OneDrive subscription renewal of user {UserId} failed", connection.UserId);
                await _connectionRepository.UpdateStatusAsync(
                    connection.Id, OneDriveConnectionStatus.Connected, SubscriptionFailedMessage, CancellationToken.None);
            }

            var hasSubscription = connection.SubscriptionId is not null
                && connection.SubscriptionExpiresAt is { } expiry
                && DateTime.SpecifyKind(expiry, DateTimeKind.Utc) > UtcNow;
            if (!reconnectRequired && !hasSubscription)
                withoutSubscription.Add(connection.UserId);
        }

        return new OneDriveRenewalSummary(connections.Count, created, renewed, failed, withoutSubscription);
    }

    private async Task MarkReconnectRequiredAsync(OneDriveConnection connection, OneDriveAuthorizationException ex)
    {
        _logger.LogWarning(ex, "OneDrive authorization of user {UserId} is no longer valid: reconnection required", connection.UserId);
        await _connectionRepository.UpdateStatusAsync(
            connection.Id, OneDriveConnectionStatus.ReconnectRequired, ReconnectMessage, CancellationToken.None);
    }
}
