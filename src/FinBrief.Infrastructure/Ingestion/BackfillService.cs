using FinBrief.Core.Entities;
using FinBrief.Infrastructure.Ai;
using FinBrief.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBrief.Infrastructure.Ingestion;

public class BackfillReport
{
    public bool DryRun { get; init; }
    public int MissingAi { get; set; }        // articles found without AI text
    public int Filled { get; set; }           // AI text added
    public int Rejected { get; set; }         // AI said "not finance": moved to article_rejections
    public int Failed { get; set; }           // AI failed again; tried next time
    public int MissingImages { get; set; }    // only with --images
    public int ImagesFound { get; set; }
    public string? AiStopReason { get; set; }
}

// "backfill-ai": finishes articles that were saved while the AI step failed (they are hidden from the feed),
// and with --images also looks for og:images for articles that have none.
public class BackfillService(
    AppDbContext db,
    IngestionSettings settings,
    ArticleAiClient ai,
    ImageFetcher images,
    ILogger<BackfillService> logger)
{
    public async Task<BackfillReport> RunAsync(bool dryRun, int max, bool fillImages, CancellationToken ct = default)
    {
        var report = new BackfillReport { DryRun = dryRun };

        await FillAiTextAsync(report, dryRun, max, ct);

        if (fillImages)
        {
            await FillImagesAsync(report, dryRun, max, ct);
        }

        return report;
    }

    private async Task FillAiTextAsync(BackfillReport report, bool dryRun, int max, CancellationToken ct)
    {
        var articles = await db.Articles.WhereBriefIsMissing().NewestFirst().Take(max).ToListAsync(ct);
        report.MissingAi = articles.Count;
        logger.LogInformation("{Count} articles without AI text (max {Max})", articles.Count, max);

        if (!ai.IsConfigured)
        {
            report.AiStopReason = "No Groq API key configured (Groq:ApiKey), so the AI step was skipped.";
            return;
        }

        foreach (var article in articles)
        {
            AiBrief brief;
            try
            {
                brief = await ai.ExplainAsync(ToFeedItem(article), ct);
            }
            catch (AiStopException ex)
            {
                // Same fail-fast rule as ingest: one clear message, no more AI calls.
                report.AiStopReason = ex.Message;
                logger.LogError("AI stopped: {Reason} No more AI calls this run.", ex.Message);
                return;
            }
            catch (AiArticleException ex)
            {
                report.Failed++;
                logger.LogWarning("AI failed again for \"{Title}\": {Error}", article.Title, ex.Message);
                continue;
            }

            if (brief.IsFinanceRelevant)
            {
                article.SimpleTitle = brief.SimpleTitle;
                article.Summary = brief.Summary;
                article.WhyItMatters = brief.WhyItMatters;
                article.KeyTerms = brief.KeyTerms;
                report.Filled++;
                logger.LogInformation("Filled: [{Category}] {Title}", article.CategorySlug, brief.SimpleTitle);
            }
            else
            {
                // Not finance after all: remove it and remember the URL so ingest never picks it again.
                db.Articles.Remove(article);
                db.ArticleRejections.Add(new ArticleRejection { Url = article.Url, Reason = brief.Reason, RejectedAt = DateTime.UtcNow });
                report.Rejected++;
                logger.LogInformation("Rejected: {Title} ({Reason})", article.Title, brief.Reason);
            }

            if (!dryRun)
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task FillImagesAsync(BackfillReport report, bool dryRun, int max, CancellationToken ct)
    {
        // The table is small, so filter by the per-source "UseImages" setting in memory.
        var withoutImage = await db.Articles.Where(a => a.ImageUrl == null).NewestFirst().ToListAsync(ct);
        var articles = withoutImage
            .Where(a => settings.ForSource(a.SourceName).UseImages)
            .Take(max)
            .ToList();

        report.MissingImages = articles.Count;
        logger.LogInformation("{Count} articles without an image from sources that allow images", articles.Count);

        foreach (var article in articles)
        {
            var imageUrl = await images.FindOgImageAsync(article.Url, ct);
            if (imageUrl is null)
            {
                continue;
            }

            article.ImageUrl = imageUrl;
            report.ImagesFound++;
            logger.LogInformation("Image found: {Title}", article.Title);

            if (!dryRun)
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static FeedItem ToFeedItem(Article a) =>
        new(a.Title, a.Description, a.Url, a.PublishedAt, a.ImageUrl, a.SourceName, a.CategorySlug);
}
