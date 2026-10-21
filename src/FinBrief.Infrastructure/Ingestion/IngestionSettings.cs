namespace FinBrief.Infrastructure.Ingestion;

// These classes are filled from the "Ingestion" and "Groq" sections of appsettings.json
// (plus user-secrets / environment variables for the API key).

public class IngestionSettings
{
    // Items older than this are skipped.
    public int MaxAgeHours { get; set; } = 72;

    public List<FeedSettings> Feeds { get; set; } = [];
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

    // Secret: comes from user-secrets ("Groq:ApiKey") or the environment variable Groq__ApiKey.
    public string? ApiKey { get; set; }
}
