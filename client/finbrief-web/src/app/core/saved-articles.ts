import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { ArticleCard } from './models';

const STORAGE_KEY = 'finbrief.saved';

// Saved articles live in the browser's localStorage until login exists.
// We store the whole card, so the Saved page works without calling the API.
@Injectable({ providedIn: 'root' })
export class SavedArticles {
  // On the server (SSR) there is no localStorage, so the list starts empty there.
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  private readonly saved = signal<ArticleCard[]>(this.read());

  readonly articles = this.saved.asReadonly();
  readonly count = computed(() => this.saved().length);
  private readonly ids = computed(() => new Set(this.saved().map((a) => a.id)));

  isSaved(id: number): boolean {
    return this.ids().has(id);
  }

  toggle(article: ArticleCard): void {
    this.saved.update((list) =>
      this.isSaved(article.id) ? list.filter((a) => a.id !== article.id) : [article, ...list],
    );
    this.write();
  }

  private read(): ArticleCard[] {
    if (!this.isBrowser) return [];
    try {
      const json = localStorage.getItem(STORAGE_KEY);
      return json ? (JSON.parse(json) as ArticleCard[]) : [];
    } catch {
      return []; // storage blocked or corrupted: start empty
    }
  }

  private write(): void {
    if (!this.isBrowser) return;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.saved()));
    } catch {
      // storage full or blocked (e.g. private mode): keep the list in memory only
    }
  }
}
