using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinBrief.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "article_rejections",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_rejections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.UniqueConstraint("ak_categories_slug", x => x.slug);
                });

            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    source_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    category_slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    simple_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    why_it_matters = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    key_terms = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_articles_categories_category_slug",
                        column: x => x.category_slug,
                        principalTable: "categories",
                        principalColumn: "slug",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_articles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    article_id = table.Column<int>(type: "integer", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_articles_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_saved_articles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "name", "slug", "sort_order" },
                values: new object[,]
                {
                    { 1, "Markets", "markets", 1 },
                    { 2, "Economy", "economy", 2 },
                    { 3, "Startups", "startups", 3 },
                    { 4, "Fintech", "fintech", 4 },
                    { 5, "AI Business", "ai-business", 5 },
                    { 6, "Global Finance", "global-finance", 6 }
                });

            migrationBuilder.InsertData(
                table: "sources",
                columns: new[] { "id", "domain", "name" },
                values: new object[,]
                {
                    { 1, "economictimes.indiatimes.com", "Economic Times" },
                    { 2, "livemint.com", "Livemint" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_rejections_url",
                table: "article_rejections",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_category_slug_published_at",
                table: "articles",
                columns: new[] { "category_slug", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ix_articles_published_at_id",
                table: "articles",
                columns: new[] { "published_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_articles_url",
                table: "articles",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_saved_articles_article_id",
                table: "saved_articles",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_saved_articles_user_id_article_id",
                table: "saved_articles",
                columns: new[] { "user_id", "article_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sources_name",
                table: "sources",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_rejections");

            migrationBuilder.DropTable(
                name: "saved_articles");

            migrationBuilder.DropTable(
                name: "sources");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
