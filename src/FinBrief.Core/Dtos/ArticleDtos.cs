namespace FinBrief.Core.Dtos;

// DTOs ("data transfer objects") are the shapes the API sends as JSON.
// They are separate from the entities, so the database can change without breaking the app.

// One jargon word and its one-line meaning.
public record KeyTermDto(string Term, string Meaning);

// What a feed card needs.
public record ArticleCardDto(
    int Id,
    string SimpleTitle,
    string Summary,
    string WhyItMatters,
    List<KeyTermDto> KeyTerms,
    string CategorySlug,
    string SourceName,
    string? ImageUrl,
    DateTime PublishedAt);

// The full brief for /articles/:id. Adds the original headline, the link back to the publisher,
// and up to 4 related articles from the same category.
public record ArticleDetailDto(
    int Id,
    string SimpleTitle,
    string Summary,
    string WhyItMatters,
    List<KeyTermDto> KeyTerms,
    string CategorySlug,
    string SourceName,
    string? ImageUrl,
    DateTime PublishedAt,
    string OriginalTitle,
    string Url,
    List<ArticleCardDto> Related);

// One page of the feed. NextCursor is null when there are no more articles.
public record ArticlePageDto(List<ArticleCardDto> Items, string? NextCursor);

public record CategoryDto(string Slug, string Name);
