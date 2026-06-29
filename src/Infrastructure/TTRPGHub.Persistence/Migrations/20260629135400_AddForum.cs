using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TTRPGHub.Migrations
{
    /// <inheritdoc />
    public partial class AddForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "forum_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forum_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "homebrew_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    tags = table.Column<string>(type: "text", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homebrew_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_homebrew_items_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forum_topics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_post_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forum_topics", x => x.id);
                    table.ForeignKey(
                        name: "FK_forum_topics_forum_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "forum_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_forum_topics_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "homebrew_likes",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homebrew_likes", x => new { x.item_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_homebrew_likes_homebrew_items_item_id",
                        column: x => x.item_id,
                        principalTable: "homebrew_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_homebrew_likes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "forum_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forum_posts", x => x.id);
                    table.ForeignKey(
                        name: "FK_forum_posts_forum_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "forum_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_forum_posts_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "forum_post_likes",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forum_post_likes", x => new { x.post_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_forum_post_likes_forum_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "forum_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_forum_post_likes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_forum_categories_slug",
                table: "forum_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_forum_post_likes_user_id",
                table: "forum_post_likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_forum_posts_author_id",
                table: "forum_posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_forum_posts_topic_id",
                table: "forum_posts",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_forum_topics_author_id",
                table: "forum_topics",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_forum_topics_category_id",
                table: "forum_topics",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_homebrew_items_author_id",
                table: "homebrew_items",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_homebrew_items_system",
                table: "homebrew_items",
                column: "system");

            migrationBuilder.CreateIndex(
                name: "IX_homebrew_items_type",
                table: "homebrew_items",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_homebrew_likes_user_id",
                table: "homebrew_likes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "forum_post_likes");

            migrationBuilder.DropTable(
                name: "homebrew_likes");

            migrationBuilder.DropTable(
                name: "forum_posts");

            migrationBuilder.DropTable(
                name: "homebrew_items");

            migrationBuilder.DropTable(
                name: "forum_topics");

            migrationBuilder.DropTable(
                name: "forum_categories");
        }
    }
}
