using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleEntrySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_systems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_official = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rule_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    content_markdown = table.Column<string>(type: "text", nullable: true),
                    stats_json = table.Column<string>(type: "jsonb", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    is_homebrew = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_systems_slug",
                table: "game_systems",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rule_entries_system_id_category",
                table: "rule_entries",
                columns: new[] { "system_id", "category" });

            migrationBuilder.CreateIndex(
                name: "IX_rule_entries_system_id_category_slug",
                table: "rule_entries",
                columns: new[] { "system_id", "category", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_systems");

            migrationBuilder.DropTable(
                name: "rule_entries");
        }
    }
}
