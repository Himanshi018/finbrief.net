using FinBrief.Infrastructure.Ai;

namespace FinBrief.Infrastructure.Ingestion;

// What happened during one ingest run. The console app prints it as a summary.
public class IngestReport
{
    public bool DryRun { get; init; }

    public int FeedsRead { get; set; }
    public int FeedsFailed { get; set; }
    public int ItemsFound { get; set; }

    // Why items were skipped before the AI step.
    public int DuplicatesAcrossFeeds { get; set; }
    public int TooOld { get; set; }
    public int ExcludedDomain { get; set; }
    public int StockTipClickbait { get; set; }
    public int NoFinanceSignal { get; set; }
    public int AlreadyStored { get; set; }
    public int AlreadyRejected { get; set; }

    public int Candidates { get; set; }
    public List<ProcessedItem> Processed { get; } = [];

    // Set when the AI step was stopped for the rest of the run (bad key, quota...).
    public string? AiStopReason { get; set; }

    public int Added => Processed.Count(p => p.Outcome == IngestOutcome.Added);
    public int AddedWithoutAi => Processed.Count(p => p.Outcome == IngestOutcome.AddedWithoutAi);
    public int Rejected => Processed.Count(p => p.Outcome == IngestOutcome.Rejected);
}

public enum IngestOutcome
{
    Added,            // saved with all AI fields → visible in the feed
    AddedWithoutAi,   // saved with empty AI fields → hidden until backfill-ai fills them
    Rejected,         // AI said "not finance" → saved in article_rejections
    SaveFailed,       // database error
}

// One selected item and what the run did (or, in a dry run, would do) with it.
public record ProcessedItem(
    FeedItem Item,
    string? ImageUrl,
    string ImageFrom,       // "rss", "og:image", "none" or "off for this source"
    AiBrief? Brief,
    string? AiError,
    IngestOutcome Outcome);
