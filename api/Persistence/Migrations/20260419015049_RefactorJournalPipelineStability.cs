using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorJournalPipelineStability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Content",
                table: "JournalEntries",
                newName: "JournalText");

            migrationBuilder.CreateTable(
                name: "MatchedItemDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchedItemId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Intensity = table.Column<int>(type: "int", nullable: false),
                    MatchText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchedItemDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchedItemDetails_MatchedItems_MatchedItemId",
                        column: x => x.MatchedItemId,
                        principalTable: "MatchedItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    Anx = table.Column<int>(type: "int", nullable: false),
                    Dep = table.Column<int>(type: "int", nullable: false),
                    Str = table.Column<int>(type: "int", nullable: false),
                    Slp = table.Column<int>(type: "int", nullable: false),
                    Soc = table.Column<int>(type: "int", nullable: false),
                    Cdt = table.Column<int>(type: "int", nullable: false),
                    Safe = table.Column<int>(type: "int", nullable: false),
                    Eng = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scores_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchedItemDetails_MatchedItemId",
                table: "MatchedItemDetails",
                column: "MatchedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_JournalEntryId",
                table: "Scores",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO Scores (JournalEntryId, Anx, Dep, Str, Slp, Soc, Cdt, Safe, Eng)
                SELECT
                    ps.JournalEntryId,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'ANX' THEN ps.NewScore ELSE 0 END) AS Anx,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'DEP' THEN ps.NewScore ELSE 0 END) AS Dep,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'STR' THEN ps.NewScore ELSE 0 END) AS Str,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'SLP' THEN ps.NewScore ELSE 0 END) AS Slp,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'SOC' THEN ps.NewScore ELSE 0 END) AS Soc,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'CDT' THEN ps.NewScore ELSE 0 END) AS Cdt,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'SAFE' THEN ps.NewScore ELSE 0 END) AS Safe,
                    MAX(CASE WHEN UPPER(ps.Parameter) = 'ENG' THEN ps.NewScore ELSE 0 END) AS Eng
                FROM ParameterScores ps
                GROUP BY ps.JournalEntryId
                """);

            migrationBuilder.Sql("""
                INSERT INTO MatchedItemDetails (MatchedItemId, ItemId, Intensity, MatchText)
                SELECT Id, ItemId, Intensity, MatchText
                FROM MatchedItems
                """);

            migrationBuilder.DropTable(
                name: "ParameterScores");

            migrationBuilder.DropColumn(
                name: "Intensity",
                table: "MatchedItems");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "MatchedItems");

            migrationBuilder.DropColumn(
                name: "MatchText",
                table: "MatchedItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JournalText",
                table: "JournalEntries",
                newName: "Content");

            migrationBuilder.AddColumn<int>(
                name: "Intensity",
                table: "MatchedItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                table: "MatchedItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatchText",
                table: "MatchedItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ParameterScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalEntryId = table.Column<int>(type: "int", nullable: false),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    NewScore = table.Column<int>(type: "int", nullable: false),
                    Parameter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ParameterScores_JournalEntryId",
                table: "ParameterScores",
                column: "JournalEntryId");

            migrationBuilder.Sql("""
                INSERT INTO ParameterScores (JournalEntryId, Parameter, Delta, NewScore)
                SELECT JournalEntryId, 'ANX', 0, Anx FROM Scores
                UNION ALL SELECT JournalEntryId, 'DEP', 0, Dep FROM Scores
                UNION ALL SELECT JournalEntryId, 'STR', 0, Str FROM Scores
                UNION ALL SELECT JournalEntryId, 'SLP', 0, Slp FROM Scores
                UNION ALL SELECT JournalEntryId, 'SOC', 0, Soc FROM Scores
                UNION ALL SELECT JournalEntryId, 'CDT', 0, Cdt FROM Scores
                UNION ALL SELECT JournalEntryId, 'SAFE', 0, Safe FROM Scores
                UNION ALL SELECT JournalEntryId, 'ENG', 0, Eng FROM Scores
                """);

            migrationBuilder.Sql("""
                UPDATE mi
                SET
                    mi.ItemId = mid.ItemId,
                    mi.Intensity = mid.Intensity,
                    mi.MatchText = mid.MatchText
                FROM MatchedItems mi
                OUTER APPLY (
                    SELECT TOP(1) d.ItemId, d.Intensity, d.MatchText
                    FROM MatchedItemDetails d
                    WHERE d.MatchedItemId = mi.Id
                    ORDER BY d.Id
                ) mid
                WHERE mid.ItemId IS NOT NULL
                """);

            migrationBuilder.DropTable(
                name: "MatchedItemDetails");

            migrationBuilder.DropTable(
                name: "Scores");
        }
    }
}
