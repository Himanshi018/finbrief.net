using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinBrief.Infrastructure.Data.Migrations
{
    // Supabase exposes every table in the "public" schema through its auto-generated REST API.
    // Turning on row level security (RLS) with no policies blocks that API from reading or writing anything.
    // Our app is unaffected: it connects directly as the table owner, and owners skip RLS.
    // Any table added in a later migration needs the same ALTER TABLE line.
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        private static readonly string[] Tables =
        [
            "categories",
            "sources",
            "articles",
            "article_rejections",
            "users",
            "saved_articles",
            "__EFMigrationsHistory",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE public.\"{table}\" ENABLE ROW LEVEL SECURITY;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE public.\"{table}\" DISABLE ROW LEVEL SECURITY;");
            }
        }
    }
}
