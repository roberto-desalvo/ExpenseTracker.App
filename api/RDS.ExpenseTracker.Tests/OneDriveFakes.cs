using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Models.DataImport;
using RDS.ExpenseTracker.Domain.Models.OneDrive;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Tests;

// Fake condivisi dai test dell'import e del collegamento OneDrive.
internal static class OneDriveFakes
{
    public const string RootId = "root";
    public const string NotificationUrl = "https://func/api/onedrive/notifications";

    public static OneDriveItem File(string id, string name, string parentId) => new(id, name, parentId, IsFile: true, IsFolder: false, IsDeleted: false);

    public static OneDriveItem Folder(string id, string name, string parentId = RootId) => new(id, name, parentId, IsFile: false, IsFolder: true, IsDeleted: false);

    internal sealed class TestOptions : IOneDriveImportOptions
    {
        public bool Enabled { get; set; } = true;
        public string ClientId => "client";
        public string FolderName => "Transazioni";
        public string NotificationUrl { get; set; } = OneDriveFakes.NotificationUrl;
        public string KeyVaultUri => "https://vault.test";
        public string RefreshTokenSecretName => "OneDriveRefreshToken";
        public string ImportedFolderName => "Importati";
        public string FailedFolderName => "Errori";
    }

    internal sealed class FakeOneDriveClient : IOneDriveClient
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
        public List<string> DeletedSubscriptions { get; } = [];
        public List<int> UserIds { get; } = [];

        /// <summary>Utenti la cui autorizzazione non è più valida.</summary>
        public HashSet<int> UnauthorizedUsers { get; } = [];
        public Exception? SubscriptionError { get; set; }
        public Exception? FolderError { get; set; }

        private void Use(int userId)
        {
            UserIds.Add(userId);
            if (UnauthorizedUsers.Contains(userId))
                throw new OneDriveAuthorizationException("revoked");
        }

        public Task<string?> GetDriveOwnerAsync(int userId, CancellationToken cancellationToken = default)
        {
            Use(userId);
            return Task.FromResult<string?>("Mario Rossi");
        }

        public Task<IReadOnlyList<OneDriveItem>> GetChildFoldersAsync(int userId, string folderId, CancellationToken cancellationToken = default)
        {
            Use(userId);
            return Task.FromResult<IReadOnlyList<OneDriveItem>>(ChildFolders.Where(f => f.ParentId == folderId).ToList());
        }

        public Task<OneDriveDeltaResult> GetDeltaAsync(int userId, string folderId, string? deltaLink, CancellationToken cancellationToken = default)
        {
            Use(userId);
            DeltaRequests.Add(deltaLink);
            if (deltaLink is not null && deltaLink == ExpireDeltaLink)
                throw new OneDriveDeltaExpiredException("expired");

            var items = deltaLink is null ? [] : DeltaItems.ToList();
            return Task.FromResult(new OneDriveDeltaResult(items, $"delta-{++_deltaCounter}"));
        }

        public Task<Stream> DownloadAsync(int userId, string itemId, CancellationToken cancellationToken = default)
        {
            Use(userId);
            return Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
        }

        public Task<string> EnsureChildFolderAsync(int userId, string parentFolderId, string name, CancellationToken cancellationToken = default)
        {
            Use(userId);
            if (FolderError is not null)
                throw FolderError;

            EnsuredFolders.Add($"{parentFolderId}/{name}");
            return Task.FromResult($"{parentFolderId}/{name}");
        }

        public Task MoveAsync(int userId, string itemId, string targetFolderId, CancellationToken cancellationToken = default)
        {
            Use(userId);
            Moves.Add((itemId, targetFolderId));
            return Task.CompletedTask;
        }

        public Task<OneDriveSubscription> CreateSubscriptionAsync(int userId, string resource, string notificationUrl, string clientState, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            Use(userId);
            if (SubscriptionError is not null)
                throw SubscriptionError;

            CreatedSubscriptions.Add((resource, notificationUrl, clientState));
            var id = $"sub-{++_subscriptionCounter}";
            ExistingSubscriptions.Add(id);
            return Task.FromResult(new OneDriveSubscription(id, expiresAt));
        }

        public Task<OneDriveSubscription?> RenewSubscriptionAsync(int userId, string subscriptionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            Use(userId);
            if (!ExistingSubscriptions.Contains(subscriptionId))
                return Task.FromResult<OneDriveSubscription?>(null);

            RenewedSubscriptions.Add(subscriptionId);
            return Task.FromResult<OneDriveSubscription?>(new OneDriveSubscription(subscriptionId, expiresAt));
        }

