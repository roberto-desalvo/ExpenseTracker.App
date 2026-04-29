IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Accounts] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Priority] int NULL,
    [IsDefault] bit NULL,
    [Tags] nvarchar(500) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [Transfers] (
    [Id] int NOT NULL IDENTITY,
    [CreatedOn] datetime2 NOT NULL,
    CONSTRAINT [PK_Transfers] PRIMARY KEY ([Id])
);

CREATE TABLE [Transactions] (
    [Id] int NOT NULL IDENTITY,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Date] datetime2 NULL,
    [AccountId] int NOT NULL,
    [CategoryId] int NULL,
    [TransferId] int NULL,
    [CreatedOn] datetime2 NOT NULL,
    [UpdatedOn] datetime2 NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Transactions_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Transactions_Transfers_TransferId] FOREIGN KEY ([TransferId]) REFERENCES [Transfers] ([Id]) ON DELETE SET NULL
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Accounts]'))
    SET IDENTITY_INSERT [Accounts] ON;
INSERT INTO [Accounts] ([Id], [Name])
VALUES (1, N'Contanti'),
(2, N'Hype'),
(3, N'Satispay'),
(4, N'Trade Republic'),
(5, N'Sella'),
(6, N'BBVA'),
(7, N'PayPal');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Accounts]'))
    SET IDENTITY_INSERT [Accounts] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsDefault', N'Name', N'Priority', N'Tags') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([Id], [Description], [IsDefault], [Name], [Priority], [Tags])
VALUES (1, N'Other', CAST(1 AS bit), N'Default', NULL, N'default'),
(2, N'Money transfers', NULL, N'Money tranfers', NULL, NULL),
(3, N'Salary or other work incomes', NULL, N'Work incomes', NULL, NULL),
(4, N'Rent, utilities, home maintenance, etc', NULL, N'Housing', NULL, NULL),
(5, N'Health expenses, gym, sports, etc.', NULL, N'Health & Fitness', NULL, NULL),
(6, N'Food and bevarage', NULL, N'Food and bevarage', NULL, NULL),
(7, N'Transportation, car maintenance and insurance, fuel, etc.', NULL, N'Transportation', NULL, NULL),
(8, N'Entertainment', NULL, N'Entertainment', NULL, NULL),
(9, N'Clothes and accessories', NULL, N'Clothes', NULL, NULL),
(10, N'Savings and investments', NULL, N'Savings and investments', NULL, NULL),
(11, N'Gifts', NULL, N'Gifts', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsDefault', N'Name', N'Priority', N'Tags') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] OFF;

CREATE INDEX [IX_Transactions_AccountId] ON [Transactions] ([AccountId]);

CREATE INDEX [IX_Transactions_CategoryId] ON [Transactions] ([CategoryId]);

CREATE INDEX [IX_Transactions_TransferId] ON [Transactions] ([TransferId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260429154039_Initialize_Database', N'10.0.7');

COMMIT;
GO

