using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_AppOid_To_Users : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppOid",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AppOid",
                table: "Users",
                column: "AppOid",
                unique: true,
                filter: "[AppOid] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_AppOid",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AppOid",
                table: "Users");
        }
    }
}
