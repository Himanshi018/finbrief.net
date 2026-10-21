import { Component, input } from '@angular/core';

// The highlighted "Why it matters" block: the heart of every FinBrief story.
// Uses the brand teal so it reads as part of FinBrief, not as a warning.
@Component({
  selector: 'app-why-it-matters',
  template: `
    <section class="rounded-md border-l-4 border-teal-600 bg-teal-50 px-4 py-3" aria-label="Why it matters">
      <h3 class="flex items-center gap-1.5 text-xs font-bold tracking-wide text-teal-700 uppercase">
        <svg viewBox="0 0 20 20" fill="currentColor" class="size-4" aria-hidden="true">
          <path d="M10 2a6 6 0 0 0-3.6 10.8c.4.3.6.8.6 1.2v.5h6V14c0-.4.2-.9.6-1.2A6 6 0 0 0 10 2Zm-3 14.5V17a1 1 0 0 0 1 1h4a1 1 0 0 0 1-1v-.5H7Z" />
        </svg>
        Why it matters
      </h3>
      <p class="mt-1.5 leading-relaxed text-slate-800" [class]="large() ? 'text-base' : 'text-[15px]'">{{ text() }}</p>
    </section>
  `,
})
export class WhyItMatters {
  readonly text = input.required<string>();
  readonly large = input(false);
}
