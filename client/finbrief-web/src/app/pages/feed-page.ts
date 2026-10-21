import { httpResource } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { ArticlesApi } from '../core/articles-api';
import { FeedStore } from '../core/feed-store';
import { Category } from '../core/models';
import { ArticleCard } from '../shared/article-card';
import { InfiniteScroll } from '../shared/infinite-scroll';
import { CardSkeleton, EmptyState, ErrorState } from '../shared/page-states';

// /feed?category=markets&q=rbi
// The category and search live in the URL, so a filtered feed can be shared and is rendered on the server.
@Component({
  selector: 'app-feed-page',
  imports: [RouterLink, ArticleCard, InfiniteScroll, CardSkeleton, EmptyState, ErrorState],
  template: `
    <div class="sticky top-14 z-10 -mx-4 space-y-3 bg-slate-50/95 px-4 pt-3 pb-3 backdrop-blur">
      <form role="search" (submit)="$event.preventDefault(); searchNow(box.value)">
        <label for="search" class="sr-only">Search finance news</label>
        <input
          #box
          id="search"
          type="search"
          placeholder="Search: RBI, IPO, inflation…"
          [value]="search()"
          (input)="searchTyped.next(box.value)"
          class="w-full rounded-full border border-slate-300 bg-white px-4 py-2.5 text-[15px] shadow-sm placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-600/20 focus:outline-none"
        />
      </form>

      <nav aria-label="Categories" class="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 [scrollbar-width:none]">
        <a [routerLink]="[]" [queryParams]="{ category: null }" queryParamsHandling="merge"
           [class]="tabClass(activeCategory() === null)">All</a>
        @for (category of categories.hasValue() ? categories.value() : []; track category.slug) {
          <a [routerLink]="[]" [queryParams]="{ category: category.slug }" queryParamsHandling="merge"
             [class]="tabClass(activeCategory() === category.slug)">{{ category.name }}</a>
        }
      </nav>
    </div>

    <div class="mt-2 flex flex-col gap-5">
      @switch (store.status()) {
        @case ('error') {
          <app-error-state (retry)="store.retry()" />
        }
        @case ('ready') {
          @for (article of store.items(); track article.id) {
            <app-article-card [article]="article" />
          } @empty {
            @if (search() || activeCategory()) {
              <app-empty-state title="No stories match" message="Try another word or category." />
            } @else {
              <app-empty-state title="No briefs yet" message="New stories are added every morning. Check back soon." />
            }
          }

          @if (store.hasMore()) {
            <div appInfiniteScroll (reached)="store.loadMore()"></div>
            @if (store.loadingMore()) {
              <app-card-skeleton [count]="1" />
            } @else if (store.loadMoreFailed()) {
              <app-error-state message="Could not load more stories." (retry)="store.loadMore()" />
            } @else {
              <button type="button" (click)="store.loadMore()"
                      class="mx-auto rounded-full border border-slate-300 bg-white px-5 py-2 text-sm font-semibold text-slate-700 hover:border-teal-600 hover:text-teal-700">
                Load more
              </button>
            }
          } @else if (store.items().length) {
            <p class="py-6 text-center text-sm text-slate-500">You're all caught up ✓</p>
          }
        }
        @default {
          <app-card-skeleton />
        }
      }
    </div>
  `,
})
export class FeedPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ArticlesApi);
  protected readonly store = inject(FeedStore);

  // httpResource loads data and exposes it as signals: value(), isLoading(), error().
  protected readonly categories = httpResource<Category[]>(() => this.api.categoriesUrl());

  private readonly params = toSignal(this.route.queryParamMap, { requireSync: true });
  protected readonly activeCategory = computed(() => this.params().get('category'));
  protected readonly search = computed(() => this.params().get('q') ?? '');

  // Every keystroke goes into this stream; we only search after 400 ms without typing.
  protected readonly searchTyped = new Subject<string>();

  constructor() {
    // Reload the feed whenever the category or search in the URL changes.
    this.route.queryParamMap
      .pipe(takeUntilDestroyed())
      .subscribe((params) => this.store.load(params.get('category'), params.get('q')?.trim() || null));

    this.searchTyped
      .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((text) => this.searchNow(text));
  }

  protected searchNow(text: string): void {
    this.router.navigate([], {
      queryParams: { q: text.trim() || null },
      queryParamsHandling: 'merge',
      replaceUrl: true, // don't add a history entry per search
    });
  }

  protected tabClass(active: boolean): string {
    const base = 'shrink-0 rounded-full px-4 py-1.5 text-sm font-medium whitespace-nowrap transition-colors';
    return active
      ? `${base} bg-teal-700 text-white`
      : `${base} bg-white text-slate-700 ring-1 ring-slate-200 hover:text-teal-700`;
  }
}
