using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    online_link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_events_users_organizer_id",
                        column: x => x.organizer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_participants",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_participants", x => new { x.event_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_event_participants_game_events_event_id",
                        column: x => x.event_id,
                        principalTable: "game_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_event_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_participants_user_id",
                table: "event_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_events_organizer_id",
                table: "game_events",
                column: "organizer_id");

            migrationBuilder.CreateIndex(
                name: "IX_game_events_starts_at",
                table: "game_events",
                column: "starts_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_participants");

            migrationBuilder.DropTable(
                name: "game_events");
        }
    }
}
