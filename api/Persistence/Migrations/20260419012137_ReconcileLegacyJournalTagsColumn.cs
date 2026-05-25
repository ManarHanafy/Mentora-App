using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileLegacyJournalTagsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('JournalEntries', 'Tags') IS NOT NULL " +
                "ALTER TABLE [JournalEntries] DROP COLUMN [Tags];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('JournalEntries', 'Tags') IS NULL " +
                "ALTER TABLE [JournalEntries] ADD [Tags] nvarchar(max) NOT NULL CONSTRAINT [DF_JournalEntries_Tags] DEFAULT N'';");
        }
    }
}
