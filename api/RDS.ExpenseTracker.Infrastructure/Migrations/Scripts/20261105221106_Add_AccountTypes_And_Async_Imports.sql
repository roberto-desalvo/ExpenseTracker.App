BEGIN TRANSACTION;
ALTER TABLE [ImportLogs] ADD [BlobName] nvarchar(300) NULL;

ALTER TABLE [ImportLogs] ADD [CompletedOn] datetime2 NULL;

ALTER TABLE [ImportLogs] ADD [StartedOn] datetime2 NULL;

ALTER TABLE [ImportLogs] ADD [Status] int NOT NULL DEFAULT 2;

ALTER TABLE [Accounts] ADD [AccountTypeId] int NULL;

CREATE TABLE [AccountTypes] (
    [Id] int NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [ImportSource] int NULL,
    CONSTRAINT [PK_AccountTypes] PRIMARY KEY ([Id])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ImportSource', N'Name') AND [object_id] = OBJECT_ID(N'[AccountTypes]'))
    SET IDENTITY_INSERT [AccountTypes] ON;
INSERT INTO [AccountTypes] ([Id], [ImportSource], [Name])
VALUES (1, 0, N'BBVA'),
(2, 2, N'Satispay'),
(3, 3, N'Trade Republic'),
(4, 1, N'Sella'),
(5, NULL, N'Revolut'),
(6, NULL, N'Contanti'),
(7, NULL, N'Hype'),
(8, NULL, N'PayPal');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ImportSource', N'Name') AND [object_id] = OBJECT_ID(N'[AccountTypes]'))
    SET IDENTITY_INSERT [AccountTypes] OFF;

CREATE INDEX [IX_ImportLogs_UserId_Status] ON [ImportLogs] ([UserId], [Status]);

CREATE INDEX [IX_Accounts_AccountTypeId] ON [Accounts] ([AccountTypeId]);

CREATE UNIQUE INDEX [IX_Accounts_UserId_AccountTypeId] ON [Accounts] ([UserId], [AccountTypeId]) WHERE [AccountTypeId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_AccountTypes_Name] ON [AccountTypes] ([Name]);

ALTER TABLE [Accounts] ADD CONSTRAINT [FK_Accounts_AccountTypes_AccountTypeId] FOREIGN KEY ([AccountTypeId]) REFERENCES [AccountTypes] ([Id]) ON DELETE NO ACTION;

EXEC(N'UPDATE [ImportLogs] SET [Status] = 3 WHERE [ErrorMessage] IS NOT NULL;');

EXEC(N'UPDATE [ImportLogs] SET [CompletedOn] = [ImportedOn] WHERE [CompletedOn] IS NULL;');

EXEC(N'
WITH Candidates AS (
    SELECT a.[Id], t.[Id] AS [TypeId],
           ROW_NUMBER() OVER (PARTITION BY a.[UserId], t.[Id] ORDER BY a.[Id]) AS [Rn]
    FROM [Accounts] a
    INNER JOIN [AccountTypes] t ON LOWER(LTRIM(RTRIM(a.[Name]))) = LOWER(t.[Name])
    WHERE a.[AccountTypeId] IS NULL
)
UPDATE a SET [AccountTypeId] = c.[TypeId]
FROM [Accounts] a
INNER JOIN Candidates c ON c.[Id] = a.[Id]
WHERE c.[Rn] = 1;');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261105221106_Add_AccountTypes_And_Async_Imports', N'10.0.7');

COMMIT;
GO

