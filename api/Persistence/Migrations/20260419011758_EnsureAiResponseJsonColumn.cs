using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAiResponseJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('JournalEntries', 'AiResponseJson') IS NULL " +
                "ALTER TABLE [JournalEntries] ADD [AiResponseJson] nvarchar(max) NOT NULL CONSTRAINT [DF_JournalEntries_AiResponseJson] DEFAULT N'';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('JournalEntries', 'AiResponseJson') IS NOT NULL " +
                "BEGIN " +
                "IF OBJECT_ID('DF_JournalEntries_AiResponseJson', 'D') IS NOT NULL ALTER TABLE [JournalEntries] DROP CONSTRAINT [DF_JournalEntries_AiResponseJson]; " +
                "ALTER TABLE [JournalEntries] DROP COLUMN [AiResponseJson]; " +
                "END");
        }
    }
}
