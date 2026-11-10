using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.OneDrive;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Security.Cryptography;

namespace RDS.ExpenseTracker.Application.Services;

public class OneDriveConnectionService : IOneDriveConnectionService
{
    private readonly IOneDriveConnectionRepository _connectionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOneDriveTokenService _tokenService;
    private readonly IOneDriveClient _oneDriveClient;
    private readonly IOneDriveImportService _importService;
    private readonly IOneDriveImportOptions _options;
    private readonly ILogger<OneDriveConnectionService> _logger;
    private readonly TimeProvider _timeProvider;

    public OneDriveConnectionService(
        IOneDriveConnectionRepository connectionRepository,
        IUserRepository userRepository,
        IOneDriveTokenService tokenService,
        IOneDriveClient oneDriveClient,
        IOneDriveImportService importService,
        IOneDriveImportOptions options,
        ILogger<OneDriveConnectionService> logger,
        TimeProvider? timeProvider = null)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _userRepository       = userRepository       ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenService         = tokenService         ?? throw new ArgumentNullException(nameof(tokenService));
        _oneDriveClient       = oneDriveClient       ?? throw new ArgumentNullException(nameof(oneDriveClient));
        _importService        = importService        ?? throw new ArgumentNullException(nameof(importService));
        _options              = options              ?? throw new ArgumentNullException(nameof(options));
        _logger               = logger               ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider         = timeProvider ?? TimeProvider.System;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    private bool Available => _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.ClientId)
        && !string.IsNullOrWhiteSpace(_options.KeyVaultUri);

    public async Task<Result<OneDriveConnectionDto>> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        var access = await EnsureAllowedAsync(userId, requireAvailable: false);
        if (access.IsFailed)
            return access;

        return ToDto(await _connectionRepository.GetByUserIdAsync(userId, cancellationToken));
    }

    public async Task<Result<OneDriveConnectionDto>> StartAsync(int userId, CancellationToken cancellationToken = default)
    {
        var access = await EnsureAllowedAsync(userId, requireAvailable: true);
        if (access.IsFailed)
            return access;

        var code = await _tokenService.StartDeviceCodeAsync(cancellationToken);

        // Un collegamento già attivo resta com'è finché il nuovo accesso non viene completato.
        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? new OneDriveConnection { UserId = userId, Status = OneDriveConnectionStatus.Pending };

        connection.DeviceCode = code.DeviceCode;
        connection.UserCode = code.UserCode;
        connection.VerificationUri = code.VerificationUri;
        connection.DeviceCodeExpiresAt = code.ExpiresAt.UtcDateTime;
        connection.PollIntervalSeconds = code.PollIntervalSeconds;
        await _connectionRepository.SaveAsync(connection, cancellationToken);

        _logger.LogInformation("OneDrive connection started for user {UserId}", userId);
        return ToDto(connection);
    }

    public async Task<Result<OneDriveConnectionDto>> CompleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var access = await EnsureAllowedAsync(userId, requireAvailable: true);
        if (access.IsFailed)
            return access;

        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (connection?.DeviceCode is null)
            return ToDto(connection);

        if (connection.DeviceCodeExpiresAt <= UtcNow)
            return await AbandonAsync(connection, "Il codice è scaduto: riprova.", cancellationToken);

        var outcome = await _tokenService.RedeemDeviceCodeAsync(userId, connection.DeviceCode, cancellationToken);
        switch (outcome)
        {
            case OneDriveDeviceCodeOutcome.Pending:
                return ToDto(connection);
            case OneDriveDeviceCodeOutcome.Declined:
                return await AbandonAsync(connection, "L'accesso a OneDrive è stato rifiutato.", cancellationToken);
            case OneDriveDeviceCodeOutcome.Expired:
                return await AbandonAsync(connection, "Il codice è scaduto: riprova.", cancellationToken);
        }

        // Nuova autorizzazione (anche di un altro account Microsoft): la subscription precedente non serve più.
        await DeleteSubscriptionAsync(connection, cancellationToken);

        string folderId;
        string? driveOwner;
        OneDriveDeltaResult baseline;
        try
        {
            folderId = await _oneDriveClient.EnsureChildFolderAsync(userId, IOneDriveClient.RootFolderId, _options.FolderName, cancellationToken);
            foreach (var bankFolder in OneDriveImportPathResolver.DefaultBankFolders)
                await _oneDriveClient.EnsureChildFolderAsync(userId, folderId, bankFolder, cancellationToken);

            // Solo i file caricati da ora in poi vengono importati.
            baseline = await _oneDriveClient.GetDeltaAsync(userId, folderId, deltaLink: null, cancellationToken);
            driveOwner = await _oneDriveClient.GetDriveOwnerAsync(userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Il codice è ormai stato riscattato: il collegamento va rifatto da capo.
            _logger.LogError(ex, "OneDrive of user {UserId} authorized but the folder setup failed", userId);
            await _connectionRepository.DeleteAsync(connection.Id, cancellationToken);
            await RemoveTokenAsync(userId, cancellationToken);
            return Result.Fail(DomainErrors.BadRequest("Non è stato possibile preparare la cartella su OneDrive: riprova."));
        }

        ClearDeviceCode(connection);
        connection.Status = OneDriveConnectionStatus.Connected;
        connection.DriveOwner = Truncate(driveOwner, 256);
        connection.FolderId = folderId;
        connection.ClientState = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        connection.DeltaLink = baseline.DeltaLink;
        connection.SubscriptionId = null;
        connection.SubscriptionExpiresAt = null;
        connection.LockedUntil = null;
        connection.ConnectedOn = UtcNow;
        connection.LastSyncOn = null;
        connection.LastError = null;
        await _connectionRepository.SaveAsync(connection, cancellationToken);

        _logger.LogInformation("OneDrive of user {UserId} connected (folder {FolderId})", userId, folderId);

        await TryCreateSubscriptionAsync(connection, cancellationToken);
        return ToDto(connection);
    }

    public async Task<Result<OneDriveConnectionDto>> DisconnectAsync(int userId, CancellationToken cancellationToken = default)
    {
        var access = await EnsureAllowedAsync(userId, requireAvailable: false);
        if (access.IsFailed)
            return access;

        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (connection is null)
            return ToDto(null);

        await DeleteSubscriptionAsync(connection, cancellationToken);
        await _connectionRepository.DeleteAsync(connection.Id, cancellationToken);
        await RemoveTokenAsync(userId, cancellationToken);

        _logger.LogInformation("OneDrive of user {UserId} disconnected", userId);
        return ToDto(null);
    }

    public async Task<Result> EnsureCanSyncAsync(int userId, CancellationToken cancellationToken = default)
    {
        var access = await EnsureAllowedAsync(userId, requireAvailable: true);
        if (access.IsFailed)
            return access;

        var connection = await _connectionRepository.GetByUserIdAsync(userId, cancellationToken);
        return connection is { Status: OneDriveConnectionStatus.Connected }
            ? Result.Ok()
            : Result.Fail(DomainErrors.Conflict("OneDrive is not connected."));
    }

    private async Task<Result> EnsureAllowedAsync(int userId, bool requireAvailable)
    {
        var user = await _userRepository.GetById(userId);
        if (user is null)
            return Result.Fail(DomainErrors.NotFound("User", userId));
        if (user.IsDemo)
            return Result.Fail(DomainErrors.Forbidden("The demo user cannot connect OneDrive."));
        if (requireAvailable && !Available)
            return Result.Fail(DomainErrors.Conflict("The OneDrive import is not available in this environment."));

        return Result.Ok();
    }

    // Accesso non completato: un collegamento già attivo resta valido, altrimenti la riga viene eliminata.
    private async Task<Result<OneDriveConnectionDto>> AbandonAsync(OneDriveConnection connection, string message, CancellationToken cancellationToken)
    {
        if (connection.Status == OneDriveConnectionStatus.Pending)
        {
            await _connectionRepository.DeleteAsync(connection.Id, cancellationToken);
        }
        else
        {
            ClearDeviceCode(connection);
            await _connectionRepository.SaveAsync(connection, cancellationToken);
        }

        return Result.Fail(DomainErrors.BadRequest(message));
    }

    private async Task RemoveTokenAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await _tokenService.RemoveAsync(userId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Il collegamento è comunque rimosso: senza riga il token non viene più usato.
            _logger.LogWarning(ex, "Could not remove the OneDrive refresh token of user {UserId}", userId);
        }
    }

    private async Task TryCreateSubscriptionAsync(OneDriveConnection connection, CancellationToken cancellationToken)
    {
        // Sviluppo locale: Graph non può raggiungere l'endpoint delle notifiche, resta la sincronizzazione manuale.
        if (string.IsNullOrWhiteSpace(_options.NotificationUrl))
            return;

        try
        {
            var result = await _importService.EnsureSubscriptionAsync(connection, cancellationToken);
            if (result.IsSuccess)
                return;

            _logger.LogWarning("OneDrive subscription of user {UserId} not created: {Errors}",
                connection.UserId, string.Join("; ", result.Errors.Select(e => e.Message)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "OneDrive subscription of user {UserId} not created", connection.UserId);
        }

        // Il collegamento resta attivo: il timer giornaliero ritenta la creazione della subscription.
        connection.LastError = OneDriveImportService.SubscriptionFailedMessage;
        await _connectionRepository.UpdateStatusAsync(connection.Id, connection.Status, connection.LastError, cancellationToken);
    }

    private async Task DeleteSubscriptionAsync(OneDriveConnection connection, CancellationToken cancellationToken)
    {
        if (connection.SubscriptionId is null)
            return;

        try
        {
            await _oneDriveClient.DeleteSubscriptionAsync(connection.UserId, connection.SubscriptionId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Non bloccante: una subscription orfana scade da sola e le sue notifiche vengono scartate.
            _logger.LogWarning(ex, "Could not delete OneDrive subscription {SubscriptionId} of user {UserId}",
                connection.SubscriptionId, connection.UserId);
        }
    }

    private static void ClearDeviceCode(OneDriveConnection connection)
    {
        connection.DeviceCode = null;
        connection.UserCode = null;
        connection.VerificationUri = null;
        connection.DeviceCodeExpiresAt = null;
        connection.PollIntervalSeconds = null;
    }

    private OneDriveConnectionDto ToDto(OneDriveConnection? connection)
    {
        var dto = new OneDriveConnectionDto
        {
            Available = Available,
            Status = connection?.Status ?? OneDriveConnectionStatus.NotConnected,
            FolderName = _options.FolderName,
            BankFolders = OneDriveImportPathResolver.DefaultBankFolders,
        };

        if (connection is null)
            return dto;

        if (connection.DeviceCode is not null && connection.DeviceCodeExpiresAt > UtcNow)
        {
            dto.UserCode = connection.UserCode;
            dto.VerificationUri = connection.VerificationUri;
            dto.CodeExpiresOn = AsUtc(connection.DeviceCodeExpiresAt);
            dto.PollIntervalSeconds = connection.PollIntervalSeconds;
        }
        else if (connection.Status == OneDriveConnectionStatus.Pending)
        {
            // Codice scaduto senza che l'utente abbia completato l'accesso.
            dto.Status = OneDriveConnectionStatus.NotConnected;
        }

        if (connection.Status != OneDriveConnectionStatus.Pending)
        {
            dto.DriveOwner = connection.DriveOwner;
            dto.ConnectedOn = AsUtc(connection.ConnectedOn);
            dto.LastSyncOn = AsUtc(connection.LastSyncOn);
            dto.AutomaticSync = connection.Status == OneDriveConnectionStatus.Connected
                && connection.SubscriptionId is not null
                && connection.SubscriptionExpiresAt > UtcNow;
            dto.LastError = connection.LastError;
        }

        return dto;
    }

    private static DateTime? AsUtc(DateTime? value)
        => value is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null;

    private static string? Truncate(string? value, int maxLength)
        => value is { Length: var length } && length > maxLength ? value[..maxLength] : value;
}
