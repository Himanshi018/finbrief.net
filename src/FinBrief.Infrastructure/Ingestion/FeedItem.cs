namespace FinBrief.Infrastructure.Ingestion;

// One story read from an RSS feed, already cleaned up (HTML removed, lengths limited, UTC date).
// Only the fields we are legally allowed to store.
public record FeedItem(
    string Title,
    string? Description,
    string Url,
    DateTime PublishedAt,
    string? ImageUrl,
    string SourceName,
    string CategorySlug);
