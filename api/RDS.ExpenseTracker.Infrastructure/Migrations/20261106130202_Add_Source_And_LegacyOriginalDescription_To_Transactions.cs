using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RDS.ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Source_And_LegacyOriginalDescription_To_Transactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LegacyOriginalDescription",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "UserCreated");

            // Movimenti esistenti: importati da file se hanno un ExternalId o sono una gamba di un trasferimento
            // importato (una sola gamba porta l'ExternalId); creati dallo storico Excel se non importati e marcati
            // come abbinati nello stesso istante della creazione (LegacyMatchService.CreateAsync).
            migrationBuilder.Sql("""
                UPDATE t SET Source = 'FileImport'
                FROM Transactions t
                WHERE t.ExternalId IS NOT NULL
                   OR EXISTS (SELECT 1 FROM Transactions o WHERE o.TransferId = t.TransferId AND o.ExternalId IS NOT NULL)
                   OR EXISTS (SELECT 1 FROM Transfers tr WHERE tr.Id = t.TransferId AND tr.ExternalId IS NOT NULL);

                UPDATE Transactions SET Source = 'LegacyImport'
                WHERE Source = 'UserCreated' AND TransferId IS NULL AND LegacyMatchedAt = CreatedOn;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacyOriginalDescription",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Transactions");
        }
    }
}
