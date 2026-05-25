using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUserConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_User_RefreshTokens",
                table: "User_RefreshTokens");

            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_User_RefreshTokens_UserId' AND object_id = OBJECT_ID('User_RefreshTokens')) DROP INDEX [IX_User_RefreshTokens_UserId] ON [User_RefreshTokens];");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User_RefreshTokens",
                table: "User_RefreshTokens",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.CreateTable(
                name: "MoodEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Mood = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoodEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoodEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MoodEntries_UserId_Date",
                table: "MoodEntries",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MoodEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User_RefreshTokens",
                table: "User_RefreshTokens");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User_RefreshTokens",
                table: "User_RefreshTokens",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_User_RefreshTokens_UserId",
                table: "User_RefreshTokens",
                column: "UserId");
        }
    }
}
