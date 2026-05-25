using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeJournalAiPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_Users_UserId",
                table: "SuggestedExercises");

            migrationBuilder.DropIndex(
                name: "IX_SuggestedExercises_UserId_ExerciseCode",
                table: "SuggestedExercises");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SuggestedExercises");

            migrationBuilder.Sql(
                "IF COL_LENGTH('JournalEntries', 'AiResponseJson') IS NULL " +
                "ALTER TABLE [JournalEntries] ADD [AiResponseJson] nvarchar(max) NOT NULL CONSTRAINT [DF_JournalEntries_AiResponseJson] DEFAULT N'';");

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[JournalTags]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [JournalTags] (
                        [Id] int NOT NULL IDENTITY,
                        [JournalEntryId] int NOT NULL,
                        [Tag] nvarchar(100) NOT NULL,
                        CONSTRAINT [PK_JournalTags] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_JournalTags_JournalEntries_JournalEntryId]
                            FOREIGN KEY ([JournalEntryId]) REFERENCES [JournalEntries] ([Id]) ON DELETE CASCADE
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('JournalEntries', 'Tags') IS NOT NULL
                BEGIN
                    EXEC(N'
                        INSERT INTO JournalTags (JournalEntryId, Tag)
                        SELECT je.Id, LTRIM(RTRIM(s.value))
                        FROM JournalEntries je
                        CROSS APPLY STRING_SPLIT(ISNULL(je.Tags, ''''), '','') s
                        WHERE LTRIM(RTRIM(s.value)) <> '''';
                    ');
                END
                """);

            migrationBuilder.Sql(
                "IF COL_LENGTH('JournalEntries', 'Tags') IS NOT NULL ALTER TABLE [JournalEntries] DROP COLUMN [Tags];");

            migrationBuilder.Sql("""
                DELETE FROM SuggestedExercises
                WHERE JournalEntryId IS NULL
                """);

            migrationBuilder.AlterColumn<int>(
                name: "JournalEntryId",
                table: "SuggestedExercises",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JournalTags_JournalEntryId' AND object_id = OBJECT_ID('JournalTags')) " +
                "CREATE INDEX [IX_JournalTags_JournalEntryId] ON [JournalTags]([JournalEntryId]);");

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises");

            migrationBuilder.AddColumn<string>(
                table: "JournalEntries",
                name: "Tags",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE je
                SET je.Tags = ISNULL(tags.TagList, '')
                FROM JournalEntries je
                OUTER APPLY (
                    SELECT STRING_AGG(jt.Tag, ',') AS TagList
                    FROM JournalTags jt
                    WHERE jt.JournalEntryId = je.Id
                ) tags
                """);

            migrationBuilder.DropTable(
                name: "JournalTags");

            migrationBuilder.DropColumn(
                name: "AiResponseJson",
                table: "JournalEntries");

            migrationBuilder.AlterColumn<int>(
                name: "JournalEntryId",
                table: "SuggestedExercises",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "SuggestedExercises",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE se
                SET se.UserId = je.UserId
                FROM SuggestedExercises se
                INNER JOIN JournalEntries je ON je.Id = se.JournalEntryId
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedExercises_UserId_ExerciseCode",
                table: "SuggestedExercises",
                columns: new[] { "UserId", "ExerciseCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedExercises_Users_UserId",
                table: "SuggestedExercises",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
