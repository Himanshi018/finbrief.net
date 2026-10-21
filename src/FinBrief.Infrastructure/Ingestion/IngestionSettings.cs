namespace FinBrief.Infrastructure.Ingestion;

// These classes are filled from the "Ingestion" and "Groq" sections of appsettings.json
// (plus user-secrets / environment variables for the API key).

public class IngestionSettings
{
    // Items older than this are skipped.
    public int MaxAgeHours { get; set; } = 72;

    public List<FeedSettings> Feeds { get; set; } = [];

    // Per-publisher options, keyed by source name, e.g. "Livemint": { "UseImages": false }.
    // A source that isn't listed uses the defaults.
    public Dictionary<string, SourceSettings> Sources { get; set; } = [];

    public SourceSettings ForSource(string sourceName) =>
        Sources.GetValueOrDefault(sourceName) ?? new SourceSettings();
}

// How long the cleanup command keeps things (the "Cleanup" section of appsettings.json).
public class CleanupSettings
{
    // Articles older than this are deleted, unless a user saved them.
    public int KeepArticlesDays { get; set; } = 30;

    // Articles whose AI step never succeeded (hidden from the feed) are deleted after this.
    public int KeepUnfinishedDays { get; set; } = 7;

    // Rejected URLs only matter while their RSS item is younger than MaxAgeHours (72 h);
    // after that, ingest skips them for being too old anyway. Keep this longer than 3 days.
    public int KeepRejectionsDays { get; set; } = 7;
}

public class SourceSettings
{
    // false = never store an image for this publisher (e.g. when its thumbnails are mostly AI graphics).
    public bool UseImages { get; set; } = true;
}

// One RSS feed, e.g. Economic Times "markets" → our "markets" category.
public class FeedSettings
{
    public string Source { get; set; } = "";     // must match a name in the sources table
    public string Topic { get; set; } = "";      // only used in logs, e.g. "funding"
    public string Category { get; set; } = "";   // our category slug, e.g. "startups"
    public string Url { get; set; } = "";
}

public class GroqSettings
{
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public string Model { get; set; } = "openai/gpt-oss-120b";

    // gpt-oss "thinks" before answering, and those reasoning tokens count toward this limit,
    // so it must leave room for both the reasoning and the JSON answer.
    public int MaxOutputTokens { get; set; } = 2048;

    // Pause between AI calls so we stay under Groq's free-tier limit of 8,000 tokens per minute.
    // One article uses about 830 tokens, so 8 seconds apart is about 7 calls (≈6,000 tokens) a minute.
    public double MinSecondsBetweenCalls { get; set; } = 8;

    // Secret: comes from user-secrets ("Groq:ApiKey") or the environment variable Groq__ApiKey.
    public string? ApiKey { get; set; }
}
