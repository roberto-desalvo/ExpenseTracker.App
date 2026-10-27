using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <summary>
    /// Le categorie diventano per utente (colonne UserId e Kind). Migrazione dati una tantum:
    /// <list type="number">
    /// <item>le categorie seed (Id 1..11, uguali ai valori di CategoryEnum) ricevono Kind = Id;</item>
    /// <item>tutte le categorie esistenti vengono assegnate all'utente con Id 1 (su un DB senza quell'utente,
    /// es. appena creato, le categorie seed vengono eliminate: gli utenti ricevono le loro alla creazione);</item>
    /// <item>gli altri utenti ricevono le categorie di default e i loro movimenti vengono spostati sulle proprie
    /// categorie dello stesso tipo (o sulla propria predefinita, per le categorie personalizzate dell'utente 1).</item>
    /// </list>
    /// Le istruzioni sui dati sono in EXEC perché, nello script SQL generato, stanno nello stesso batch
    /// degli ALTER TABLE che aggiungono le colonne.
    /// </summary>
    public partial class Scope_Categories_Per_User : Migration
    {
        private const int FirstUserId = 1;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("EXEC(N'UPDATE [Categories] SET [Kind] = [Id] WHERE [Id] BETWEEN 1 AND 11;');");

            migrationBuilder.Sql($"""
                IF EXISTS (SELECT 1 FROM [Users] WHERE [Id] = {FirstUserId})
                    EXEC(N'UPDATE [Categories] SET [UserId] = {FirstUserId};');
                ELSE
                    EXEC(N'DELETE FROM [Categories];');
                """);

            // snapshot delle categorie di default di CategorySeedBuilder al momento della migrazione
            migrationBuilder.Sql($"""
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
                WHERE u.[Id] <> {FirstUserId}
                  AND NOT EXISTS (SELECT 1 FROM [Categories] c WHERE c.[UserId] = u.[Id]);
                ');
                """);

            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Categories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId_Kind",
                table: "Categories",
                columns: new[] { "UserId", "Kind" },
                unique: true,
                filter: "[Kind] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_UserId",
                table: "Categories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Ripristina solo lo schema: le categorie restano tutte (anche le copie create per gli altri utenti)
        /// in un unico elenco globale, senza riassegnare i movimenti.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Users_UserId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_UserId_Kind",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Categories");
        }
    }
}
