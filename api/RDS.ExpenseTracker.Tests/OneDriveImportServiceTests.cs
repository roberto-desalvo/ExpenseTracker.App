using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Services;
using static RDS.ExpenseTracker.Tests.OneDriveFakes;

namespace RDS.ExpenseTracker.Tests;

public class OneDriveImportServiceTests
{
    private const int UserId = 7;
    private const int AdminId = 8;
    private const string FolderId = "transazioni";
    private const string BbvaFolderId = "bbva";
    private const string SatispayFolderId = "satispay";
    private const string ExcelFolderId = "excel";
    private const string OtherFolderId = "other";
    private const string ClientState = "secret-state";

    private readonly FakeOneDriveClient _client = new();
    private readonly FakeConnectionRepository _connections = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeBankFileImportService _importer = new();
    private readonly TestOptions _options = new();
    private readonly OneDriveConnection _connection;

    public OneDriveImportServiceTests()
    {
        _client.ChildFolders.AddRange([
            Folder(BbvaFolderId, "BBVA", FolderId),
            Folder(SatispayFolderId, "Satispay", FolderId),
            Folder(ExcelFolderId, "Excel", FolderId),
            Folder(OtherFolderId, "Varie", FolderId),
        ]);
        _users.Users.Add(new User(UserId, "user@example.com"));
        _users.Users.Add(new User(AdminId, "admin@example.com") { IsAdmin = true });
        _connection = _connections.Add(Connected(UserId));
    }

    private static OneDriveConnection Connected(int userId, string? deltaLink = "delta-0") => new()
    {
        UserId = userId,
        Status = OneDriveConnectionStatus.Connected,
        FolderId = FolderId,
        ClientState = ClientState,
        DeltaLink = deltaLink,
    };

    private OneDriveImportService CreateService() => new(
        _client, _connections, _users, _importer, _options, NullLogger<OneDriveImportService>.Instance);

    [Fact]
    public async Task SyncAsync_CreatesBaselineWithoutImporting_WhenThereIsNoDeltaLink()
    {
        _connection.DeltaLink = null;
        _client.DeltaItems.Add(File("f1", "old.csv", BbvaFolderId));

        var result = await CreateService().SyncAsync(UserId);

        result.Value.BaselineCreated.Should().BeTrue();
        _client.DeltaRequests.Should().ContainSingle().Which.Should().BeNull("the baseline uses token=latest");
        _connection.DeltaLink.Should().Be("delta-1");
        _importer.Calls.Should().BeEmpty();
        _connections.ReleasedLocks.Should().Equal(_connection.Id);
    }

