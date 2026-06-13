using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdToTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Transfers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ExternalId",
                table: "Transfers",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_ExternalId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Transfers");
        }
    }
}
