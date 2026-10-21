import { Component, input, signal } from '@angular/core';
import { KeyTerm } from '../core/models';

// Jargon words as chips. Tap a chip to see its one-line meaning; tap again to hide it.
@Component({
  selector: 'app-key-term-chips',
  template: `
    <div class="flex flex-wrap gap-2">
      @for (term of terms(); track term.term; let i = $index) {
        <button
          type="button"
          class="rounded-full border px-3 py-1 text-xs font-medium transition-colors"
          [class]="
            selected() === i
              ? 'border-teal-700 bg-teal-700 text-white'
              : 'border-slate-300 bg-white text-slate-700 hover:border-teal-600 hover:text-teal-700'
          "
          [attr.aria-expanded]="selected() === i"
          (click)="toggle(i)"
        >
          {{ term.term }}
        </button>
      }
    </div>

    @if (selected() !== null) {
      @let term = terms()[selected()!];
      <p class="mt-2 rounded-md bg-teal-50 px-3 py-2 text-sm text-teal-950" aria-live="polite">
        <span class="font-semibold">{{ term.term }}:</span> {{ term.meaning }}
      </p>
    }
  `,
})
export class KeyTermChips {
  readonly terms = input.required<KeyTerm[]>();
  protected readonly selected = signal<number | null>(null);

  protected toggle(index: number): void {
    this.selected.update((current) => (current === index ? null : index));
  }
}
