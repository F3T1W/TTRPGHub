using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiativeTrackerSessionSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "linked_session_id",
                table: "initiative_trackers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "conditions_json",
                table: "initiative_entries",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "linked_token_id",
                table: "initiative_entries",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "linked_session_id",
                table: "initiative_trackers");

            migrationBuilder.DropColumn(
                name: "conditions_json",
                table: "initiative_entries");

            migrationBuilder.DropColumn(
                name: "linked_token_id",
                table: "initiative_entries");
        }
    }
}
