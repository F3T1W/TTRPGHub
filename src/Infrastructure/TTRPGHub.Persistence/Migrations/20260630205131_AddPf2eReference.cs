using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPf2eReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pf2e_monsters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    traits = table.Column<string>(type: "text", nullable: false),
                    perception = table.Column<int>(type: "integer", nullable: false),
                    senses = table.Column<string>(type: "text", nullable: true),
                    languages = table.Column<string>(type: "text", nullable: true),
                    skills = table.Column<string>(type: "text", nullable: true),
                    strength = table.Column<int>(type: "integer", nullable: false),
                    dexterity = table.Column<int>(type: "integer", nullable: false),
                    constitution = table.Column<int>(type: "integer", nullable: false),
                    intelligence = table.Column<int>(type: "integer", nullable: false),
                    wisdom = table.Column<int>(type: "integer", nullable: false),
                    charisma = table.Column<int>(type: "integer", nullable: false),
                    armor_class = table.Column<int>(type: "integer", nullable: false),
                    fortitude = table.Column<int>(type: "integer", nullable: false),
                    reflex = table.Column<int>(type: "integer", nullable: false),
                    will = table.Column<int>(type: "integer", nullable: false),
                    hit_points = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<string>(type: "text", nullable: false),
                    attacks = table.Column<string>(type: "text", nullable: true),
                    abilities = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pf2e_monsters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pf2e_spells",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    traditions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    traits = table.Column<string>(type: "text", nullable: false),
                    cast = table.Column<string>(type: "text", nullable: false),
                    range = table.Column<string>(type: "text", nullable: true),
                    area = table.Column<string>(type: "text", nullable: true),
                    targets = table.Column<string>(type: "text", nullable: true),
                    duration = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    heightened = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pf2e_spells", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_monsters_level",
                table: "pf2e_monsters",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_monsters_size",
                table: "pf2e_monsters",
                column: "size");

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_monsters_slug",
                table: "pf2e_monsters",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_spells_level",
                table: "pf2e_spells",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_spells_slug",
                table: "pf2e_spells",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pf2e_spells_traditions",
                table: "pf2e_spells",
                column: "traditions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pf2e_monsters");

            migrationBuilder.DropTable(
                name: "pf2e_spells");
        }
    }
}
