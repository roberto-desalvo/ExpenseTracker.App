using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Models.OneDrive;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Tests;

public class OneDriveImportServiceTests
{
    private const string RootId = "root";
    private const string BbvaFolderId = "bbva";
    private const string SatispayFolderId = "satispay";
    private const string ExcelFolderId = "excel";
    private const string OtherFolderId = "other";
    private const string UserEmail = "owner@example.com";
    private const string ClientState = "secret-state";

    private readonly FakeOneDriveClient _client = new();
    private readonly FakeSyncStateRepository _state = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeBankFileImportService _importer = new();
    private readonly TestOptions _options = new();

    public OneDriveImportServiceTests()
    {
        _client.ChildFolders.AddRange([
            Folder(BbvaFolderId, "BBVA"),
            Folder(SatispayFolderId, "Satispay"),
            Folder(ExcelFolderId, "Excel"),
            Folder(OtherFolderId, "Varie"),
        ]);
        _users.Users.Add(new User(7, UserEmail));
    }

    private OneDriveImportService CreateService() => new(
        _client, _state, _users, _importer, _options, NullLogger<OneDriveImportService>.Instance);

    [Fact]
    public async Task SyncAsync_CreatesBaselineWithoutImporting_OnFirstRun()
    {
        _client.DeltaItems.Add(File("f1", "old.csv", BbvaFolderId));

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.Value.BaselineCreated.Should().BeTrue();
        _client.DeltaRequests.Should().ContainSingle().Which.Should().BeNull("the baseline uses token=latest");
        _state.State.DeltaLink.Should().Be("delta-1");
        _importer.Calls.Should().BeEmpty();
        _state.LockReleased.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAsync_ImportsNewBankFiles_AndMovesThemToImportedFolder()
    {
        _state.State.DeltaLink = "delta-0";
        _client.DeltaItems.AddRange([
            File("f1", "estratto.csv", BbvaFolderId),
            File("f2", "satispay.xlsx", SatispayFolderId),
            File("f3", "movimenti.xlsx", ExcelFolderId),
        ]);

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.Value.Should().Be(new OneDriveSyncSummary(false, ImportedFiles: 3, FailedFiles: 0, IgnoredItems: 0));
        _importer.Calls.Should().Equal(
            (BankFileSource.Bbva, BankFileFormat.Csv, "estratto.csv", 7, "BBVA CSV (OneDrive)"),
            (BankFileSource.SatisPay, BankFileFormat.Xlsx, "satispay.xlsx", 7, "Satispay XLSX (OneDrive)"),
            (BankFileSource.Excel, BankFileFormat.Xlsx, "movimenti.xlsx", 7, "Excel (OneDrive)"));
        _client.Moves.Should().Equal(
            ("f1", $"{BbvaFolderId}/Importati"),
            ("f2", $"{SatispayFolderId}/Importati"),
            ("f3", $"{ExcelFolderId}/Importati"));
        _state.State.DeltaLink.Should().Be("delta-1");
    }

    [Fact]
    public async Task SyncAsync_MovesFailedImportsToErrorFolder()
    {
        _state.State.DeltaLink = "delta-0";
        _client.DeltaItems.AddRange([File("f1", "bad.csv", BbvaFolderId), File("f2", "boom.csv", BbvaFolderId)]);
        _importer.Results["bad.csv"] = Result.Fail("Invalid header");
        _importer.Throws["boom.csv"] = new InvalidOperationException("boom");

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.Value.FailedFiles.Should().Be(2);
        _client.Moves.Should().Equal(("f1", $"{BbvaFolderId}/Errori"), ("f2", $"{BbvaFolderId}/Errori"));
        _client.EnsuredFolders.Should().ContainSingle("the target folder is resolved once per sync");
    }

    [Fact]
    public async Task SyncAsync_MovesUnsupportedFilesToErrorFolder_WithoutImporting()
    {
        _state.State.DeltaLink = "delta-0";
        _client.DeltaItems.AddRange([File("f1", "estratto.pdf", BbvaFolderId), File("f2", "movimenti.csv", ExcelFolderId)]);

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.Value.FailedFiles.Should().Be(2);
        _importer.Calls.Should().BeEmpty();
        _client.Moves.Should().Equal(("f1", $"{BbvaFolderId}/Errori"), ("f2", $"{ExcelFolderId}/Errori"));
    }

    [Fact]
    public async Task SyncAsync_IgnoresFoldersDeletedItemsAndFilesOutsideBankFolders()
    {
        _state.State.DeltaLink = "delta-0";
        _client.DeltaItems.AddRange([
            Folder(BbvaFolderId, "BBVA"),
            File("f1", "root.csv", RootId),
            File("f2", "other.csv", OtherFolderId),
            File("f3", "already.csv", "bbva-importati"),
            File("f4", "deleted.csv", BbvaFolderId) with { IsDeleted = true },
        ]);

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.Value.Should().Be(new OneDriveSyncSummary(false, 0, 0, IgnoredItems: 5));
        _importer.Calls.Should().BeEmpty();
        _client.Moves.Should().BeEmpty();
        _state.State.DeltaLink.Should().Be("delta-1");
    }

    [Fact]
    public async Task SyncAsync_Fails_WhenAnotherSyncHoldsTheLock()
    {
        _state.LockAvailable = false;

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("in progress");
        _client.DeltaRequests.Should().BeEmpty();
        _state.LockReleased.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAsync_DoesNotAdvanceDelta_WhenTargetUserIsMissing()
    {
        _state.State.DeltaLink = "delta-0";
        _users.Users.Clear();
        _client.DeltaItems.Add(File("f1", "estratto.csv", BbvaFolderId));

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        _state.State.DeltaLink.Should().Be("delta-0");
        _client.Moves.Should().BeEmpty();
        _state.LockReleased.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAsync_CreatesNewBaseline_WhenDeltaLinkExpired()
    {
        _state.State.DeltaLink = "delta-0";
        _client.ExpireDeltaLink = "delta-0";

        var result = await CreateService().SyncAsync(CancellationToken.None);

        result.Value.BaselineCreated.Should().BeTrue();
        _state.State.DeltaLink.Should().Be("delta-1");
    }

    [Fact]
    public async Task EnsureSubscriptionAsync_CreatesBaselineAndSubscription_WhenMissing()
    {
        var result = await CreateService().EnsureSubscriptionAsync("https://func/api/onedrive/notifications", CancellationToken.None);

        result.Value.Action.Should().Be("created");
        _state.State.DeltaLink.Should().Be("delta-1");
        var created = _client.CreatedSubscriptions.Should().ContainSingle().Subject;
        created.Resource.Should().Be($"/me/drive/items/{RootId}");
        created.NotificationUrl.Should().Be("https://func/api/onedrive/notifications");
        created.ClientState.Should().Be(ClientState);
        _state.State.SubscriptionId.Should().Be("sub-1");
    }

    [Fact]
    public async Task EnsureSubscriptionAsync_KeepsSubscription_WhenFarFromExpiry()
    {
        _state.State.DeltaLink = "delta-0";
        _state.State.SubscriptionId = "sub-0";
        _state.State.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(20);

        var result = await CreateService().EnsureSubscriptionAsync("https://func/api/onedrive/notifications", CancellationToken.None);

        result.Value.Action.Should().Be("unchanged");
        _client.CreatedSubscriptions.Should().BeEmpty();
        _client.RenewedSubscriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureSubscriptionAsync_RenewsSubscription_WhenCloseToExpiry()
    {
        _state.State.DeltaLink = "delta-0";
        _state.State.SubscriptionId = "sub-0";
        _state.State.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(2);
        _client.ExistingSubscriptions.Add("sub-0");

        var result = await CreateService().EnsureSubscriptionAsync("https://func/api/onedrive/notifications", CancellationToken.None);

        result.Value.Action.Should().Be("renewed");
        _client.RenewedSubscriptions.Should().Equal("sub-0");
        _state.State.SubscriptionExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(25));
    }

    [Fact]
    public async Task EnsureSubscriptionAsync_RecreatesSubscription_WhenItNoLongerExists()
    {
        _state.State.DeltaLink = "delta-0";
        _state.State.SubscriptionId = "sub-0";
        _state.State.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(-1);

        var result = await CreateService().EnsureSubscriptionAsync("https://func/api/onedrive/notifications", CancellationToken.None);

        result.Value.Action.Should().Be("created");
        _state.State.SubscriptionId.Should().Be("sub-1");
    }

    [Theory]
    [InlineData(ClientState, true)]
    [InlineData("other", false)]
    [InlineData(null, false)]
    public void IsValidClientState_ComparesWithTheConfiguredSecret(string? clientState, bool expected)
        => CreateService().IsValidClientState(clientState).Should().Be(expected);

    [Fact]
    public void IsValidClientState_RejectsEverything_WhenNoSecretIsConfigured()
    {
        _options.ClientState = string.Empty;

        CreateService().IsValidClientState(string.Empty).Should().BeFalse();
    }

    private static OneDriveItem File(string id, string name, string parentId) => new(id, name, parentId, IsFile: true, IsFolder: false, IsDeleted: false);

    private static OneDriveItem Folder(string id, string name) => new(id, name, RootId, IsFile: false, IsFolder: true, IsDeleted: false);

    private sealed class TestOptions : IOneDriveImportOptions
    {
        public bool Enabled => true;
        public string ClientId => "client";
        public string FolderId => RootId;
        public string TargetUserEmail => UserEmail;
        public string NotificationUrl => string.Empty;
        public string ClientState { get; set; } = OneDriveImportServiceTests.ClientState;
        public string SubscriptionResource => string.Empty;
        public string KeyVaultUri => string.Empty;
        public string RefreshTokenSecretName => "OneDriveRefreshToken";
        public string ImportedFolderName => "Importati";
        public string FailedFolderName => "Errori";
    }

    private sealed class FakeOneDriveClient : IOneDriveClient
    {
        private int _deltaCounter;
        private int _subscriptionCounter;

        public List<OneDriveItem> ChildFolders { get; } = [];
        public List<OneDriveItem> DeltaItems { get; } = [];
        public List<string?> DeltaRequests { get; } = [];
        public string? ExpireDeltaLink { get; set; }
        public List<(string ItemId, string Target)> Moves { get; } = [];
        public List<string> EnsuredFolders { get; } = [];
        public HashSet<string> ExistingSubscriptions { get; } = [];
        public List<(string Resource, string NotificationUrl, string ClientState)> CreatedSubscriptions { get; } = [];
        public List<string> RenewedSubscriptions { get; } = [];

        public Task<IReadOnlyList<OneDriveItem>> GetChildFoldersAsync(string folderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OneDriveItem>>(ChildFolders.Where(f => f.ParentId == folderId).ToList());

        public Task<OneDriveDeltaResult> GetDeltaAsync(string folderId, string? deltaLink, CancellationToken cancellationToken = default)
        {
            DeltaRequests.Add(deltaLink);
            if (deltaLink is not null && deltaLink == ExpireDeltaLink)
                throw new OneDriveDeltaExpiredException("expired");

            var items = deltaLink is null ? [] : DeltaItems.ToList();
            return Task.FromResult(new OneDriveDeltaResult(items, $"delta-{++_deltaCounter}"));
        }

        public Task<Stream> DownloadAsync(string itemId, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));

        public Task<string> EnsureChildFolderAsync(string parentFolderId, string name, CancellationToken cancellationToken = default)
        {
            EnsuredFolders.Add($"{parentFolderId}/{name}");
            return Task.FromResult($"{parentFolderId}/{name}");
        }

        public Task MoveAsync(string itemId, string targetFolderId, CancellationToken cancellationToken = default)
        {
            Moves.Add((itemId, targetFolderId));
            return Task.CompletedTask;
        }

        public Task<OneDriveSubscription> CreateSubscriptionAsync(string resource, string notificationUrl, string clientState, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            CreatedSubscriptions.Add((resource, notificationUrl, clientState));
            var id = $"sub-{++_subscriptionCounter}";
            ExistingSubscriptions.Add(id);
            return Task.FromResult(new OneDriveSubscription(id, expiresAt));
        }

        public Task<OneDriveSubscription?> RenewSubscriptionAsync(string subscriptionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            if (!ExistingSubscriptions.Contains(subscriptionId))
                return Task.FromResult<OneDriveSubscription?>(null);

            RenewedSubscriptions.Add(subscriptionId);
            return Task.FromResult<OneDriveSubscription?>(new OneDriveSubscription(subscriptionId, expiresAt));
        }
    }

    private sealed class FakeSyncStateRepository : IOneDriveSyncStateRepository
    {
        public OneDriveSyncState State { get; } = new() { Id = OneDriveSyncState.SingletonId };
        public bool LockAvailable { get; set; } = true;
        public bool LockReleased { get; private set; }

        public Task<OneDriveSyncState> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OneDriveSyncState
            {
                Id = State.Id,
                DeltaLink = State.DeltaLink,
                SubscriptionId = State.SubscriptionId,
                SubscriptionExpiresAt = State.SubscriptionExpiresAt,
            });

        public Task<bool> TryAcquireLockAsync(TimeSpan duration, CancellationToken cancellationToken = default)
            => Task.FromResult(LockAvailable);

        public Task ReleaseLockAsync(CancellationToken cancellationToken = default)
        {
            LockReleased = true;
            return Task.CompletedTask;
        }

        public Task UpdateDeltaLinkAsync(string deltaLink, DateTime syncedOn, CancellationToken cancellationToken = default)
        {
            State.DeltaLink = deltaLink;
            State.LastSyncOn = syncedOn;
            return Task.CompletedTask;
        }

        public Task UpdateSubscriptionAsync(string subscriptionId, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            State.SubscriptionId = subscriptionId;
            State.SubscriptionExpiresAt = expiresAt;
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByEmail(string email) => Task.FromResult(Users.FirstOrDefault(u => u.Email == email));
        public Task<User?> GetByAzureOid(string azureOid) => throw new NotSupportedException();
        public Task<User?> GetByAppOid(string appOid) => throw new NotSupportedException();
        public Task<User?> GetById(int id) => throw new NotSupportedException();
        public Task<User> GetOrCreateUserAsync(string azureOid, string email) => throw new NotSupportedException();
        public Task AddUser(User user) => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeBankFileImportService : IBankFileImportService
    {
        public List<(BankFileSource Source, BankFileFormat Format, string FileName, int UserId, string? LogSource)> Calls { get; } = [];
        public Dictionary<string, Result<int>> Results { get; } = [];
        public Dictionary<string, Exception> Throws { get; } = [];

        public Task<Result<int>> ImportAsync(BankFileSource source, BankFileFormat format, Stream content, string fileName, int userId, bool importAll = false, string? logSource = null)
        {
            Calls.Add((source, format, fileName, userId, logSource));
            if (Throws.TryGetValue(fileName, out var ex))
                throw ex;
            return Task.FromResult(Results.TryGetValue(fileName, out var result) ? result : Result.Ok(1));
        }

        public Task<Result<int>> RunTrackedAsync(string logSource, string? fileName, int userId, Func<ImportStatistics, Task<Result<int>>> import)
            => throw new NotSupportedException();
    }
}
