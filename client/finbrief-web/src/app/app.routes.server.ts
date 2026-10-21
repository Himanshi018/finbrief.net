import { RenderMode, ServerRoute } from '@angular/ssr';

// How each URL is rendered:
// - Prerender: HTML made once at build time (the landing page never changes).
// - Server: HTML made on every request, with fresh news (good for SEO).
// - Client: rendered only in the browser (Saved reads localStorage, which the server doesn't have).
export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Prerender },
  { path: 'saved', renderMode: RenderMode.Client },
  { path: '**', renderMode: RenderMode.Server },
];
