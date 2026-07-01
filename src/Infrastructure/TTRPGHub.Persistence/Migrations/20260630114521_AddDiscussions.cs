using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscussions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discussion_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_slug = table.Column<string>(type: "text", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discussion_posts", x => x.id);
                    table.ForeignKey(
                        name: "FK_discussion_posts_discussion_posts_parent_id",
                        column: x => x.parent_id,
                        principalTable: "discussion_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_discussion_posts_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discussion_likes",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discussion_likes", x => new { x.post_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_discussion_likes_discussion_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "discussion_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_discussion_likes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discussion_likes_user_id",
                table: "discussion_likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discussion_posts_author_id",
                table: "discussion_posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_discussion_posts_entity_type_entity_slug",
                table: "discussion_posts",
                columns: new[] { "entity_type", "entity_slug" });

            migrationBuilder.CreateIndex(
                name: "IX_discussion_posts_parent_id",
                table: "discussion_posts",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discussion_likes");

            migrationBuilder.DropTable(
                name: "discussion_posts");
        }
    }
}
