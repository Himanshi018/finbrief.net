import { Routes } from '@angular/router';
import { ArticlePage } from './pages/article-page';
import { FeedPage } from './pages/feed-page';
import { LandingPage } from './pages/landing-page';
import { NotFoundPage } from './pages/not-found-page';
import { SavedPage } from './pages/saved-page';

// Which page component shows for which URL. "title" sets the browser tab title.
export const routes: Routes = [
  { path: '', component: LandingPage, title: 'FinBrief: finance news explained simply' },
  { path: 'feed', component: FeedPage, title: 'Feed | FinBrief' },
  { path: 'articles/:id', component: ArticlePage, title: 'FinBrief' },
  { path: 'saved', component: SavedPage, title: 'Saved | FinBrief' },
  { path: '**', component: NotFoundPage, title: 'Page not found | FinBrief' },
];
