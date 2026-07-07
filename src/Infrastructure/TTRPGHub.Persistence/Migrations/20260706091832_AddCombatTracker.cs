using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "initiative",
                table: "table_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "combat_active",
                table: "game_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "combat_round",
                table: "game_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "combat_turn_token_id",
                table: "game_sessions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "initiative",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "combat_active",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "combat_round",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "combat_turn_token_id",
                table: "game_sessions");
        }
    }
}
