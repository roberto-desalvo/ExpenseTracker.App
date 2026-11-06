using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_IsAdmin_And_LegacyMatchedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LegacyMatchedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            // Il primo utente (proprietario dell'app) è admin di default.
            migrationBuilder.Sql("UPDATE [Users] SET [IsAdmin] = 1 WHERE [Id] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LegacyMatchedAt",
                table: "Transactions");
        }
    }
}
