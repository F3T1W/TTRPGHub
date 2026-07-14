using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSpellAutomationJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "damage_json",
                table: "pf2e_spells",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "defense_json",
                table: "pf2e_spells",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "heightening_json",
                table: "pf2e_spells",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "damage_json",
                table: "pf2e_spells");

            migrationBuilder.DropColumn(
                name: "defense_json",
                table: "pf2e_spells");

            migrationBuilder.DropColumn(
                name: "heightening_json",
                table: "pf2e_spells");
        }
    }
}
