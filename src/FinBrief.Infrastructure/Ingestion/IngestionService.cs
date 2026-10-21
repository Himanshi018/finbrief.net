using System.Xml;
using FinBrief.Core.Entities;
using FinBrief.Infrastructure.Ai;
using FinBrief.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBrief.Infrastructure.Ingestion;

// The whole ingestion pipeline: read feeds → filter → pick → AI → save.
// With dryRun = true, every step runs except saving, so you can see what would be added.
public class IngestionService(
    AppDbContext db,
    IngestionSettings settings,
    RssReader rss,
    ImageFetcher images,
    ArticleAiClient ai,
    ILogger<IngestionService> logger)
{
    public async Task<IngestReport> RunAsync(bool dryRun, int max, CancellationToken ct = default)
    {
        var report = new IngestReport { DryRun = dryRun };

        var categorySlugs = await db.Categories.OrderBy(c => c.SortOrder).Select(c => c.Slug).ToListAsync(ct);

        // Steps 1-4: read every feed, then drop what we don't want.
        var allItems = await ReadFeedsAsync(categorySlugs, report, ct);
        var candidates = Filter(allItems, report);
        candidates = await RemoveKnownUrlsAsync(candidates, report, ct);
        report.Candidates = candidates.Count;

        // Step 5: fair selection across categories.
        var selected = await SelectRoundRobinAsync(candidates, categorySlugs, max, ct);
        logger.LogInformation("{Candidates} new candidates, {Selected} selected (max {Max})", candidates.Count, selected.Count, max);

        if (!ai.IsConfigured)
        {
            report.AiStopReason = "No Groq API key configured (Groq:ApiKey), so the AI step was skipped.";
        }

        // Steps 6-9: image, AI, save, one article at a time.
        foreach (var item in selected)
        {
            report.Processed.Add(await ProcessAsync(item, report, dryRun, ct));
        }

        return report;
    }

    private async Task<List<FeedItem>> ReadFeedsAsync(List<string> categorySlugs, IngestReport report, CancellationToken ct)
    {
        var items = new List<FeedItem>();

        foreach (var feed in settings.Feeds)
        {
            // A typo in appsettings.json would otherwise fail later with a foreign key error.
            if (!categorySlugs.Contains(feed.Category))
            {
                logger.LogError("Feed {Url} has unknown category '{Category}', skipping it", feed.Url, feed.Category);
                report.FeedsFailed++;
                continue;
            }

            try
            {
                var feedItems = await rss.ReadAsync(feed, ct);
                items.AddRange(feedItems);
                report.FeedsRead++;
                logger.LogInformation("{Source} / {Topic}: {Count} items", feed.Source, feed.Topic, feedItems.Count);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or XmlException)
            {
                // One broken feed shouldn't stop the others.
                report.FeedsFailed++;
                logger.LogWarning("{Source} / {Topic} failed: {Error}", feed.Source, feed.Topic, ex.Message);
            }
        }

        report.ItemsFound = items.Count;
        return items;
    }

    // Steps 2-4 (the parts that don't need the database).
    private List<FeedItem> Filter(List<FeedItem> items, IngestReport report)
    {
        var cutoff = DateTime.UtcNow.AddHours(-settings.MaxAgeHours);
        var seenUrls = new HashSet<string>();
        var kept = new List<FeedItem>();

        // Newest first, so if the same story is in two feeds we keep one copy.
        foreach (var item in items.OrderByDescending(i => i.PublishedAt))
        {
            var text = item.Title + " " + item.Description;

            if (!seenUrls.Add(item.Url)) report.DuplicatesAcrossFeeds++;
            else if (item.PublishedAt < cutoff) report.TooOld++;
            else if (NewsFilter.IsExcludedDomain(item.Url)) report.ExcludedDomain++;
            else if (NewsFilter.IsStockTipClickbait(item.Title)) report.StockTipClickbait++;
            else if (!NewsFilter.HasFinanceSignal(text)) report.NoFinanceSignal++;
            else kept.Add(item with { CategorySlug = NewsFilter.PickCategory(item) });
        }

        return kept;
    }

    // Step 4 (database part): skip URLs we already stored or the AI already rejected.
    private async Task<List<FeedItem>> RemoveKnownUrlsAsync(List<FeedItem> items, IngestReport report, CancellationToken ct)
    {
        var urls = items.Select(i => i.Url).ToList();

        var stored = (await db.Articles.Where(a => urls.Contains(a.Url)).Select(a => a.Url).ToListAsync(ct)).ToHashSet();
        var rejected = (await db.ArticleRejections.Where(r => urls.Contains(r.Url)).Select(r => r.Url).ToListAsync(ct)).ToHashSet();

        report.AlreadyStored = stored.Count;
        report.AlreadyRejected = rejected.Count;

        return items.Where(i => !stored.Contains(i.Url) && !rejected.Contains(i.Url)).ToList();
    }

    // Step 5: take one article per category in turn until we have "max".
    // Categories with the fewest stored articles go first, so small categories aren't starved
    // when max is smaller than the number of categories.
    private async Task<List<FeedItem>> SelectRoundRobinAsync(
        List<FeedItem> candidates, List<string> categorySlugs, int max, CancellationToken ct)
    {
        var counts = await db.Articles
            .GroupBy(a => a.CategorySlug)
            .Select(g => new { Slug = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Slug, x => x.Count, ct);

        var queues = categorySlugs
            .OrderBy(slug => counts.GetValueOrDefault(slug))   // stable: ties keep the category sort order
            .Select(slug => new Queue<FeedItem>(candidates
                .Where(c => c.CategorySlug == slug)
                .OrderByDescending(c => c.PublishedAt)))
            .ToList();

        var selected = new List<FeedItem>();
        while (selected.Count < max && queues.Any(q => q.Count > 0))
        {
            foreach (var queue in queues)
            {
                if (selected.Count < max && queue.TryDequeue(out var item))
                {
                    selected.Add(item);
                }
            }
        }

        return selected;
    }

    private async Task<ProcessedItem> ProcessAsync(FeedItem item, IngestReport report, bool dryRun, CancellationToken ct)
    {
        // Step 9: image from RSS, else from the page's og:image, else none.
        // Publishers with "UseImages": false in appsettings.json never get an image.
        string? imageUrl = null;
        string imageFrom;
        if (!settings.ForSource(item.SourceName).UseImages)
        {
            imageFrom = "off for this source";
        }
        else if (item.ImageUrl is not null)
        {
            imageUrl = item.ImageUrl;
            imageFrom = "rss";
        }
        else
        {
            imageUrl = await images.FindOgImageAsync(item.Url, ct);
            imageFrom = imageUrl is null ? "none" : "og:image";
        }

        // Step 6: one AI call per article, unless the AI was stopped earlier in this run.
        AiBrief? brief = null;
        string? aiError = null;
        if (report.AiStopReason is null)
        {
            try
            {
                brief = await ai.ExplainAsync(item, ct);
            }
            catch (AiStopException ex)
            {
                // Step 8: fail fast. One clear message, then no more AI calls this run.
                report.AiStopReason = ex.Message;
                aiError = ex.Message;
                logger.LogError("AI stopped: {Reason} No more AI calls this run.", ex.Message);
            }
            catch (AiArticleException ex)
            {
                aiError = ex.Message;
                logger.LogWarning("AI failed for \"{Title}\": {Error}", item.Title, ex.Message);
            }
        }

        // Step 7: relevant → articles, not relevant → article_rejections.
        // If the AI step failed, the article is saved without AI fields so backfill-ai can retry it.
        var outcome = brief switch
        {
            { IsFinanceRelevant: false } => IngestOutcome.Rejected,
            not null => IngestOutcome.Added,
            null => IngestOutcome.AddedWithoutAi,
        };

        if (!dryRun && !await SaveAsync(item, imageUrl, brief, ct))
        {
            outcome = IngestOutcome.SaveFailed;
        }

        logger.LogInformation("{Outcome}: [{Category}] {Title}", outcome, item.CategorySlug, item.Title);
        return new ProcessedItem(item, imageUrl, imageFrom, brief, aiError, outcome);
    }

    private async Task<bool> SaveAsync(FeedItem item, string? imageUrl, AiBrief? brief, CancellationToken ct)
    {
        if (brief is { IsFinanceRelevant: false })
        {
            db.ArticleRejections.Add(new ArticleRejection
            {
                Url = item.Url,
                Reason = brief.Reason,   // cut to 500 characters by the entity
                RejectedAt = DateTime.UtcNow,
            });
        }
        else
        {
            db.Articles.Add(new Article
            {
                Title = item.Title,
                Description = item.Description,
                SourceName = item.SourceName,
                Url = item.Url,
                ImageUrl = imageUrl,
                PublishedAt = item.PublishedAt,
                CategorySlug = item.CategorySlug,
                SimpleTitle = brief?.SimpleTitle,
                Summary = brief?.Summary,
                WhyItMatters = brief?.WhyItMatters,
                KeyTerms = brief?.KeyTerms ?? [],
                CreatedAt = DateTime.UtcNow,
            });
        }

        // Saved one at a time, so a crash halfway through keeps the articles already done.
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError("Could not save \"{Title}\": {Error}", item.Title, ex.InnerException?.Message ?? ex.Message);
            db.ChangeTracker.Clear();   // forget the failed row so the next save doesn't retry it
            return false;
        }
    }
}
