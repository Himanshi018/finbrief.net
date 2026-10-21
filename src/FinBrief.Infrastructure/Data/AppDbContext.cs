using FinBrief.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinBrief.Infrastructure.Data;

// The DbContext is EF Core's "gateway" to the database.
// Each DbSet below becomes a table; OnModelCreating adds rules (lengths, indexes, seed data).
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleRejection> ArticleRejections => Set<ArticleRejection>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SavedArticle> SavedArticles => Set<SavedArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(b =>
        {
            b.Property(c => c.Slug).HasMaxLength(50);
            b.Property(c => c.Name).HasMaxLength(100);
            b.HasIndex(c => c.Slug).IsUnique();

            b.HasData(
                new Category { Id = 1, Slug = "markets", Name = "Markets", SortOrder = 1 },
                new Category { Id = 2, Slug = "economy", Name = "Economy", SortOrder = 2 },
                new Category { Id = 3, Slug = "startups", Name = "Startups", SortOrder = 3 },
                new Category { Id = 4, Slug = "fintech", Name = "Fintech", SortOrder = 4 },
                new Category { Id = 5, Slug = "ai-business", Name = "AI Business", SortOrder = 5 },
                new Category { Id = 6, Slug = "global-finance", Name = "Global Finance", SortOrder = 6 });
        });

        modelBuilder.Entity<Source>(b =>
        {
            b.Property(s => s.Name).HasMaxLength(100);
            b.Property(s => s.Domain).HasMaxLength(200);
            b.HasIndex(s => s.Name).IsUnique();

            b.HasData(
                new Source { Id = 1, Name = "Economic Times", Domain = "economictimes.indiatimes.com" },
                new Source { Id = 2, Name = "Livemint", Domain = "livemint.com" });
        });

        modelBuilder.Entity<Article>(b =>
        {
            b.Property(a => a.Title).HasMaxLength(500);
            b.Property(a => a.Description).HasMaxLength(280);
            b.Property(a => a.SourceName).HasMaxLength(100);
            b.Property(a => a.Url).HasMaxLength(1000);
            b.Property(a => a.ImageUrl).HasMaxLength(1000);
            b.Property(a => a.CategorySlug).HasMaxLength(50);
            b.Property(a => a.SimpleTitle).HasMaxLength(300);
            b.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

            b.HasIndex(a => a.Url).IsUnique();
            // Speeds up "newest first" queries, with or without a category filter.
            b.HasIndex(a => new { a.PublishedAt, a.Id });
            b.HasIndex(a => new { a.CategorySlug, a.PublishedAt });

            // Every article must belong to an existing category (linked by slug, not by Id).
            b.HasOne<Category>()
                .WithMany()
                .HasForeignKey(a => a.CategorySlug)
                .HasPrincipalKey(c => c.Slug)
                .OnDelete(DeleteBehavior.Restrict);

            // KeyTerms is stored as one jsonb column: [{"term": "...", "meaning": "..."}]
            b.OwnsMany(a => a.KeyTerms, kt =>
            {
                kt.ToJson("key_terms");
                kt.Property(k => k.Term).HasJsonPropertyName("term");
                kt.Property(k => k.Meaning).HasJsonPropertyName("meaning");
            });
        });

        modelBuilder.Entity<ArticleRejection>(b =>
        {
            b.Property(r => r.Url).HasMaxLength(1000);
            b.Property(r => r.Reason).HasMaxLength(ArticleRejection.ReasonMaxLength);
            b.Property(r => r.RejectedAt).HasDefaultValueSql("now()");
            b.HasIndex(r => r.Url).IsUnique();
        });

        modelBuilder.Entity<User>(b =>
        {
            b.Property(u => u.Email).HasMaxLength(320);
            b.Property(u => u.DisplayName).HasMaxLength(100);
            b.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<SavedArticle>(b =>
        {
            b.Property(s => s.SavedAt).HasDefaultValueSql("now()");
            // A user can save the same article only once.
            b.HasIndex(s => new { s.UserId, s.ArticleId }).IsUnique();
            // Deleting a user or an article also deletes their "saved" rows.
            b.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(s => s.Article).WithMany().HasForeignKey(s => s.ArticleId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
