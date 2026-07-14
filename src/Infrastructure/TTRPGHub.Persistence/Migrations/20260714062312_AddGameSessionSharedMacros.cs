using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSessionSharedMacros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "shared_macro_ids",
                table: "game_sessions",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shared_macro_ids",
                table: "game_sessions");
        }
    }
}
