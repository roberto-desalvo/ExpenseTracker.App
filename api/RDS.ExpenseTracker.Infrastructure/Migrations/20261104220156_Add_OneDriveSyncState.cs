using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_OneDriveSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OneDriveSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DeltaLink = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneDriveSyncStates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "OneDriveSyncStates",
                columns: new[] { "Id", "DeltaLink", "LastSyncOn", "LockedUntil", "SubscriptionExpiresAt", "SubscriptionId" },
                values: new object[] { 1, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneDriveSyncStates");
        }
    }
}
