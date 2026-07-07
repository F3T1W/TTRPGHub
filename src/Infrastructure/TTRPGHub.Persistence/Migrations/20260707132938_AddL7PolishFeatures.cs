using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddL7PolishFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_low_light_vision",
                table: "table_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "campaign_id",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_id",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visible_to_user_ids_json",
                table: "journal_entries",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_low_light_vision",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "campaign_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "visible_to_user_ids_json",
                table: "journal_entries");
        }
    }
}
