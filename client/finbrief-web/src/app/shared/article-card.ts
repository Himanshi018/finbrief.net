import { Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { categoryName } from '../core/category-names';
import { ArticleCard as Article } from '../core/models';
import { TimeAgoPipe } from '../core/time-ago';
import { KeyTermChips } from './key-term-chips';
import { SaveButton } from './save-button';
import { WhyItMatters } from './why-it-matters';

// One story in the feed, Inshorts-style: image, category + time, simple title, summary,
// the highlighted "Why it matters" block, key-term chips, "Read full brief" and a save button.
@Component({
  selector: 'app-article-card',
  imports: [RouterLink, TimeAgoPipe, KeyTermChips, SaveButton, WhyItMatters],
  template: `
    <!-- One design for both card types. With a photo: a short 2:1 image on top.
         Without: a thin teal accent bar instead (no empty image space) and a bigger, bolder headline. -->
    <article class="overflow-hidden rounded-md bg-white shadow-sm ring-1 ring-slate-200">
      @if (showImage()) {
        <img
          [src]="article().imageUrl"
          alt=""
          loading="lazy"
          referrerpolicy="no-referrer"
          class="aspect-[2/1] w-full bg-slate-100 object-cover"
          (error)="imageFailed.set(true)"
        />
      } @else {
        <div class="h-1 bg-teal-600" aria-hidden="true"></div>
      }

      <div class="space-y-3 p-4">
        <p class="flex flex-wrap items-center gap-x-2 text-xs text-slate-500">
          <span class="font-semibold tracking-wide text-teal-700 uppercase">{{ category() }}</span>
          <span aria-hidden="true">·</span>
          <time [attr.datetime]="article().publishedAt">{{ article().publishedAt | timeAgo }}</time>
          <span aria-hidden="true">·</span>
          <span>{{ article().sourceName }}</span>
        </p>

        <h2 class="leading-snug text-slate-900"
            [class]="showImage() ? 'text-lg font-bold' : 'text-xl font-extrabold tracking-tight'">
          @if (example()) {
            {{ article().simpleTitle }}
          } @else {
            <a [routerLink]="['/articles', article().id]" class="hover:text-teal-800">{{ article().simpleTitle }}</a>
          }
        </h2>

        <p class="text-[15px] leading-relaxed text-slate-700">{{ article().summary }}</p>

        <app-why-it-matters [text]="article().whyItMatters" />

        @if (article().keyTerms.length) {
          <app-key-term-chips [terms]="article().keyTerms" />
        }

        @if (!example()) {
          <div class="flex items-center justify-between border-t border-slate-100 pt-3">
            <a [routerLink]="['/articles', article().id]"
               class="text-sm font-semibold text-teal-700 hover:text-teal-900">
              Read full brief →
            </a>
            <app-save-button [article]="article()" />
          </div>
        }
      </div>
    </article>
  `,
})
export class ArticleCard {
  readonly article = input.required<Article>();
  // The landing page shows a sample card that has no real article behind it.
  readonly example = input(false);

  protected readonly category = computed(() => categoryName(this.article().categorySlug));
  // An image that fails to load is removed, so the card never shows a broken-image icon or an empty box.
  protected readonly imageFailed = signal(false);
  protected readonly showImage = computed(() => !!this.article().imageUrl && !this.imageFailed());
}
