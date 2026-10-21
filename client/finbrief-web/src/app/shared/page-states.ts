import { Component, computed, input, output } from '@angular/core';

// Small building blocks for the loading, empty and error states every page needs.

@Component({
  selector: 'app-card-skeleton',
  template: `
    @for (i of placeholders(); track i) {
      <div class="animate-pulse overflow-hidden rounded-md bg-white shadow-sm ring-1 ring-slate-200" aria-hidden="true">
        <div class="aspect-[2/1] bg-slate-200"></div>
        <div class="space-y-3 p-4">
          <div class="h-3 w-1/3 rounded bg-slate-200"></div>
          <div class="h-5 w-11/12 rounded bg-slate-200"></div>
          <div class="h-3 w-full rounded bg-slate-200"></div>
          <div class="h-3 w-5/6 rounded bg-slate-200"></div>
          <div class="h-16 rounded-md bg-teal-100"></div>
        </div>
      </div>
    }
    <span class="sr-only">Loading…</span>
  `,
  host: { class: 'flex flex-col gap-5', role: 'status' },
})
export class CardSkeleton {
  readonly count = input(3);
  protected readonly placeholders = computed(() => Array.from({ length: this.count() }, (_, i) => i));
}

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="rounded-md border border-dashed border-slate-300 bg-white px-6 py-10 text-center">
      <p class="text-base font-semibold text-slate-800">{{ title() }}</p>
      @if (message()) {
        <p class="mt-2 text-sm text-slate-600">{{ message() }}</p>
      }
      <ng-content />
    </div>
  `,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input<string>();
}

@Component({
  selector: 'app-error-state',
  template: `
    <div class="rounded-md border border-red-200 bg-red-50 px-6 py-8 text-center" role="alert">
      <p class="text-base font-semibold text-red-800">{{ title() }}</p>
      <p class="mt-2 text-sm text-red-700">{{ message() }}</p>
      <button
        type="button"
        class="mt-4 rounded-full bg-red-700 px-5 py-2 text-sm font-semibold text-white hover:bg-red-800"
        (click)="retry.emit()"
      >
        Try again
      </button>
    </div>
  `,
})
export class ErrorState {
  readonly title = input('Something went wrong');
  readonly message = input('We could not load the news. Check your connection and try again.');
  readonly retry = output<void>();
}
