using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSceneEnvironmentToScene : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ambient_lighting",
                table: "scenes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "bright");

            migrationBuilder.AddColumn<string>(
                name: "terrain_tags_json",
                table: "scenes",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ambient_lighting",
                table: "scenes");

            migrationBuilder.DropColumn(
                name: "terrain_tags_json",
                table: "scenes");
        }
    }
}
