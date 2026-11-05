using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <summary>
    /// Tipi di conto (AccountTypes, FK nullable su Accounts) e stato degli import asincroni su ImportLogs.
    /// Le istruzioni sui dati sono in EXEC perché, nello script SQL generato, stanno nello stesso batch
    /// delle ALTER TABLE che aggiungono le colonne e altrimenti non compilerebbero.
    /// </summary>
    public partial class Add_AccountTypes_And_Async_Imports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobName",
                table: "ImportLogs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOn",
                table: "ImportLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedOn",
                table: "ImportLogs",
                type: "datetime2",
                nullable: true);

            // Gli import già registrati sono conclusi (ImportStatus.Completed = 2); quelli con errore diventano Failed più sotto.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ImportLogs",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "AccountTypeId",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImportSource = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AccountTypes",
                columns: new[] { "Id", "ImportSource", "Name" },
                values: new object[,]
                {
                    { 1, 0, "BBVA" },
                    { 2, 2, "Satispay" },
                    { 3, 3, "Trade Republic" },
                    { 4, 1, "Sella" },
                    { 5, null, "Revolut" },
                    { 6, null, "Contanti" },
                    { 7, null, "Hype" },
                    { 8, null, "PayPal" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_UserId_Status",
                table: "ImportLogs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountTypeId",
                table: "Accounts",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_AccountTypeId",
                table: "Accounts",
                columns: new[] { "UserId", "AccountTypeId" },
                unique: true,
                filter: "[AccountTypeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTypes_Name",
                table: "AccountTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AccountTypes_AccountTypeId",
                table: "Accounts",
                column: "AccountTypeId",
                principalTable: "AccountTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ImportStatus.Failed = 3
            migrationBuilder.Sql("EXEC(N'UPDATE [ImportLogs] SET [Status] = 3 WHERE [ErrorMessage] IS NOT NULL;');");
            migrationBuilder.Sql("EXEC(N'UPDATE [ImportLogs] SET [CompletedOn] = [ImportedOn] WHERE [CompletedOn] IS NULL;');");

            // Gli import ora risolvono il conto dal tipo: i conti esistenti che si chiamano come un tipo
            // (es. "BBVA", "Sella", "Satispay", "Trade Republic") ricevono quel tipo, uno solo per utente e tipo.
            migrationBuilder.Sql(@"EXEC(N'
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
WHERE c.[Rn] = 1;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AccountTypes_AccountTypeId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "AccountTypes");

            migrationBuilder.DropIndex(
                name: "IX_ImportLogs_UserId_Status",
                table: "ImportLogs");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AccountTypeId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId_AccountTypeId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "BlobName",
                table: "ImportLogs");

            migrationBuilder.DropColumn(
                name: "CompletedOn",
                table: "ImportLogs");

            migrationBuilder.DropColumn(
                name: "StartedOn",
                table: "ImportLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ImportLogs");

            migrationBuilder.DropColumn(
                name: "AccountTypeId",
                table: "Accounts");
        }
    }
}
