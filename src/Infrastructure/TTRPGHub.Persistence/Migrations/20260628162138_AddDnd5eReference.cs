using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddDnd5eReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dnd5e_monsters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subtype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    alignment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    armor_class = table.Column<int>(type: "integer", nullable: false),
                    armor_desc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hit_points = table.Column<int>(type: "integer", nullable: false),
                    hit_dice = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    speed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    strength = table.Column<int>(type: "integer", nullable: false),
                    dexterity = table.Column<int>(type: "integer", nullable: false),
                    constitution = table.Column<int>(type: "integer", nullable: false),
                    intelligence = table.Column<int>(type: "integer", nullable: false),
                    wisdom = table.Column<int>(type: "integer", nullable: false),
                    charisma = table.Column<int>(type: "integer", nullable: false),
                    challenge_rating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    senses = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    languages = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    actions = table.Column<string>(type: "text", nullable: true),
                    special_abilities = table.Column<string>(type: "text", nullable: true),
                    reactions = table.Column<string>(type: "text", nullable: true),
                    legendary_actions = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dnd5e_monsters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dnd5e_spells",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    school = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    casting_time = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    range = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    components = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    concentration = table.Column<bool>(type: "boolean", nullable: false),
                    ritual = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    higher_level = table.Column<string>(type: "text", nullable: true),
                    classes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    material = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dnd5e_spells", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dnd5e_monsters_cr",
                table: "dnd5e_monsters",
                column: "challenge_rating");

            migrationBuilder.CreateIndex(
                name: "ix_dnd5e_monsters_slug",
                table: "dnd5e_monsters",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dnd5e_monsters_type",
                table: "dnd5e_monsters",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_dnd5e_spells_level",
                table: "dnd5e_spells",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_dnd5e_spells_school",
                table: "dnd5e_spells",
                column: "school");

            migrationBuilder.CreateIndex(
                name: "ix_dnd5e_spells_slug",
                table: "dnd5e_spells",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dnd5e_monsters");

            migrationBuilder.DropTable(
                name: "dnd5e_spells");
        }
    }
}