    [Fact]
    public async Task SyncAsync_ImportsNewBankFilesOfTheUser_AndMovesThemToImportedFolder()
    {
        _client.DeltaItems.AddRange([
            File("f1", "estratto.csv", BbvaFolderId),
            File("f2", "satispay.xlsx", SatispayFolderId),
        ]);

        var result = await CreateService().SyncAsync(UserId);

        result.Value.Should().Be(new OneDriveSyncSummary(false, ImportedFiles: 2, FailedFiles: 0, IgnoredItems: 0));
        _importer.Calls.Should().Equal(
            (BankFileSource.Bbva, BankFileFormat.Csv, "estratto.csv", UserId, "BBVA CSV (OneDrive)"),
            (BankFileSource.SatisPay, BankFileFormat.Xlsx, "satispay.xlsx", UserId, "Satispay XLSX (OneDrive)"));
        _client.Moves.Should().Equal(("f1", $"{BbvaFolderId}/Importati"), ("f2", $"{SatispayFolderId}/Importati"));
        _client.UserIds.Should().OnlyContain(id => id == UserId, "Graph is always called with the token of the connection owner");
        _connection.DeltaLink.Should().Be("delta-1");
        _connection.LastSyncOn.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncAsync_IgnoresTheLegacyExcelFolder_ForNonAdminUsers()
    {
        _client.DeltaItems.Add(File("f1", "Spese.xlsx", ExcelFolderId));

        var result = await CreateService().SyncAsync(UserId);

        result.Value.Should().Be(new OneDriveSyncSummary(false, 0, 0, IgnoredItems: 1));
        _importer.Calls.Should().BeEmpty();
        _client.Moves.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_ImportsTheLegacyExcelFolder_ForAdminUsers()
    {
        _connections.Add(Connected(AdminId));
        _client.DeltaItems.Add(File("f1", "Spese.xlsx", ExcelFolderId));

        var result = await CreateService().SyncAsync(AdminId);

        result.Value.ImportedFiles.Should().Be(1);
        _importer.Calls.Should().Equal((BankFileSource.Excel, BankFileFormat.Xlsx, "Spese.xlsx", AdminId, "Excel (OneDrive)"));
        _client.Moves.Should().Equal(("f1", $"{ExcelFolderId}/Importati"));
    }

    [Fact]
    public async Task SyncAsync_MovesFailedImportsToErrorFolder()
    {
        _client.DeltaItems.AddRange([File("f1", "bad.csv", BbvaFolderId), File("f2", "boom.csv", BbvaFolderId)]);
        _importer.Results["bad.csv"] = Result.Fail("Invalid header");
        _importer.Throws["boom.csv"] = new InvalidOperationException("boom");

        var result = await CreateService().SyncAsync(UserId);

        result.Value.FailedFiles.Should().Be(2);
        _client.Moves.Should().Equal(("f1", $"{BbvaFolderId}/Errori"), ("f2", $"{BbvaFolderId}/Errori"));
        _client.EnsuredFolders.Should().ContainSingle("the target folder is resolved once per sync");
    }

    [Fact]
    public async Task SyncAsync_MovesUnsupportedFilesToErrorFolder_WithoutImporting()
    {
        _client.DeltaItems.Add(File("f1", "estratto.pdf", BbvaFolderId));

        var result = await CreateService().SyncAsync(UserId);

        result.Value.FailedFiles.Should().Be(1);
        _importer.Calls.Should().BeEmpty();
        _client.Moves.Should().Equal(("f1", $"{BbvaFolderId}/Errori"));
    }

    [Fact]
    public async Task SyncAsync_IgnoresFoldersDeletedItemsAndFilesOutsideBankFolders()
    {
        _client.DeltaItems.AddRange([
            Folder(BbvaFolderId, "BBVA", FolderId),
            File("f1", "root.csv", FolderId),
            File("f2", "other.csv", OtherFolderId),
            File("f3", "already.csv", "bbva-importati"),
            File("f4", "deleted.csv", BbvaFolderId) with { IsDeleted = true },
        ]);

        var result = await CreateService().SyncAsync(UserId);

        result.Value.Should().Be(new OneDriveSyncSummary(false, 0, 0, IgnoredItems: 5));
        _importer.Calls.Should().BeEmpty();
        _client.Moves.Should().BeEmpty();
        _connection.DeltaLink.Should().Be("delta-1");
    }

    [Fact]
    public async Task SyncAsync_Fails_WhenAnotherSyncOfTheUserHoldsTheLock()
    {
        _connections.LockAvailable = false;

        var result = await CreateService().SyncAsync(UserId);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("in progress");
        _client.DeltaRequests.Should().BeEmpty();
        _connections.ReleasedLocks.Should().BeEmpty();
    }

    [Theory]
    [InlineData(OneDriveConnectionStatus.Pending)]
    [InlineData(OneDriveConnectionStatus.ReconnectRequired)]
    public async Task SyncAsync_DoesNothing_WhenTheConnectionIsNotActive(OneDriveConnectionStatus status)
    {
        _connection.Status = status;

        var result = await CreateService().SyncAsync(UserId);

        result.Value.Should().Be(new OneDriveSyncSummary(false, 0, 0, 0));
        _client.UserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_DoesNothing_WhenTheUserHasNoConnection()
    {
        var result = await CreateService().SyncAsync(AdminId);

        result.IsSuccess.Should().BeTrue();
        _client.UserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_RequiresReconnection_WhenTheAuthorizationIsNoLongerValid()
    {
        _client.UnauthorizedUsers.Add(UserId);

        var result = await CreateService().SyncAsync(UserId);

        result.IsSuccess.Should().BeTrue("the message must not be retried");
        _connection.Status.Should().Be(OneDriveConnectionStatus.ReconnectRequired);
        _connection.LastError.Should().Be(OneDriveImportService.ReconnectMessage);
        _connection.DeltaLink.Should().Be("delta-0");
        _connections.ReleasedLocks.Should().Equal(_connection.Id);
    }

    [Fact]
    public async Task SyncAsync_CreatesNewBaseline_WhenDeltaLinkExpired()
    {
        _client.ExpireDeltaLink = "delta-0";

        var result = await CreateService().SyncAsync(UserId);

        result.Value.BaselineCreated.Should().BeTrue();
        _connection.DeltaLink.Should().Be("delta-1");
    }

    [Fact]
    public async Task EnsureSubscriptionAsync_CreatesSubscriptionOnTheUserFolder_WhenMissing()
    {
        var result = await CreateService().EnsureSubscriptionAsync(Connected(UserId).WithId(_connection.Id));

        result.Value.Action.Should().Be("created");
        var created = _client.CreatedSubscriptions.Should().ContainSingle().Subject;
        created.Resource.Should().Be($"/me/drive/items/{FolderId}");
        created.NotificationUrl.Should().Be(NotificationUrl);
        created.ClientState.Should().Be(ClientState);
        _connection.SubscriptionId.Should().Be("sub-1");
    }

    [Fact]
    public async Task EnsureSubscriptionAsync_KeepsSubscription_WhenFarFromExpiry()
    {
        var connection = Connected(UserId).WithId(_connection.Id);
        connection.SubscriptionId = "sub-0";
        connection.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(20);

        var result = await CreateService().EnsureSubscriptionAsync(connection);

        result.Value.Action.Should().Be("unchanged");
        _client.CreatedSubscriptions.Should().BeEmpty();
        _client.RenewedSubscriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureSubscriptionAsync_Fails_WithoutANotificationUrl()
    {
        _options.NotificationUrl = string.Empty;

        var result = await CreateService().EnsureSubscriptionAsync(Connected(UserId).WithId(_connection.Id));

        result.IsFailed.Should().BeTrue();
        _client.CreatedSubscriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task RenewSubscriptionsAsync_HandlesEveryActiveConnection_AndKeepsGoingAfterAFailure()
    {
        // utente 7: subscription vicina alla scadenza → rinnovata
        _connection.SubscriptionId = "sub-0";
        _connection.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(2);
        _client.ExistingSubscriptions.Add("sub-0");
        // utente 8: subscription scomparsa → ricreata
        var admin = _connections.Add(Connected(AdminId));
        admin.SubscriptionId = "gone";
        admin.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1);
        // utente 9: autorizzazione revocata → da ricollegare
        var revoked = _connections.Add(Connected(9));
        _client.UnauthorizedUsers.Add(9);
        // utente 10: collegamento non completato → ignorato
        _connections.Add(new OneDriveConnection { UserId = 10, Status = OneDriveConnectionStatus.Pending });

        var summary = await CreateService().RenewSubscriptionsAsync();

        summary.Should().Be(new OneDriveRenewalSummary(Connections: 3, Created: 1, Renewed: 1, Failed: 1));
        _client.RenewedSubscriptions.Should().Equal("sub-0");
        _connection.SubscriptionExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(25));
        admin.SubscriptionId.Should().Be("sub-1");
        revoked.Status.Should().Be(OneDriveConnectionStatus.ReconnectRequired);
    }

    [Fact]
    public async Task RenewSubscriptionsAsync_RecordsTheError_WhenGraphRefusesTheSubscription()
    {
        _client.SubscriptionError = new HttpRequestException("validation failed");

        var summary = await CreateService().RenewSubscriptionsAsync();

        summary.Failed.Should().Be(1);
        _connection.Status.Should().Be(OneDriveConnectionStatus.Connected);
        _connection.LastError.Should().Be(OneDriveImportService.SubscriptionFailedMessage);
    }

    [Fact]
    public async Task ResolveNotifiedUsersAsync_ReturnsTheOwnersOfTheNotifiedSubscriptions_Once()
    {
        _connection.SubscriptionId = "sub-user";
        var admin = _connections.Add(Connected(AdminId));
        admin.SubscriptionId = "sub-admin";
        admin.ClientState = "admin-state";

        var userIds = await CreateService().ResolveNotifiedUsersAsync([
            new OneDriveNotification("sub-user", ClientState),
            new OneDriveNotification("sub-user", ClientState),
            new OneDriveNotification("sub-admin", "admin-state"),
        ]);

        userIds.Should().Equal(UserId, AdminId);
    }

    [Fact]
    public async Task ResolveNotifiedUsersAsync_DiscardsUnknownSubscriptionsAndWrongClientStates()
    {
        _connection.SubscriptionId = "sub-user";
        var admin = _connections.Add(Connected(AdminId));
        admin.SubscriptionId = "sub-admin";
        admin.ClientState = "admin-state";

        var userIds = await CreateService().ResolveNotifiedUsersAsync([
            new OneDriveNotification("sub-user", "admin-state"),
            new OneDriveNotification("sub-admin", null),
            new OneDriveNotification("sub-unknown", ClientState),
            new OneDriveNotification(null, ClientState),
        ]);

        userIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveNotifiedUsersAsync_DiscardsNotificationsOfInactiveConnections()
    {
        _connection.SubscriptionId = "sub-user";
        _connection.Status = OneDriveConnectionStatus.ReconnectRequired;

        var userIds = await CreateService().ResolveNotifiedUsersAsync([new OneDriveNotification("sub-user", ClientState)]);

        userIds.Should().BeEmpty();
    }
}

internal static class OneDriveConnectionTestExtensions
{
    public static OneDriveConnection WithId(this OneDriveConnection connection, int id)
    {
        connection.Id = id;
        return connection;
    }
}
