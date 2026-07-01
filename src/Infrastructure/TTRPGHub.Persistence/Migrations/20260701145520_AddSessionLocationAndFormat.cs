using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLocationAndFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "format",
                table: "game_sessions",
                type: "text",
                nullable: false,
                defaultValue: "Online");

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "game_sessions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_format",
                table: "game_sessions",
                column: "format");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_sessions_format",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "format",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "location",
                table: "game_sessions");
        }
    }
}
