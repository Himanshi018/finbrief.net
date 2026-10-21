import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ArticleCard as Article } from '../core/models';
import { ArticleCard } from '../shared/article-card';

// "/": the promise of FinBrief and a sample card. Pre-rendered at build time (see app.routes.server.ts).
@Component({
  selector: 'app-landing-page',
  imports: [RouterLink, ArticleCard],
  template: `
    <section class="pt-6 pb-8 text-center">
      <p class="text-xs font-semibold tracking-widest text-teal-700 uppercase">India-first finance news</p>
      <h1 class="mt-3 text-4xl leading-tight font-extrabold tracking-tight text-slate-900">
        Finance news,<br />explained simply.
      </h1>
      <p class="mx-auto mt-4 max-w-md text-base leading-relaxed text-slate-600">
        The day's markets, economy, startup and fintech stories in under 60 words each, with what they mean
        for your money, your job and India's economy.
      </p>
      <a routerLink="/feed"
         class="mt-6 inline-block rounded-full bg-teal-700 px-7 py-3 text-base font-semibold text-white shadow-sm hover:bg-teal-800">
        Start reading →
      </a>
    </section>

    <section aria-label="What every story gives you" class="grid grid-cols-2 gap-3 pb-8">
      @for (point of promises; track point.title) {
        <div class="rounded-md bg-white p-4 shadow-sm ring-1 ring-slate-200">
          <p class="text-sm font-bold text-slate-900">{{ point.title }}</p>
          <p class="mt-1 text-xs leading-relaxed text-slate-600">{{ point.text }}</p>
        </div>
      }
    </section>

    <section aria-label="Example story" class="pb-8">
      <p class="mb-3 text-center text-xs font-semibold tracking-widest text-slate-500 uppercase">This is what a brief looks like</p>
      <app-article-card [article]="example" [example]="true" />
    </section>

    <p class="pb-6 text-center text-xs leading-relaxed text-slate-500">
      FinBrief explains the news. It does not give stock tips or investment advice.<br />
      Every story links to the original publisher.
    </p>
  `,
})
export class LandingPage {
  protected readonly promises = [
    { title: 'Plain headlines', text: 'No clickbait, no jargon. Just what happened.' },
    { title: 'Short summaries', text: 'Under 45 words, in complete sentences.' },
    { title: 'Why it matters', text: 'What the news means for your money, job or the economy.' },
    { title: 'Terms explained', text: 'Tap any jargon word to see a one-line meaning.' },
  ];

  // An illustrative example, not a real news story.
  protected readonly example: Article = {
    id: 0,
    simpleTitle: 'RBI keeps its main lending rate unchanged at 5.5%',
    summary:
      'The Reserve Bank of India left the repo rate at 5.5% for now. It said prices are rising slowly and the economy is growing steadily, so it wants to wait before making loans cheaper.',
    whyItMatters:
      'Your home, car and personal loan EMIs are likely to stay the same for now. Fixed deposit rates are also unlikely to change soon.',
    keyTerms: [
      { term: 'Repo rate', meaning: 'The interest rate at which the RBI lends money to banks.' },
      { term: 'EMI', meaning: 'Equated Monthly Instalment: the fixed amount you pay each month on a loan.' },
      { term: 'Inflation', meaning: 'How fast prices of everyday things are rising.' },
    ],
    categorySlug: 'economy',
    sourceName: 'Example',
    imageUrl: null,
    publishedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
  };
}
