import { HttpErrorResponse, httpResource } from '@angular/common/http';
import { Component, RESPONSE_INIT, computed, effect, inject, input, signal } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { ArticlesApi } from '../core/articles-api';
import { categoryName } from '../core/category-names';
import { ArticleCard, ArticleDetail } from '../core/models';
import { TimeAgoPipe } from '../core/time-ago';
import { CardSkeleton, EmptyState, ErrorState } from '../shared/page-states';
import { SaveButton } from '../shared/save-button';
import { WhyItMatters } from '../shared/why-it-matters';

// /articles/:id: the full brief, the link to the original article, and related stories.
@Component({
  selector: 'app-article-page',
  imports: [RouterLink, TimeAgoPipe, CardSkeleton, EmptyState, ErrorState, SaveButton, WhyItMatters],
  template: `
    <a routerLink="/feed" class="mb-4 inline-block text-sm font-medium text-teal-700 hover:text-teal-900">← Back to feed</a>

    @if (article.hasValue()) {
      @let a = article.value();
      <article class="overflow-hidden rounded-md bg-white shadow-sm ring-1 ring-slate-200">
        @if (a.imageUrl && !failedImages().has(a.imageUrl)) {
          <img [src]="a.imageUrl" alt="" referrerpolicy="no-referrer" class="aspect-[2/1] w-full bg-slate-100 object-cover"
               (error)="hideImage(a.imageUrl)" />
        } @else {
          <div class="h-1 bg-teal-600" aria-hidden="true"></div>
        }

        <div class="space-y-5 p-5">
          <div class="space-y-2">
            <p class="flex flex-wrap items-center gap-x-2 text-xs text-slate-500">
              <span class="font-semibold tracking-wide text-teal-700 uppercase">{{ category() }}</span>
              <span aria-hidden="true">·</span>
              <time [attr.datetime]="a.publishedAt">{{ a.publishedAt | timeAgo }}</time>
              <span aria-hidden="true">·</span>
              <span>{{ a.sourceName }}</span>
            </p>
            <h1 class="text-2xl leading-tight font-bold text-slate-900">{{ a.simpleTitle }}</h1>
          </div>

          <p class="text-base leading-relaxed text-slate-700">{{ a.summary }}</p>

          <app-why-it-matters [text]="a.whyItMatters" [large]="true" />

          @if (a.keyTerms.length) {
            <section>
              <h2 class="text-sm font-bold tracking-wide text-slate-900 uppercase">Terms explained</h2>
              <dl class="mt-2 divide-y divide-slate-100 rounded-md bg-teal-50/60 px-4">
                @for (term of a.keyTerms; track term.term) {
                  <div class="py-3">
                    <dt class="font-semibold text-teal-900">{{ term.term }}</dt>
                    <dd class="mt-0.5 text-sm text-slate-700">{{ term.meaning }}</dd>
                  </div>
                }
              </dl>
            </section>
          }

          <section class="rounded-md bg-slate-50 p-4">
            <h2 class="text-xs font-semibold tracking-wide text-slate-500 uppercase">Original headline</h2>
            <p class="mt-1 text-sm text-slate-800">{{ a.originalTitle }}</p>
            <a [href]="a.url" target="_blank" rel="noopener noreferrer"
               class="mt-3 inline-flex items-center gap-1 rounded-full bg-teal-700 px-4 py-2 text-sm font-semibold text-white hover:bg-teal-800">
              Read full article on {{ a.sourceName }}
              <svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true">
                <path d="M11 3h6v6h-2V6.4l-6.3 6.3-1.4-1.4L13.6 5H11V3ZM5 5h4v2H5v8h8v-4h2v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z" />
              </svg>
            </a>
          </section>

          <div class="flex justify-end">
            <app-save-button [article]="card()!" />
          </div>
        </div>
      </article>

      @if (a.related.length) {
        <section class="mt-8">
          <h2 class="mb-3 text-base font-bold text-slate-900">More in {{ category() }}</h2>
          <ul class="space-y-3">
            @for (r of a.related; track r.id) {
              <li>
                <a [routerLink]="['/articles', r.id]"
                   class="flex gap-3 rounded-md bg-white p-3 shadow-sm ring-1 ring-slate-200 hover:ring-teal-600">
                  @if (r.imageUrl && !failedImages().has(r.imageUrl)) {
                    <img [src]="r.imageUrl" alt="" loading="lazy" referrerpolicy="no-referrer"
                         class="size-16 shrink-0 rounded-md bg-slate-100 object-cover" (error)="hideImage(r.imageUrl)" />
                  }
                  <div class="min-w-0">
                    <p class="text-sm leading-snug font-semibold text-slate-900">{{ r.simpleTitle }}</p>
                    <p class="mt-1 text-xs text-slate-500">{{ r.publishedAt | timeAgo }} · {{ r.sourceName }}</p>
                  </div>
                </a>
              </li>
            }
          </ul>
        </section>
      }
    } @else if (notFound()) {
      <app-empty-state title="Story not found" message="It may have been removed, or the link is wrong.">
        <a routerLink="/feed" class="mt-4 inline-block text-sm font-semibold text-teal-700">Go to the feed →</a>
      </app-empty-state>
    } @else if (article.error()) {
      <app-error-state message="We could not load this story." (retry)="article.reload()" />
    } @else {
      <app-card-skeleton [count]="1" />
    }
  `,
})
export class ArticlePage {
  // Filled from the :id in the URL (withComponentInputBinding in app.config.ts).
  readonly id = input.required<string>();

  private readonly api = inject(ArticlesApi);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  // Only exists during server-side rendering; lets us send a real 404 status to search engines.
  private readonly responseInit = inject(RESPONSE_INIT, { optional: true });

  // Re-runs automatically when id() changes (e.g. clicking a related article).
  protected readonly article = httpResource<ArticleDetail>(() => this.api.articleUrl(this.id()));

  // Image URLs that failed to load. They are hidden instead of showing a broken-image icon.
  protected readonly failedImages = signal(new Set<string>());

  protected hideImage(url: string): void {
    this.failedImages.update((failed) => new Set(failed).add(url));
  }

  protected readonly notFound = computed(
    () => (this.article.error() as HttpErrorResponse | undefined)?.status === 404,
  );
  protected readonly category = computed(() =>
    this.article.hasValue() ? categoryName(this.article.value().categorySlug) : '',
  );
  // The save button stores the card fields only.
  protected readonly card = computed<ArticleCard | undefined>(() => {
    if (!this.article.hasValue()) return undefined;
    const { originalTitle, url, related, ...card } = this.article.value();
    return card;
  });

  constructor() {
    // Page title and description for search engines and link previews.
    effect(() => {
      if (this.article.hasValue()) {
        const a = this.article.value();
        this.title.setTitle(`${a.simpleTitle} | FinBrief`);
        this.meta.updateTag({ name: 'description', content: a.summary });
      } else if (this.notFound()) {
        this.title.setTitle('Story not found | FinBrief');
        if (this.responseInit) this.responseInit.status = 404;
      }
    });
  }
}
