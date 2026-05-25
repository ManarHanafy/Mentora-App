using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FreshStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExerciseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApplicableParameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchedItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Intensity = table.Column<int>(type: "int", nullable: false),
                    MatchText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchedItems_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    NewScore = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterScores_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestedExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    ExerciseId = table.Column<int>(type: "int", nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ScoreRange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestedExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestedExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuggestedExercises_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserParameterSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LatestJournalEntryId = table.Column<int>(type: "int", nullable: true),
                    Anx = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Dep = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Str = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Slp = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Soc = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Cdt = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Safe = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Eng = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserParameterSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserParameterSnapshots_JournalEntries_LatestJournalEntryId",
                        column: x => x.LatestJournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserParameterSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Exercises",
                columns: new[] { "Id", "ApplicableParameters", "CreatedAt", "Description", "Difficulty", "DurationMinutes", "ExerciseType", "Instructions", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "ANX,DEP,CDT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Record and challenge negative thoughts using cognitive behavioural therapy.", "intermediate", 15, "CBT", "Write the automatic thought. Rate belief (0–100%). List evidence for/against. Write a balanced thought. Re-rate belief.", true, "CBT Thought Record" },
                    { 2, "ANX,STR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Calm the nervous system with a 4-4-4-4 breathing pattern.", "beginner", 5, "Breathing", "Inhale 4 counts → hold 4 → exhale 4 → hold 4. Repeat 4 cycles.", true, "Box Breathing" },
                    { 3, "SLP,ANX", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Improve sleep quality with evidence-based pre-sleep practices.", "beginner", 10, "Sleep", "No screens 1 hr before bed. Cool dark room. Fixed sleep/wake time. No caffeine after noon.", true, "Sleep Hygiene Checklist" },
                    { 4, "DEP,ENG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Overcome low mood through structured enjoyable activity scheduling.", "beginner", 20, "Behavioral", "List 5 activities you used to enjoy. Schedule one for today. Note mood before and after.", true, "Behavioural Activation" },
                    { 5, "STR,ANX,SLP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Reduce tension by systematically tensing and releasing muscle groups.", "beginner", 15, "Relaxation", "Start from feet — tense each group for 5 s then release. Work up to the face.", true, "Progressive Muscle Relaxation" },
                    { 6, "SOC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Combat isolation with a small, low-pressure social connection.", "beginner", 10, "Social", "Send one message to someone you trust. No expectation of a reply needed.", true, "Social Connection Task" },
                    { 7, "SAFE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create a personal crisis plan with coping strategies and support contacts.", "intermediate", 30, "Safety", "List warning signs → coping strategies → trusted contacts → professional services. Review with a clinician.", true, "Safety Planning" },
                    { 8, "ENG,DEP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boost engagement by recording three good things each day.", "beginner", 5, "Mindfulness", "Each evening write three things that went well today and why they happened.", true, "Gratitude Journaling" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CreatedAt",
                table: "JournalEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_UserId",
                table: "JournalEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchedItems_JournalEntryId",
                table: "MatchedItems",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterScores_JournalEntryId",
                table: "ParameterScores",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedExercises_ExerciseId",
                table: "SuggestedExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedExercises_JournalEntryId",
                table: "SuggestedExercises",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParameterSnapshots_LatestJournalEntryId",
                table: "UserParameterSnapshots",
                column: "LatestJournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParameterSnapshots_UserId",
                table: "UserParameterSnapshots",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchedItems");

            migrationBuilder.DropTable(
                name: "ParameterScores");

            migrationBuilder.DropTable(
                name: "SuggestedExercises");

            migrationBuilder.DropTable(
                name: "UserParameterSnapshots");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
