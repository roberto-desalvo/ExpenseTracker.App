using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Scope_ExternalId_Per_User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_ExternalId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExternalId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ExternalId",
                table: "Transfers",
                column: "ExternalId",
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_ExternalId",
                table: "Transactions",
                columns: new[] { "AccountId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_ExternalId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_ExternalId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ExternalId",
                table: "Transfers",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExternalId",
                table: "Transactions",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }
    }
}
