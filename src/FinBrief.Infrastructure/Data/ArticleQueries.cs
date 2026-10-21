using FinBrief.Core.Entities;

namespace FinBrief.Infrastructure.Data;

// Reusable query filters for articles, so every endpoint uses the same rules.
public static class ArticleQueries
{
    // Only articles whose AI step succeeded are shown to readers.
    // Articles saved with empty AI fields stay hidden until backfill-ai fills them.
    public static IQueryable<Article> WhereBriefIsReady(this IQueryable<Article> articles) =>
        articles.Where(a =>
            a.SimpleTitle != null && a.SimpleTitle != "" &&
            a.Summary != null && a.Summary != "" &&
            a.WhyItMatters != null && a.WhyItMatters != "" &&
            a.KeyTerms.Count > 0);

    // Newest first. Id breaks ties when two articles have the same publish time.
    public static IOrderedQueryable<Article> NewestFirst(this IQueryable<Article> articles) =>
        articles.OrderByDescending(a => a.PublishedAt).ThenByDescending(a => a.Id);
}
