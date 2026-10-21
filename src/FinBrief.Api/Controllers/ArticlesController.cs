using FinBrief.Core.Dtos;
using FinBrief.Core.Entities;
using FinBrief.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinBrief.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController(AppDbContext db) : ControllerBase
{
    private const int DefaultLimit = 10;
    private const int MaxLimit = 30;
    private const int RelatedCount = 4;

    // GET /api/articles?category=markets&search=repo&cursor=...&limit=10
    // Returns one page of the feed, newest first.
    // For the next page, send back the "nextCursor" from the previous response.
    [HttpGet]
    public async Task<ActionResult<ArticlePageDto>> GetArticles(
        string? category, string? search, string? cursor, int limit = DefaultLimit)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);

        var query = db.Articles.AsNoTracking().WhereBriefIsReady();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.CategorySlug == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILIKE is Postgres's case-insensitive LIKE. % and _ typed by the user are escaped
            // so they are searched for literally instead of acting as wildcards.
            var pattern = "%" + EscapeLike(search.Trim()) + "%";
            query = query.Where(a =>
                EF.Functions.ILike(a.SimpleTitle!, pattern) ||
                EF.Functions.ILike(a.Summary!, pattern) ||
                EF.Functions.ILike(a.Title, pattern));
        }

        // Cursor pagination: the cursor points at the last article of the previous page,
        // and this page continues with the articles that come after it.
        // Unlike page numbers, it doesn't skip or repeat items when new articles arrive.
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!TryParseCursor(cursor, out var cursorDate, out var cursorId))
            {
                return BadRequest("Invalid cursor.");
            }

            query = query.Where(a =>
                a.PublishedAt < cursorDate ||
                (a.PublishedAt == cursorDate && a.Id < cursorId));
        }

        // Ask for one extra article to find out whether another page exists.
        var articles = await query.NewestFirst().Take(limit + 1).ToListAsync();

        var hasMore = articles.Count > limit;
        if (hasMore)
        {
            articles.RemoveAt(articles.Count - 1);
        }

        var nextCursor = hasMore ? MakeCursor(articles[^1]) : null;
        return new ArticlePageDto(articles.Select(ToCard).ToList(), nextCursor);
    }

    // GET /api/articles/5
    // The full brief plus up to 4 related articles from the same category.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDetailDto>> GetArticle(int id)
    {
        var article = await db.Articles.AsNoTracking()
            .WhereBriefIsReady()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article is null)
        {
            return NotFound();
        }

        var related = await db.Articles.AsNoTracking()
            .WhereBriefIsReady()
            .Where(a => a.CategorySlug == article.CategorySlug && a.Id != article.Id)
            .NewestFirst()
            .Take(RelatedCount)
            .ToListAsync();

        return new ArticleDetailDto(
            article.Id,
            article.SimpleTitle!,
            article.Summary!,
            article.WhyItMatters!,
            ToKeyTermDtos(article),
            article.CategorySlug,
            article.SourceName,
            article.ImageUrl,
            article.PublishedAt,
            OriginalTitle: article.Title,
            Url: article.Url,
            Related: related.Select(ToCard).ToList());
    }

    // ---------- helpers ----------

    private static ArticleCardDto ToCard(Article a) => new(
        a.Id,
        a.SimpleTitle!,
        a.Summary!,
        a.WhyItMatters!,
        ToKeyTermDtos(a),
        a.CategorySlug,
        a.SourceName,
        a.ImageUrl,
        a.PublishedAt);

    private static List<KeyTermDto> ToKeyTermDtos(Article a) =>
        a.KeyTerms.Select(k => new KeyTermDto(k.Term, k.Meaning)).ToList();

    // The cursor is "<publish time in ticks>_<id>" of the last article on the page, e.g. "638962560000000000_42".
    private static string MakeCursor(Article a) => $"{a.PublishedAt.Ticks}_{a.Id}";

    private static bool TryParseCursor(string cursor, out DateTime publishedAt, out int id)
    {
        publishedAt = default;
        id = 0;

        var parts = cursor.Split('_');
        if (parts.Length != 2 ||
            !long.TryParse(parts[0], out var ticks) ||
            !int.TryParse(parts[1], out id) ||
            ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            return false;
        }

        // Npgsql requires UTC DateTimes for "timestamp with time zone" columns.
        publishedAt = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }

    private static string EscapeLike(string text) =>
        text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
