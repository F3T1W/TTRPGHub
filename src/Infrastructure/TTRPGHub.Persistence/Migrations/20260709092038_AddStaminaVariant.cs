using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddStaminaVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "current_stamina",
                table: "table_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_stamina",
                table: "table_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "stamina_variant",
                table: "game_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_stamina",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "max_stamina",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "stamina_variant",
                table: "game_sessions");
        }
    }
}
