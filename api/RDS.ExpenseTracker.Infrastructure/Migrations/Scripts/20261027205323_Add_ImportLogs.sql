BEGIN TRANSACTION;
CREATE TABLE [ImportLogs] (
    [Id] int NOT NULL IDENTITY,
    [ImportedOn] datetime2 NOT NULL,
    [UserId] int NOT NULL,
    [Source] nvarchar(50) NOT NULL,
    [FileName] nvarchar(260) NULL,
    [AccountId] int NULL,
    [MinTransactionDate] datetime2 NULL,
    [MaxTransactionDate] datetime2 NULL,
    [NewTransactionsCount] int NOT NULL,
    [NewTransfersCount] int NOT NULL,
    [SkippedDuplicatesCount] int NOT NULL,
    [ErrorMessage] nvarchar(4000) NULL,
    CONSTRAINT [PK_ImportLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ImportLogs_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ImportLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ImportLogs_AccountId] ON [ImportLogs] ([AccountId]);

CREATE INDEX [IX_ImportLogs_UserId_ImportedOn] ON [ImportLogs] ([UserId], [ImportedOn]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027205323_Add_ImportLogs', N'10.0.7');

COMMIT;
GO

