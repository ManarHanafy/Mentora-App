using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnboardingQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputControlType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScoringNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxAllowedSelections = table.Column<int>(type: "int", nullable: true),
                    IsSensitiveQuestion = table.Column<bool>(type: "bit", nullable: false),
                    PreQuestionDisclaimer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConditionalActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserOnboardingStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RawResponsesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardingStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardingStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OnboardingQuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScorePoints = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingQuestionOptions_OnboardingQuestions_OnboardingQuestionId",
                        column: x => x.OnboardingQuestionId,
                        principalTable: "OnboardingQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOnboardingResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserOnboardingStateId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OnboardingQuestionId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    LocaleSnapshot = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CategorySnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParameterSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuestionTextSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputControlTypeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScoringNoteSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaxAllowedSelectionsSnapshot = table.Column<int>(type: "int", nullable: true),
                    IsSensitiveQuestionSnapshot = table.Column<bool>(type: "bit", nullable: false),
                    PreQuestionDisclaimerSnapshot = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConditionalActionsSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardingResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardingResponses_OnboardingQuestions_OnboardingQuestionId",
                        column: x => x.OnboardingQuestionId,
                        principalTable: "OnboardingQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOnboardingResponses_UserOnboardingStates_UserOnboardingStateId",
                        column: x => x.UserOnboardingStateId,
                        principalTable: "UserOnboardingStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOnboardingResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserOnboardingStateId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Anx = table.Column<int>(type: "int", nullable: false),
                    Dep = table.Column<int>(type: "int", nullable: false),
                    Str = table.Column<int>(type: "int", nullable: false),
                    Slp = table.Column<int>(type: "int", nullable: false),
                    Soc = table.Column<int>(type: "int", nullable: false),
                    Cdt = table.Column<int>(type: "int", nullable: false),
                    Safe = table.Column<int>(type: "int", nullable: false),
                    Eng = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardingResults_UserOnboardingStates_UserOnboardingStateId",
                        column: x => x.UserOnboardingStateId,
                        principalTable: "UserOnboardingStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingOptionMetricModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OnboardingQuestionOptionId = table.Column<int>(type: "int", nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifierValue = table.Column<int>(type: "int", nullable: true),
                    ModifierValueText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingOptionMetricModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingOptionMetricModifiers_OnboardingQuestionOptions_OnboardingQuestionOptionId",
                        column: x => x.OnboardingQuestionOptionId,
                        principalTable: "OnboardingQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOnboardingResponseOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserOnboardingResponseId = table.Column<int>(type: "int", nullable: false),
                    OnboardingQuestionOptionId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    OptionTextSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScorePointsSnapshot = table.Column<int>(type: "int", nullable: true),
                    MetricModifiersSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOnboardingResponseOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOnboardingResponseOptions_OnboardingQuestionOptions_OnboardingQuestionOptionId",
                        column: x => x.OnboardingQuestionOptionId,
                        principalTable: "OnboardingQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOnboardingResponseOptions_UserOnboardingResponses_UserOnboardingResponseId",
                        column: x => x.UserOnboardingResponseId,
                        principalTable: "UserOnboardingResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingOptionMetricModifiers_OnboardingQuestionOptionId_Parameter",
                table: "OnboardingOptionMetricModifiers",
                columns: new[] { "OnboardingQuestionOptionId", "Parameter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuestionOptions_IsActive",
                table: "OnboardingQuestionOptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuestionOptions_OnboardingQuestionId_OptionId",
                table: "OnboardingQuestionOptions",
                columns: new[] { "OnboardingQuestionId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuestions_Locale_IsActive",
                table: "OnboardingQuestions",
                columns: new[] { "Locale", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingQuestions_QuestionId_Locale",
                table: "OnboardingQuestions",
                columns: new[] { "QuestionId", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingResponseOptions_OnboardingQuestionOptionId",
                table: "UserOnboardingResponseOptions",
                column: "OnboardingQuestionOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingResponseOptions_UserOnboardingResponseId_OnboardingQuestionOptionId",
                table: "UserOnboardingResponseOptions",
                columns: new[] { "UserOnboardingResponseId", "OnboardingQuestionOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingResponses_OnboardingQuestionId",
                table: "UserOnboardingResponses",
                column: "OnboardingQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingResponses_UserId",
                table: "UserOnboardingResponses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingResponses_UserOnboardingStateId_OnboardingQuestionId",
                table: "UserOnboardingResponses",
                columns: new[] { "UserOnboardingStateId", "OnboardingQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingResults_UserId",
                table: "UserOnboardingResults",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingResults_UserOnboardingStateId",
                table: "UserOnboardingResults",
                column: "UserOnboardingStateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingStates_UserId",
                table: "UserOnboardingStates",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingOptionMetricModifiers");

            migrationBuilder.DropTable(
                name: "UserOnboardingResponseOptions");

            migrationBuilder.DropTable(
                name: "UserOnboardingResults");

            migrationBuilder.DropTable(
                name: "OnboardingQuestionOptions");

            migrationBuilder.DropTable(
                name: "UserOnboardingResponses");

            migrationBuilder.DropTable(
                name: "OnboardingQuestions");

            migrationBuilder.DropTable(
                name: "UserOnboardingStates");
        }
    }
}