        public Task DeleteSubscriptionAsync(int userId, string subscriptionId, CancellationToken cancellationToken = default)
        {
            Use(userId);
            DeletedSubscriptions.Add(subscriptionId);
            ExistingSubscriptions.Remove(subscriptionId);
            return Task.CompletedTask;
        }
    }

    // Come il repository reale restituisce copie non tracciate: le modifiche passano solo dai metodi di scrittura.
    internal sealed class FakeConnectionRepository : IOneDriveConnectionRepository
    {
        private int _nextId = 1;

        public List<OneDriveConnection> Connections { get; } = [];
        public bool LockAvailable { get; set; } = true;
        public List<int> ReleasedLocks { get; } = [];

        public OneDriveConnection Add(OneDriveConnection connection)
        {
            connection.Id = _nextId++;
            Connections.Add(connection);
            return connection;
        }

        public OneDriveConnection? ForUser(int userId) => Connections.FirstOrDefault(c => c.UserId == userId);

        private OneDriveConnection ById(int id) => Connections.Single(c => c.Id == id);

        public Task<OneDriveConnection?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Copy(ForUser(userId)));

        public Task<OneDriveConnection?> GetBySubscriptionIdAsync(string subscriptionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Copy(Connections.FirstOrDefault(c => c.SubscriptionId == subscriptionId)));

        public Task<IReadOnlyList<OneDriveConnection>> GetConnectedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OneDriveConnection>>(
                [.. Connections.Where(c => c.Status == OneDriveConnectionStatus.Connected).Select(c => Copy(c)!)]);

        public Task SaveAsync(OneDriveConnection connection, CancellationToken cancellationToken = default)
        {
            if (connection.Id == 0)
            {
                connection.Id = _nextId++;
            }
            else
            {
                Connections.RemoveAll(c => c.Id == connection.Id);
            }

            Connections.Add(Copy(connection)!);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int connectionId, CancellationToken cancellationToken = default)
        {
            Connections.RemoveAll(c => c.Id == connectionId);
            return Task.CompletedTask;
        }

        public Task<bool> TryAcquireLockAsync(int connectionId, TimeSpan duration, CancellationToken cancellationToken = default)
            => Task.FromResult(LockAvailable);

        public Task ReleaseLockAsync(int connectionId, CancellationToken cancellationToken = default)
        {
            ReleasedLocks.Add(connectionId);
            return Task.CompletedTask;
        }

        public Task UpdateDeltaLinkAsync(int connectionId, string deltaLink, DateTime syncedOn, CancellationToken cancellationToken = default)
        {
            ById(connectionId).DeltaLink = deltaLink;
            ById(connectionId).LastSyncOn = syncedOn;
            return Task.CompletedTask;
        }

        public Task UpdateSubscriptionAsync(int connectionId, string subscriptionId, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            var connection = ById(connectionId);
            connection.SubscriptionId = subscriptionId;
            connection.SubscriptionExpiresAt = expiresAt;
            connection.LastError = null;
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(int connectionId, OneDriveConnectionStatus status, string? lastError, CancellationToken cancellationToken = default)
        {
            ById(connectionId).Status = status;
            ById(connectionId).LastError = lastError;
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        private static OneDriveConnection? Copy(OneDriveConnection? c) => c is null ? null : new OneDriveConnection
        {
            Id = c.Id,
            UserId = c.UserId,
            Status = c.Status,
            DeviceCode = c.DeviceCode,
            UserCode = c.UserCode,
            VerificationUri = c.VerificationUri,
            DeviceCodeExpiresAt = c.DeviceCodeExpiresAt,
            PollIntervalSeconds = c.PollIntervalSeconds,
            DriveOwner = c.DriveOwner,
            FolderId = c.FolderId,
            ClientState = c.ClientState,
            DeltaLink = c.DeltaLink,
            SubscriptionId = c.SubscriptionId,
            SubscriptionExpiresAt = c.SubscriptionExpiresAt,
            LockedUntil = c.LockedUntil,
            ConnectedOn = c.ConnectedOn,
            LastSyncOn = c.LastSyncOn,
            LastError = c.LastError,
        };
    }

    internal sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetById(int id) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
        public Task<User?> GetByEmail(string email) => throw new NotSupportedException();
        public Task<User?> GetByAzureOid(string azureOid) => throw new NotSupportedException();
        public Task<User?> GetByAppOid(string appOid) => throw new NotSupportedException();
        public Task<User> GetOrCreateUserAsync(string azureOid, string email) => throw new NotSupportedException();
        public Task AddUser(User user) => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    internal sealed class FakeBankFileImportService : IBankFileImportService
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

        public Task<Result<int>> ImportWithoutLogAsync(BankFileSource source, BankFileFormat format, Stream content, string fileName, int userId, bool importAll, ImportStatistics statistics, int? accountId = null)
            => throw new NotSupportedException();
    }

    internal sealed class FakeTokenService : IOneDriveTokenService
    {
        public OneDriveDeviceCodeOutcome Outcome { get; set; } = OneDriveDeviceCodeOutcome.Completed;
        public int StartedCodes { get; private set; }
        public List<(int UserId, string DeviceCode)> Redeemed { get; } = [];
        public List<int> Removed { get; } = [];

        public Task<OneDriveDeviceCode> StartDeviceCodeAsync(CancellationToken cancellationToken = default)
        {
            var n = ++StartedCodes;
            return Task.FromResult(new OneDriveDeviceCode(
                $"device-{n}", $"CODE{n}", "https://www.microsoft.com/link", DateTimeOffset.UtcNow.AddMinutes(15), 5));
        }

        public Task<OneDriveDeviceCodeOutcome> RedeemDeviceCodeAsync(int userId, string deviceCode, CancellationToken cancellationToken = default)
        {
            Redeemed.Add((userId, deviceCode));
            return Task.FromResult(Outcome);
        }

        public Task<string> GetAccessTokenAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult("access-token");

        public Task RemoveAsync(int userId, CancellationToken cancellationToken = default)
        {
            Removed.Add(userId);
            return Task.CompletedTask;
        }
    }
}
