using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_OneDriveConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneDriveSyncStates");

            migrationBuilder.CreateTable(
                name: "OneDriveConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerificationUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceCodeExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PollIntervalSeconds = table.Column<int>(type: "int", nullable: true),
                    DriveOwner = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FolderId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClientState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeltaLink = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneDriveConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneDriveConnections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OneDriveConnections_SubscriptionId",
                table: "OneDriveConnections",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OneDriveConnections_UserId",
                table: "OneDriveConnections",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneDriveConnections");

            migrationBuilder.CreateTable(
                name: "OneDriveSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DeltaLink = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LastSyncOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
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
    }
}
