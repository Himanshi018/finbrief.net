import { Injectable, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { ArticlesApi } from './articles-api';
import { ArticleCard } from './models';

const PAGE_SIZE = 10;

// Holds the feed's articles. It is a root service (one per app), so when you open an article and press Back,
// the feed is still loaded and the router can restore your scroll position.
@Injectable({ providedIn: 'root' })
export class FeedStore {
  private readonly api = inject(ArticlesApi);

  readonly items = signal<ArticleCard[]>([]);
  readonly status = signal<'idle' | 'loading' | 'ready' | 'error'>('idle');
  readonly loadingMore = signal(false);
  readonly loadMoreFailed = signal(false);
  readonly hasMore = signal(false);

  private category: string | null = null;
  private search: string | null = null;
  private nextCursor: string | null = null;
  private request?: Subscription;

  // Loads the first page for this category + search. Does nothing if it is already loaded.
  load(category: string | null, search: string | null): void {
    const sameQuery = category === this.category && search === this.search;
    if (sameQuery && (this.status() === 'ready' || this.status() === 'loading')) return;

    this.category = category;
    this.search = search;
    this.nextCursor = null;
    this.items.set([]);
    this.hasMore.set(false);
    this.loadingMore.set(false);
    this.loadMoreFailed.set(false);
    this.status.set('loading');

    this.request?.unsubscribe(); // cancel an older, slower request
    this.request = this.api.getArticles({ category, search, limit: PAGE_SIZE }).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.nextCursor = page.nextCursor;
        this.hasMore.set(page.nextCursor !== null);
        this.status.set('ready');
      },
      error: () => this.status.set('error'),
    });
  }

  // Loads the next page and appends it (infinite scroll).
  loadMore(): void {
    if (this.status() !== 'ready' || this.loadingMore() || !this.nextCursor) return;

    this.loadingMore.set(true);
    this.loadMoreFailed.set(false);
    this.request = this.api
      .getArticles({ category: this.category, search: this.search, cursor: this.nextCursor, limit: PAGE_SIZE })
      .subscribe({
        next: (page) => {
          this.items.update((items) => [...items, ...page.items]);
          this.nextCursor = page.nextCursor;
          this.hasMore.set(page.nextCursor !== null);
          this.loadingMore.set(false);
        },
        error: () => {
          this.loadingMore.set(false);
          this.loadMoreFailed.set(true);
        },
      });
  }

  retry(): void {
    const { category, search } = this;
    this.status.set('idle');
    this.load(category, search);
  }
}
