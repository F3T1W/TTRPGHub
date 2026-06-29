using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rater_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ratee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_ratings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_ratings_users_ratee_id",
                        column: x => x.ratee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_ratings_users_rater_id",
                        column: x => x.rater_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_ratings_ratee_id",
                table: "user_ratings",
                column: "ratee_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_ratings_rater_id_ratee_id",
                table: "user_ratings",
                columns: new[] { "rater_id", "ratee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_ratings");
        }
    }
}
