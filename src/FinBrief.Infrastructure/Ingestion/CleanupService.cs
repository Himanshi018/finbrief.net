using FinBrief.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBrief.Infrastructure.Ingestion;

public class CleanupReport
{
    public bool DryRun { get; init; }
    public int OldArticles { get; set; }
    public int UnfinishedArticles { get; set; }
    public int OldRejections { get; set; }
}

// "cleanup": keeps the database small (Supabase's free tier has a size limit) by deleting
// old articles, articles whose AI step never succeeded, and old rejected URLs.
// The limits come from the "Cleanup" section of appsettings.json.
public class CleanupService(AppDbContext db, CleanupSettings settings, ILogger<CleanupService> logger)
{
    public async Task<CleanupReport> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var articleCutoff = now.AddDays(-settings.KeepArticlesDays);
        var unfinishedCutoff = now.AddDays(-settings.KeepUnfinishedDays);
        var rejectionCutoff = now.AddDays(-settings.KeepRejectionsDays);

        // Old articles, except ones a user has saved.
        var oldArticles = db.Articles.Where(a =>
            a.PublishedAt < articleCutoff && !db.SavedArticles.Any(s => s.ArticleId == a.Id));

        // Hidden articles that backfill-ai never managed to finish.
        var unfinishedArticles = db.Articles.WhereBriefIsMissing().Where(a => a.CreatedAt < unfinishedCutoff);

        var oldRejections = db.ArticleRejections.Where(r => r.RejectedAt < rejectionCutoff);

        var report = new CleanupReport { DryRun = dryRun };

        if (dryRun)
        {
            // Only count what would be deleted.
            report.OldArticles = await oldArticles.CountAsync(ct);
            report.UnfinishedArticles = await unfinishedArticles.CountAsync(ct);
            report.OldRejections = await oldRejections.CountAsync(ct);
        }
        else
        {
            // ExecuteDeleteAsync runs one DELETE statement in the database, without loading the rows first.
            report.OldArticles = await oldArticles.ExecuteDeleteAsync(ct);
            report.UnfinishedArticles = await unfinishedArticles.ExecuteDeleteAsync(ct);
            report.OldRejections = await oldRejections.ExecuteDeleteAsync(ct);
        }

        logger.LogInformation(
            "Cleanup {Mode}: {Old} old articles, {Unfinished} unfinished articles, {Rejections} old rejections",
            dryRun ? "(dry run)" : "done", report.OldArticles, report.UnfinishedArticles, report.OldRejections);

        return report;
    }
}
