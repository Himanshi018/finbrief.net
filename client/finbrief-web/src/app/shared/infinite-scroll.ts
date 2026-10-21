import { DestroyRef, Directive, ElementRef, afterNextRender, inject, output } from '@angular/core';

// Put this on an element at the end of a list: <div appInfiniteScroll (reached)="loadMore()"></div>
// It emits "reached" when the element comes near the screen, using the browser's IntersectionObserver.
@Directive({ selector: '[appInfiniteScroll]' })
export class InfiniteScroll {
  readonly reached = output<void>();

  constructor() {
    const element = inject(ElementRef<HTMLElement>);
    const destroyRef = inject(DestroyRef);

    // afterNextRender only runs in the browser, never during server-side rendering.
    afterNextRender(() => {
      const observer = new IntersectionObserver(
        (entries) => {
          if (entries.some((entry) => entry.isIntersecting)) this.reached.emit();
        },
        { rootMargin: '600px' }, // start loading a bit before the user reaches the end
      );
      observer.observe(element.nativeElement);
      destroyRef.onDestroy(() => observer.disconnect());
    });
  }
}
