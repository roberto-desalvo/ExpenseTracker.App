using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.OneDrive;
using static RDS.ExpenseTracker.Tests.OneDriveFakes;

namespace RDS.ExpenseTracker.Tests;

public class OneDriveConnectionServiceTests
{
    private const int UserId = 7;
    private const int DemoUserId = 8;
    private const string FolderId = "root/Transazioni";

    private readonly FakeOneDriveClient _client = new();
    private readonly FakeConnectionRepository _connections = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeTokenService _tokens = new();
    private readonly TestOptions _options = new();

    public OneDriveConnectionServiceTests()
    {
        _users.Users.Add(new User(UserId, "user@example.com"));
        _users.Users.Add(new User(DemoUserId, "demo@example.com", isDemo: true));
    }

    private OneDriveConnectionService CreateService() => new(
        _connections,
        _users,
        _tokens,
        _client,
        new OneDriveImportService(_client, _connections, _users, new FakeBankFileImportService(), _options, NullLogger<OneDriveImportService>.Instance),
        _options,
        NullLogger<OneDriveConnectionService>.Instance);

    [Fact]
    public async Task GetAsync_ReportsNotConnected_WithTheFoldersToUse()
    {
        var result = await CreateService().GetAsync(UserId);

        result.Value.Available.Should().BeTrue();
        result.Value.Status.Should().Be(OneDriveConnectionStatus.NotConnected);
        result.Value.FolderName.Should().Be("Transazioni");
        result.Value.BankFolders.Should().Equal("BBVA", "Sella", "Satispay", "Trade_Republic");
    }

    [Fact]
    public async Task GetAsync_ReportsNotAvailable_WhenTheImportIsDisabled()
    {
        _options.Enabled = false;

        (await CreateService().GetAsync(UserId)).Value.Available.Should().BeFalse();
        (await CreateService().StartAsync(UserId)).Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictResultError>();
    }

    [Fact]
    public async Task EveryOperation_IsForbidden_ForTheDemoUser()
    {
        var service = CreateService();

        (await service.GetAsync(DemoUserId)).Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        (await service.StartAsync(DemoUserId)).Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        (await service.CompleteAsync(DemoUserId)).Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        (await service.DisconnectAsync(DemoUserId)).Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        (await service.EnsureCanSyncAsync(DemoUserId)).Errors.Should().ContainSingle().Which.Should().BeOfType<ForbiddenResultError>();
        _tokens.StartedCodes.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_ReturnsTheCodeToEnter_WithoutExposingTheDeviceCode()
    {
        var result = await CreateService().StartAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.Pending);
        result.Value.UserCode.Should().Be("CODE1");
        result.Value.VerificationUri.Should().Be("https://www.microsoft.com/link");
        result.Value.PollIntervalSeconds.Should().Be(5);
        result.Value.CodeExpiresOn.Should().BeAfter(DateTime.UtcNow);
        _connections.ForUser(UserId)!.DeviceCode.Should().Be("device-1");
    }

    [Fact]
    public async Task CompleteAsync_StaysPending_UntilTheUserAuthorizesTheApp()
    {
        var service = CreateService();
        await service.StartAsync(UserId);
        _tokens.Outcome = OneDriveDeviceCodeOutcome.Pending;

        var result = await service.CompleteAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.Pending);
        result.Value.UserCode.Should().Be("CODE1");
        _tokens.Redeemed.Should().Equal((UserId, "device-1"));
        _client.UserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_CreatesFoldersBaselineAndSubscription_WhenAuthorized()
    {
        var service = CreateService();
        await service.StartAsync(UserId);

        var result = await service.CompleteAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.Connected);
        result.Value.DriveOwner.Should().Be("Mario Rossi");
        result.Value.AutomaticSync.Should().BeTrue();
        result.Value.UserCode.Should().BeNull();
        result.Value.LastError.Should().BeNull();

        _client.EnsuredFolders.Should().Equal(
            "root/Transazioni", $"{FolderId}/BBVA", $"{FolderId}/Sella", $"{FolderId}/Satispay", $"{FolderId}/Trade_Republic");

