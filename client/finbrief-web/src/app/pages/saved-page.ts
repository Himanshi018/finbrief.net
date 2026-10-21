import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SavedArticles } from '../core/saved-articles';
import { ArticleCard } from '../shared/article-card';
import { EmptyState } from '../shared/page-states';

// /saved: articles saved in this browser (localStorage). Rendered only in the browser (see app.routes.server.ts).
@Component({
  selector: 'app-saved-page',
  imports: [RouterLink, ArticleCard, EmptyState],
  template: `
    <h1 class="mb-1 text-2xl font-bold text-slate-900">Saved</h1>
    <p class="mb-5 text-sm text-slate-500">Stored on this device only.</p>

    <div class="flex flex-col gap-5">
      @for (article of saved.articles(); track article.id) {
        <app-article-card [article]="article" />
      } @empty {
        <app-empty-state title="Nothing saved yet" message="Tap Save on any story to keep it here.">
          <a routerLink="/feed" class="mt-4 inline-block text-sm font-semibold text-teal-700">Browse the feed →</a>
        </app-empty-state>
      }
    </div>
  `,
})
export class SavedPage {
  protected readonly saved = inject(SavedArticles);
}
