BEGIN TRANSACTION;
CREATE TABLE [OneDriveSyncStates] (
    [Id] int NOT NULL,
    [DeltaLink] nvarchar(4000) NULL,
    [SubscriptionId] nvarchar(100) NULL,
    [SubscriptionExpiresAt] datetime2 NULL,
    [LockedUntil] datetime2 NULL,
    [LastSyncOn] datetime2 NULL,
    CONSTRAINT [PK_OneDriveSyncStates] PRIMARY KEY ([Id])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DeltaLink', N'LastSyncOn', N'LockedUntil', N'SubscriptionExpiresAt', N'SubscriptionId') AND [object_id] = OBJECT_ID(N'[OneDriveSyncStates]'))
    SET IDENTITY_INSERT [OneDriveSyncStates] ON;
INSERT INTO [OneDriveSyncStates] ([Id], [DeltaLink], [LastSyncOn], [LockedUntil], [SubscriptionExpiresAt], [SubscriptionId])
VALUES (1, NULL, NULL, NULL, NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DeltaLink', N'LastSyncOn', N'LockedUntil', N'SubscriptionExpiresAt', N'SubscriptionId') AND [object_id] = OBJECT_ID(N'[OneDriveSyncStates]'))
    SET IDENTITY_INSERT [OneDriveSyncStates] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261104220156_Add_OneDriveSyncState', N'10.0.7');

COMMIT;
GO

