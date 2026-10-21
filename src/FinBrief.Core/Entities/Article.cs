namespace FinBrief.Core.Entities;

// One news story.
// Legal rule: we only store the title, a short description (max 280 chars), source, link, date and image URL
// from the publisher. We never store the full article text, and we always link back to Url.
public class Article
{
    public int Id { get; set; }

    // --- From the RSS feed ---
    public string Title { get; set; } = "";          // original headline
    public string? Description { get; set; }         // short RSS description, max 280 chars
    public string SourceName { get; set; } = "";
    public string Url { get; set; } = "";            // unique: the same story is never stored twice
    public string? ImageUrl { get; set; }
    public DateTime PublishedAt { get; set; }        // always UTC
    public string CategorySlug { get; set; } = "";

    // --- Written by the AI. Empty until the AI step succeeds; empty articles are hidden from the feed. ---
    public string? SimpleTitle { get; set; }
    public string? Summary { get; set; }
    public string? WhyItMatters { get; set; }
    public List<KeyTerm> KeyTerms { get; set; } = [];

    public DateTime CreatedAt { get; set; }          // when we saved it (UTC)
}
