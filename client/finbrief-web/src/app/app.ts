import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SavedArticles } from './core/saved-articles';

// The app shell: a sticky header with the logo and navigation, then the current page.
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="sticky top-0 z-20 h-14 border-b border-slate-200 bg-white/95 backdrop-blur">
      <div class="mx-auto flex h-full max-w-xl items-center justify-between px-4">
        <a routerLink="/" class="text-xl font-extrabold tracking-tight text-slate-900">
          Fin<span class="text-teal-700">Brief</span>
        </a>
        <nav class="flex items-center gap-1 text-sm font-medium">
          <a routerLink="/feed" routerLinkActive="bg-teal-50 text-teal-800"
             class="rounded-full px-3 py-1.5 text-slate-600 hover:text-teal-800">Feed</a>
          <a routerLink="/saved" routerLinkActive="bg-teal-50 text-teal-800"
             class="flex items-center gap-1.5 rounded-full px-3 py-1.5 text-slate-600 hover:text-teal-800">
            Saved
            @if (saved.count()) {
              <span class="rounded-full bg-teal-700 px-1.5 text-xs leading-5 text-white">{{ saved.count() }}</span>
            }
          </a>
        </nav>
      </div>
    </header>

    <main class="mx-auto max-w-xl px-4 pt-4 pb-12">
      <router-outlet />
    </main>
  `,
})
export class App {
  protected readonly saved = inject(SavedArticles);
}
