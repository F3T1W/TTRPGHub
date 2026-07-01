using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddTableAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "audio_position_seconds",
                table: "game_sessions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "audio_updated_at",
                table: "game_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "current_track_title",
                table: "game_sessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "current_track_url",
                table: "game_sessions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_audio_playing",
                table: "game_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audio_position_seconds",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "audio_updated_at",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "current_track_title",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "current_track_url",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "is_audio_playing",
                table: "game_sessions");
        }
    }
}
