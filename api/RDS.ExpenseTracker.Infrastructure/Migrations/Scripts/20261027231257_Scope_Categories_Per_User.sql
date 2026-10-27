BEGIN TRANSACTION;
ALTER TABLE [Categories] ADD [Kind] int NULL;

ALTER TABLE [Categories] ADD [UserId] int NULL;

EXEC(N'UPDATE [Categories] SET [Kind] = [Id] WHERE [Id] BETWEEN 1 AND 11;');

IF EXISTS (SELECT 1 FROM [Users] WHERE [Id] = 1)
    EXEC(N'UPDATE [Categories] SET [UserId] = 1;');
ELSE
    EXEC(N'DELETE FROM [Categories];');

EXEC(N'
INSERT INTO [Categories] ([Name], [Description], [Priority], [IsDefault], [Tags], [UserId], [Kind])
SELECT d.[Name], d.[Description], NULL, d.[IsDefault], d.[Tags], u.[Id], d.[Kind]
FROM [Users] u
CROSS JOIN (VALUES
    (1, N''Default'', N''Other'', N''default'', CAST(1 AS bit)),
    (2, N''Money tranfers'', N''Money transfers'', NULL, NULL),
    (3, N''Work incomes'', N''Salary or other work incomes'', NULL, NULL),
    (4, N''Housing'', N''Rent, utilities, home maintenance, etc'', NULL, NULL),
    (5, N''Health & Fitness'', N''Health expenses, gym, sports, etc.'', NULL, NULL),
    (6, N''Food and bevarage'', N''Food and bevarage'', NULL, NULL),
    (7, N''Transportation'', N''Transportation, car maintenance and insurance, fuel, etc.'', NULL, NULL),
    (8, N''Entertainment'', N''Entertainment'', NULL, NULL),
    (9, N''Clothes'', N''Clothes and accessories'', NULL, NULL),
    (10, N''Savings and investments'', N''Savings and investments'', NULL, NULL),
    (11, N''Gifts'', N''Gifts'', NULL, NULL)
) AS d([Kind], [Name], [Description], [Tags], [IsDefault])
WHERE u.[Id] <> 1
  AND NOT EXISTS (SELECT 1 FROM [Categories] c WHERE c.[UserId] = u.[Id]);
');

EXEC(N'
UPDATE t
SET t.[CategoryId] = COALESCE(own.[Id], def.[Id])
FROM [Transactions] t
JOIN [Accounts] a ON a.[Id] = t.[AccountId]
JOIN [Categories] cur ON cur.[Id] = t.[CategoryId]
LEFT JOIN [Categories] own ON own.[UserId] = a.[UserId] AND own.[Kind] = cur.[Kind]
LEFT JOIN [Categories] def ON def.[UserId] = a.[UserId] AND def.[Kind] = 1
WHERE cur.[UserId] <> a.[UserId];
');

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Categories]') AND [c].[name] = N'UserId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Categories] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Categories] ALTER COLUMN [UserId] int NOT NULL;

CREATE UNIQUE INDEX [IX_Categories_UserId_Kind] ON [Categories] ([UserId], [Kind]) WHERE [Kind] IS NOT NULL;

ALTER TABLE [Categories] ADD CONSTRAINT [FK_Categories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261027231257_Scope_Categories_Per_User', N'10.0.7');

COMMIT;
GO

