import { Component, RESPONSE_INIT, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmptyState } from '../shared/page-states';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink, EmptyState],
  template: `
    <app-empty-state title="Page not found" message="The page you're looking for doesn't exist.">
      <a routerLink="/feed" class="mt-4 inline-block text-sm font-semibold text-teal-700">Go to the feed →</a>
    </app-empty-state>
  `,
})
export class NotFoundPage {
  constructor() {
    // During server-side rendering, answer with HTTP 404 instead of 200.
    const responseInit = inject(RESPONSE_INIT, { optional: true });
    if (responseInit) responseInit.status = 404;
  }
}
