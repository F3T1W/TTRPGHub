using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPf2eVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pf2e_vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ru = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    price = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dimensions = table.Column<string>(type: "text", nullable: true),
                    crew = table.Column<string>(type: "text", nullable: true),
                    passengers = table.Column<string>(type: "text", nullable: true),
                    piloting_check = table.Column<string>(type: "text", nullable: true),
                    armor_class = table.Column<int>(type: "integer", nullable: true),
                    fortitude = table.Column<int>(type: "integer", nullable: true),
                    hardness = table.Column<int>(type: "integer", nullable: true),
                    hit_points = table.Column<int>(type: "integer", nullable: true),
                    broken_threshold = table.Column<int>(type: "integer", nullable: true),
                    immunities = table.Column<string>(type: "text", nullable: true),
                    speed = table.Column<string>(type: "text", nullable: true),
                    collision = table.Column<string>(type: "text", nullable: true),
                    abilities_text = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pf2e_vehicles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_vehicles_level",
                table: "pf2e_vehicles",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_vehicles_slug",
                table: "pf2e_vehicles",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pf2e_vehicles");
        }
    }
}
