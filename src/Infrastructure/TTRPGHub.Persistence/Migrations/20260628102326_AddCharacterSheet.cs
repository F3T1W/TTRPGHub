using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notes",
                table: "characters");

            migrationBuilder.AlterColumn<string>(
                name: "race",
                table: "characters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "characters",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "class",
                table: "characters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<string>(
                name: "alignment",
                table: "characters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "armor_class",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "background",
                table: "characters",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bonds",
                table: "characters",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "charisma",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "constitution",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "current_hit_points",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "dexterity",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "equipment",
                table: "characters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "experience_points",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "features_and_traits",
                table: "characters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "flaws",
                table: "characters",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hit_dice",
                table: "characters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "1d8");

            migrationBuilder.AddColumn<string>(
                name: "ideals",
                table: "characters",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "intelligence",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "max_hit_points",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "personality_traits",
                table: "characters",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "saving_throw_proficiencies",
                table: "characters",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<string>>(
                name: "skill_proficiencies",
                table: "characters",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "speed",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "strength",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "temporary_hit_points",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wisdom",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alignment",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "armor_class",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "background",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "bonds",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "charisma",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "constitution",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "current_hit_points",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "dexterity",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "equipment",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "experience_points",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "features_and_traits",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "flaws",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "hit_dice",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "ideals",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "intelligence",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "max_hit_points",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "personality_traits",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "saving_throw_proficiencies",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "skill_proficiencies",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "speed",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "strength",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "temporary_hit_points",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "wisdom",
                table: "characters");

            migrationBuilder.AlterColumn<string>(
                name: "race",
                table: "characters",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "characters",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "class",
                table: "characters",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "characters",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);
        }
    }
}
