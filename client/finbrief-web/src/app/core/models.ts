// TypeScript versions of the API's DTOs (src/FinBrief.Core/Dtos/ArticleDtos.cs).
// The API sends camelCase JSON, so the names match these properties.

export interface KeyTerm {
  term: string;
  meaning: string;
}

// What a feed card needs.
export interface ArticleCard {
  id: number;
  simpleTitle: string;
  summary: string;
  whyItMatters: string;
  keyTerms: KeyTerm[];
  categorySlug: string;
  sourceName: string;
  imageUrl: string | null;
  publishedAt: string; // ISO date in UTC, e.g. "2026-10-21T11:39:14Z"
}

// The full brief for /articles/:id.
export interface ArticleDetail extends ArticleCard {
  originalTitle: string;
  url: string;
  related: ArticleCard[];
}

// One page of the feed. nextCursor is null on the last page.
export interface ArticlePage {
  items: ArticleCard[];
  nextCursor: string | null;
}

export interface Category {
  slug: string;
  name: string;
}
