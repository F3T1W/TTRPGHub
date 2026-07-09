using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPathfinderSocietyChronicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pathfinder_society_chronicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    session_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gm_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    faction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gold_earned = table.Column<int>(type: "integer", nullable: false),
                    achievement_points = table.Column<int>(type: "integer", nullable: false),
                    boons_used = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pathfinder_society_chronicles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pathfinder_society_chronicles_character_id",
                table: "pathfinder_society_chronicles",
                column: "character_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pathfinder_society_chronicles");
        }
    }
}
