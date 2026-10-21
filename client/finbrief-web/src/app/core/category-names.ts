// Display names for category slugs on cards.
// The category tabs load the real list from /api/categories; cards use this small map so they can render
// without waiting for that call (and so the landing page can be pre-rendered without the API).
const NAMES: Record<string, string> = {
  markets: 'Markets',
  economy: 'Economy',
  startups: 'Startups',
  fintech: 'Fintech',
  'ai-business': 'AI Business',
  'global-finance': 'Global Finance',
};

export function categoryName(slug: string): string {
  return NAMES[slug] ?? slug.replace(/-/g, ' ');
}
