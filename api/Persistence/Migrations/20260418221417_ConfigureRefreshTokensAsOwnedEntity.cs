using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureRefreshTokensAsOwnedEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_Exercises_ExerciseId",
                table: "SuggestedExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                table: "UserParameterSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SuggestedExercises_ExerciseId",
                table: "SuggestedExercises");

            migrationBuilder.RenameColumn(
                name: "ExerciseId",
                table: "SuggestedExercises",
                newName: "UserId");

            migrationBuilder.AlterColumn<int>(
                name: "Str",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Soc",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Slp",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Safe",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Eng",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Dep",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Cdt",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Anx",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "JournalEntryId",
                table: "SuggestedExercises",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

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

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "JournalEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "User_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedExercises_UserId_ExerciseCode",
                table: "SuggestedExercises",
                columns: new[] { "UserId", "ExerciseCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_RefreshTokens_UserId",
                table: "User_RefreshTokens",
                column: "UserId");

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
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_SuggestedExercises_Users_UserId",
                table: "SuggestedExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                table: "UserParameterSnapshots");

            migrationBuilder.DropTable(
                name: "User_RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_SuggestedExercises_UserId_ExerciseCode",
                table: "SuggestedExercises");

            migrationBuilder.DropColumn(
                name: "ExerciseCode",
                table: "SuggestedExercises");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "SuggestedExercises");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "JournalEntries");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "SuggestedExercises",
                newName: "ExerciseId");

            migrationBuilder.AlterColumn<int>(
                name: "Str",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Soc",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Slp",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Safe",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Eng",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Dep",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Cdt",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Anx",
                table: "UserParameterSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "JournalEntryId",
                table: "SuggestedExercises",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                table: "SuggestedExercises",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                table: "UserParameterSnapshots",
                column: "LatestJournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id");
        }
    }
}
