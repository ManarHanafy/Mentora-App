using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                table: "UserParameterSnapshots");

            migrationBuilder.AddForeignKey(
                name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                table: "UserParameterSnapshots",
                column: "LatestJournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                table: "UserParameterSnapshots");

            migrationBuilder.AddForeignKey(
                name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                table: "UserParameterSnapshots",
                column: "LatestJournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
