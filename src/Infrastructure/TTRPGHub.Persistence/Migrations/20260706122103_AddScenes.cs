using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddScenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Данные-миграция (J.4): у каждой существующей сессии карта/сетка/туман/стены/свет/бой
            // жили прямо на game_sessions — переносим их в новую таблицу scenes (по одной сцене
            // "Сцена 1" на сессию) и перепривязываем существующие токены, прежде чем удалять
            // старые колонки. Без этого апгрейд стёр бы карты/токены всех уже идущих игр.
            migrationBuilder.CreateTable(
                name: "scenes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    showcase_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    grid_cell_size_px = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    fog_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    vision_radius_feet = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    walls_json = table.Column<string>(type: "jsonb", nullable: true),
                    lights_json = table.Column<string>(type: "jsonb", nullable: true),
                    combat_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    combat_round = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    combat_turn_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scenes_session_id",
                table: "scenes",
                column: "session_id");

            migrationBuilder.Sql(@"
                INSERT INTO scenes (id, session_id, name, sort_order, showcase_image_url,
                    grid_cell_size_px, fog_enabled, vision_radius_feet, walls_json, lights_json,
                    combat_active, combat_round, combat_turn_token_id, created_at, updated_at)
                SELECT gen_random_uuid(), id, 'Сцена 1', 0, current_showcase_image_url,
                    grid_cell_size_px, fog_enabled, vision_radius_feet, walls_json, lights_json,
                    combat_active, combat_round, combat_turn_token_id, created_at, updated_at
                FROM game_sessions;
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "scene_id",
                table: "table_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE table_tokens t SET scene_id = s.id
                FROM scenes s WHERE s.session_id = t.session_id;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "scene_id",
                table: "table_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_table_tokens_scene_id",
                table: "table_tokens",
                column: "scene_id");

            // Временно переиспользуем старую колонку combat_turn_token_id для переноса id новой
            // активной сцены — после SQL-обновления она переименовывается в active_scene_id ниже,
            // так что значение окажется в правильном месте под правильным именем.
            migrationBuilder.Sql(@"
                UPDATE game_sessions gs SET combat_turn_token_id = s.id
                FROM scenes s WHERE s.session_id = gs.id;
            ");

            migrationBuilder.DropColumn(
                name: "combat_active",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "combat_round",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "current_showcase_image_url",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "fog_enabled",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "grid_cell_size_px",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "lights_json",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "vision_radius_feet",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "walls_json",
                table: "game_sessions");

            migrationBuilder.RenameColumn(
                name: "combat_turn_token_id",
                table: "game_sessions",
                newName: "active_scene_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scenes");

            migrationBuilder.DropIndex(
                name: "IX_table_tokens_scene_id",
                table: "table_tokens");

            migrationBuilder.DropColumn(
                name: "scene_id",
                table: "table_tokens");

            migrationBuilder.RenameColumn(
                name: "active_scene_id",
                table: "game_sessions",
                newName: "combat_turn_token_id");

            migrationBuilder.AddColumn<bool>(
                name: "combat_active",
                table: "game_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "combat_round",
                table: "game_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "current_showcase_image_url",
                table: "game_sessions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "fog_enabled",
                table: "game_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "grid_cell_size_px",
                table: "game_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<string>(
                name: "lights_json",
                table: "game_sessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vision_radius_feet",
                table: "game_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "walls_json",
                table: "game_sessions",
                type: "jsonb",
                nullable: true);
        }
    }
}
