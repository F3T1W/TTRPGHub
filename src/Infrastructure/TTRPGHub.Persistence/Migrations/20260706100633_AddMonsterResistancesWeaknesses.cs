using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterResistancesWeaknesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "resistances_json",
                table: "pf2e_monsters",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "weaknesses_json",
                table: "pf2e_monsters",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "resistances_json",
                table: "pf2e_monsters");

            migrationBuilder.DropColumn(
                name: "weaknesses_json",
                table: "pf2e_monsters");
        }
    }
}
