using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncSuggestedExerciseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises");

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
                ;WITH Ranked AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY UserId, ExerciseCode
                            ORDER BY Id DESC
                        ) AS rn
                    FROM SuggestedExercises
                )
                DELETE FROM SuggestedExercises
                WHERE Id IN (SELECT Id FROM Ranked WHERE rn > 1)
                """);

            migrationBuilder.Sql("""
                DELETE FROM SuggestedExercises
                WHERE UserId NOT IN (SELECT Id FROM Users)
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
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<int>(
                name: "JournalEntryId",
                table: "SuggestedExercises",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
