BEGIN TRANSACTION;
DROP TABLE [OneDriveSyncStates];

CREATE TABLE [OneDriveConnections] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Status] int NOT NULL,
    [DeviceCode] nvarchar(4000) NULL,
    [UserCode] nvarchar(50) NULL,
    [VerificationUri] nvarchar(500) NULL,
    [DeviceCodeExpiresAt] datetime2 NULL,
    [PollIntervalSeconds] int NULL,
    [DriveOwner] nvarchar(256) NULL,
    [FolderId] nvarchar(200) NULL,
    [ClientState] nvarchar(100) NULL,
    [DeltaLink] nvarchar(4000) NULL,
    [SubscriptionId] nvarchar(100) NULL,
    [SubscriptionExpiresAt] datetime2 NULL,
    [LockedUntil] datetime2 NULL,
    [ConnectedOn] datetime2 NULL,
    [LastSyncOn] datetime2 NULL,
    [LastError] nvarchar(1000) NULL,
    CONSTRAINT [PK_OneDriveConnections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OneDriveConnections_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_OneDriveConnections_SubscriptionId] ON [OneDriveConnections] ([SubscriptionId]);

CREATE UNIQUE INDEX [IX_OneDriveConnections_UserId] ON [OneDriveConnections] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261110201234_Add_OneDriveConnections', N'10.0.7');

COMMIT;
GO

