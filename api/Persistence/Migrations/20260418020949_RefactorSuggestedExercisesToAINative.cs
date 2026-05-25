using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSuggestedExercisesToAINative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_Exercises_ExerciseId",
                table: "SuggestedExercises");

            migrationBuilder.DropIndex(
                name: "IX_SuggestedExercises_ExerciseId",
                table: "SuggestedExercises");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "SuggestedExercises");

            migrationBuilder.AddColumn<string>(
                name: "ExerciseCode",
                table: "SuggestedExercises",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "SuggestedExercises",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExerciseCode",
                table: "SuggestedExercises");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "SuggestedExercises");

            migrationBuilder.AddColumn<int>(
                name: "ExerciseId",
                table: "SuggestedExercises",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedExercises_ExerciseId",
                table: "SuggestedExercises",
                column: "ExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedExercises_Exercises_ExerciseId",
                table: "SuggestedExercises",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
