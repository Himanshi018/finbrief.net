import { Component, computed, inject, input } from '@angular/core';
import { ArticleCard } from '../core/models';
import { SavedArticles } from '../core/saved-articles';

// Bookmark button that saves or un-saves an article in localStorage.
@Component({
  selector: 'app-save-button',
  template: `
    <button
      type="button"
      class="inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium transition-colors"
      [class]="saved() ? 'bg-teal-700 text-white hover:bg-teal-800' : 'text-slate-600 hover:bg-slate-100'"
      [attr.aria-pressed]="saved()"
      [attr.aria-label]="saved() ? 'Remove from saved' : 'Save article'"
      (click)="savedArticles.toggle(article())"
    >
      <svg viewBox="0 0 20 20" class="size-4" aria-hidden="true"
           [attr.fill]="saved() ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="1.6">
        <path d="M5 3h10a1 1 0 0 1 1 1v13l-6-3.5L4 17V4a1 1 0 0 1 1-1Z" stroke-linejoin="round" />
      </svg>
      {{ saved() ? 'Saved' : 'Save' }}
    </button>
  `,
})
export class SaveButton {
  readonly article = input.required<ArticleCard>();
  protected readonly savedArticles = inject(SavedArticles);
  protected readonly saved = computed(() => this.savedArticles.isSaved(this.article().id));
}
