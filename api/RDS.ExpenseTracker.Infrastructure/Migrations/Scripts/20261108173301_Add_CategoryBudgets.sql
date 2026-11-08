BEGIN TRANSACTION;
CREATE TABLE [CategoryBudgets] (
    [Id] int NOT NULL IDENTITY,
    [CategoryId] int NOT NULL,
    [Month] date NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_CategoryBudgets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CategoryBudgets_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_CategoryBudgets_CategoryId_Month] ON [CategoryBudgets] ([CategoryId], [Month]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261108173301_Add_CategoryBudgets', N'10.0.7');

COMMIT;
GO