        var connection = _connections.ForUser(UserId)!;
        connection.FolderId.Should().Be(FolderId);
        connection.DeviceCode.Should().BeNull();
        connection.DeltaLink.Should().Be("delta-1");
        connection.ClientState.Should().NotBeNullOrEmpty();
        connection.SubscriptionId.Should().Be("sub-1");
        _client.CreatedSubscriptions.Should().ContainSingle()
            .Which.Should().Be(($"/me/drive/items/{FolderId}", NotificationUrl, connection.ClientState!));
    }

    [Fact]
    public async Task CompleteAsync_StaysConnectedWithoutAutomaticSync_WhenTheSubscriptionCannotBeCreated()
    {
        _client.SubscriptionError = new HttpRequestException("validation failed");
        var service = CreateService();
        await service.StartAsync(UserId);

        var result = await service.CompleteAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.Connected);
        result.Value.AutomaticSync.Should().BeFalse();
        result.Value.LastError.Should().Be(OneDriveImportService.SubscriptionFailedMessage);
        _connections.ForUser(UserId)!.LastError.Should().Be(OneDriveImportService.SubscriptionFailedMessage);
    }

    [Fact]
    public async Task CompleteAsync_DropsTheConnectionAndTheToken_WhenTheFolderSetupFails()
    {
        _client.FolderError = new HttpRequestException("quota exceeded");
        var service = CreateService();
        await service.StartAsync(UserId);

        var result = await service.CompleteAsync(UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
        _connections.Connections.Should().BeEmpty("the redeemed code cannot be used again");
        _tokens.Removed.Should().Equal(UserId);
    }

    [Fact]
    public async Task CompleteAsync_SkipsTheSubscription_WithoutANotificationUrl()
    {
        _options.NotificationUrl = string.Empty;
        var service = CreateService();
        await service.StartAsync(UserId);

        var result = await service.CompleteAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.Connected);
        result.Value.AutomaticSync.Should().BeFalse();
        result.Value.LastError.Should().BeNull();
        _client.CreatedSubscriptions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(OneDriveDeviceCodeOutcome.Declined)]
    [InlineData(OneDriveDeviceCodeOutcome.Expired)]
    public async Task CompleteAsync_DropsThePendingConnection_WhenTheSignInFails(OneDriveDeviceCodeOutcome outcome)
    {
        var service = CreateService();
        await service.StartAsync(UserId);
        _tokens.Outcome = outcome;

        var result = await service.CompleteAsync(UserId);

        result.Errors.Should().ContainSingle().Which.Should().BeOfType<BadRequestResultError>();
        _connections.ForUser(UserId).Should().BeNull();
    }

    [Fact]
    public async Task Reconnecting_KeepsTheActiveConnection_UntilTheNewSignInCompletes()
    {
        var service = CreateService();
        await service.StartAsync(UserId);
        await service.CompleteAsync(UserId);

        // nuovo accesso avviato e poi rifiutato: il collegamento precedente resta attivo
        var restarted = await service.StartAsync(UserId);
        restarted.Value.Status.Should().Be(OneDriveConnectionStatus.Connected);
        restarted.Value.UserCode.Should().Be("CODE2");
        _tokens.Outcome = OneDriveDeviceCodeOutcome.Declined;
        (await service.CompleteAsync(UserId)).IsFailed.Should().BeTrue();

        var connection = _connections.ForUser(UserId)!;
        connection.Status.Should().Be(OneDriveConnectionStatus.Connected);
        connection.SubscriptionId.Should().Be("sub-1");
        connection.DeviceCode.Should().BeNull();

        // nuovo accesso completato: la subscription viene sostituita
        await service.StartAsync(UserId);
        _tokens.Outcome = OneDriveDeviceCodeOutcome.Completed;
        var result = await service.CompleteAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.Connected);
        _client.DeletedSubscriptions.Should().Equal("sub-1");
        _connections.ForUser(UserId)!.SubscriptionId.Should().Be("sub-2");
        _connections.Connections.Should().ContainSingle();
    }

    [Fact]
    public async Task CompleteAsync_ReturnsTheCurrentState_WhenNoSignInIsInProgress()
    {
        var result = await CreateService().CompleteAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.NotConnected);
        _tokens.Redeemed.Should().BeEmpty();
    }

    [Fact]
    public async Task DisconnectAsync_RemovesSubscriptionTokenAndConnection()
    {
        var service = CreateService();
        await service.StartAsync(UserId);
        await service.CompleteAsync(UserId);

        var result = await service.DisconnectAsync(UserId);

        result.Value.Status.Should().Be(OneDriveConnectionStatus.NotConnected);
        _client.DeletedSubscriptions.Should().Equal("sub-1");
        _tokens.Removed.Should().Equal(UserId);
        _connections.Connections.Should().BeEmpty();
    }

    [Fact]
    public async Task DisconnectAsync_StillRemovesTheConnection_WhenTheAuthorizationIsNoLongerValid()
    {
        var service = CreateService();
        await service.StartAsync(UserId);
        await service.CompleteAsync(UserId);
        _client.UnauthorizedUsers.Add(UserId);

        var result = await service.DisconnectAsync(UserId);

        result.IsSuccess.Should().BeTrue();
        _connections.Connections.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureCanSyncAsync_RequiresAnActiveConnection()
    {
        var service = CreateService();
        (await service.EnsureCanSyncAsync(UserId)).Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictResultError>();

        await service.StartAsync(UserId);
        (await service.EnsureCanSyncAsync(UserId)).IsFailed.Should().BeTrue("the sign-in is not complete");

        await service.CompleteAsync(UserId);
        (await service.EnsureCanSyncAsync(UserId)).IsSuccess.Should().BeTrue();
    }
}
