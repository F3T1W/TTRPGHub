using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneGridAndTokenStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "armor_class",
                table: "table_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "combatant_id",
                table: "table_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "combatant_type",
                table: "table_tokens",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<int>(
                name: "current_hp",
                table: "table_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "height",
                table: "table_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "max_hp",
                table: "table_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "width",
                table: "table_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "grid_cell_size_px",
                table: "game_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "armor_class",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "combatant_id",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "combatant_type",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "current_hp",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "height",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "max_hp",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "width",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "grid_cell_size_px",
                table: "game_sessions");
        }
    }
}
