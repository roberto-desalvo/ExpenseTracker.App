using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_AnalyticsLayout_To_Users : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalyticsLayout",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalyticsLayout",
                table: "Users");
        }
    }
}
