# FinBrief – Project Spec

FinBrief is a mobile-first finance news web app, like "Inshorts for finance".
Tagline: "Finance news explained simply."

I'm a beginner in .NET and Angular. Explain briefly what each step does, keep the
code simple and readable, and avoid unnecessary patterns (no MediatR, no CQRS,
no generic repositories).

## PRODUCT

Every story shows:
- A plain-language headline (no clickbait)
- A short summary (under ~45 words, complete sentences)
- "Why it matters": what the news means for the reader's money, job or the economy
- Key terms: 3–5 jargon words, each explained in one line

Audience: students, young professionals, early investors and startup enthusiasts
in India. India-first coverage.

Categories: Markets, Economy, Startups, Fintech, AI Business, Global Finance

It is NOT a stock tips / buy-sell signals app, NOT a general news aggregator,
NOT a finance terminal. Guiding question for every feature: "Does this help
users understand finance better?"

Legal rules: store only the title, a short description (≤280 chars), source,
link, date and image URL. Never store full article text. Always link back to
the original article.

## TECH STACK

- Frontend: Angular (standalone components, signals, SSR enabled for SEO) + Tailwind
- Backend: ASP.NET Core Web API on .NET 10
- Database: PostgreSQL on Supabase, EF Core + Npgsql, code-first migrations
- News: RSS feeds from Economic Times and Livemint (markets, economy, startups,
  funding, banking, AI/tech, international, commodities). Find the official feed
  URLs, verify each returns recent items, and keep them in configuration.
- AI: Groq free tier via the OpenAI .NET SDK (custom endpoint), model
  openai/gpt-oss-120b
- Scheduling: GitHub Actions daily workflow running the ingestion console app
- Secrets: dotnet user-secrets in dev, environment variables in prod. Never
  hardcode or commit secrets.

## STRUCTURE

```
FinBrief.sln
src/FinBrief.Core            – entities, DTOs, interfaces (no dependencies)
src/FinBrief.Infrastructure  – DbContext, migrations, RSS reader, AI client,
                               image fetcher, ingestion service
src/FinBrief.Api             – Web API controllers
src/FinBrief.Ingest          – console app: ingest / backfill-ai / cleanup
client/finbrief-web          – Angular app
.github/workflows/ingest.yml
```

## API

- GET /api/articles?category=&search=&cursor=&limit= → only articles with all
  AI fields filled, newest first, cursor pagination for infinite scroll
- GET /api/articles/{id} → full brief + 4 related articles from the same category
- GET /api/categories
- No ingest endpoint: ingestion only runs through the FinBrief.Ingest console app
  (GitHub Actions)
- CORS for the Angular dev URL, Scalar API page in development, /health endpoint

## INGESTION PIPELINE

1. Read RSS feeds (System.ServiceModel.Syndication)
2. Skip items older than 72 hours
3. Keyword filter: keep items with finance signals; block stock-tip clickbait
   patterns ("Prediction:", "could soar", "History Says", "multibagger",
   "stocks to buy", etc.)
4. Skip excluded stock-tip domains (Motley Fool, Zacks, Seeking Alpha,
   InvestorPlace, Simply Wall St, MarketBeat, Insider Monkey), URLs already
   stored, and URLs in article_rejections
5. Round-robin selection across categories so every category gets articles.
   Market-moving geopolitics (oil, sanctions, tariffs, shipping routes) goes to
   global-finance. Real estate counts as finance.
6. ONE AI call per article returning JSON:
   `{is_finance_relevant, reason, simple_title, summary, why_it_matters, key_terms}`
   - gpt-oss is a reasoning model: reasoning effort low, enough max tokens
   - Tolerant JSON parsing (strip code fences, strip labels like "**Why It Matters:**")
7. Relevant → articles. Not relevant → article_rejections (never checked again).
8. Fail fast: on quota, auth or model errors, print one clear message and stop AI
   calls for the run. If the AI step fails for an article, save it with empty AI
   fields (hidden from the feed) so backfill can fill it later.
9. Images: RSS media/enclosure tags, else the article page's og:image (AngleSharp).
   If none, leave null. No placeholder or illustrated images.

Commands:
```
dotnet run --project src/FinBrief.Ingest -- ingest [--dry-run] [--max 20]
dotnet run --project src/FinBrief.Ingest -- backfill-ai [--dry-run] [--max 30] [--images]
dotnet run --project src/FinBrief.Ingest -- cleanup [--dry-run]
```
Default --max to a small number. Clear logs and a summary after each run.

## FRONTEND (mobile-first)

- /  landing page with the promise and an example card
- /feed  infinite scroll, category tabs, search
- /articles/:id  simple title, summary, Why it matters, Terms explained,
  original headline, "Read full article on {source}" link, related articles
- /saved  saved articles in localStorage (login comes later)
- Card: image, category + relative time, simple title, summary, highlighted
  "Why it matters" block, key-term chips (tap to see meaning), "Read full brief",
  save button
- Loading, empty and error states on every page
- API base URL in Angular environment files
- Note: Angular CLI 22 needs Node 22.22+ or 24.15+; upgrade Node before phase 5

## OTHER

- .gitignore for .NET + Angular, also ignoring .claude/, AGENTS.md and any file
  with secrets (CLAUDE.md is committed)
- Beginner-friendly README with setup steps
- Dockerfile for the API

## WORK IN PHASES

After each phase, tell me exactly what to run to verify it, wait for my
confirmation, and update the PROGRESS section in CLAUDE.md.

After every phase, also update docs/LEARNING.md with a new section for that
phase, in simple language for a beginner:
- What was built and why
- Every file created or changed, and what it does
- The concepts used, each explained in 2-4 lines with a small example from our code
- How a request flows through the app, step by step
- Commands run and what each one does
- Common errors we hit and how we fixed them
- 3-5 interview questions about the phase, with short answers

1. Solution and projects scaffolded, everything builds
2. EF Core entities, DbContext, first migration, seed data
3. API endpoints, testable in Swagger (+ RLS migration)
4. RSS + AI ingestion console app (test with --dry-run first)
5. Angular app: landing, feed, card, detail, saved
6. Backfill and cleanup commands, GitHub Actions workflow, Dockerfile, README

## PROGRESS

- Phase 1: done (solution scaffolded, builds, /health works)
- Phase 2: done (EF Core entities, InitialCreate migration applied to Supabase,
  6 categories + 2 sources seeded, connection string in user-secrets)
- Phase 3: done (articles/categories endpoints, cursor pagination, search, CORS
  for localhost:4200, Scalar at /scalar, EnableRowLevelSecurity migration applied
  to all tables incl. __EFMigrationsHistory)
- Phase 4: done (Ingest console app: 12 verified ET/Livemint feeds in
  src/FinBrief.Ingest/appsettings.json, keyword filters, round-robin selection,
  one Groq call per article, tolerant JSON parsing, fail-fast, og:image fallback,
  --dry-run). Ingest shares the API's UserSecretsId; secrets are
  ConnectionStrings:Default and Groq:ApiKey (env vars ConnectionStrings__Default,
  Groq__ApiKey). POST /api/ingest was removed: GitHub Actions runs the console app.
- Phase 5: done (Angular 22 app in client/finbrief-web: standalone components,
  signals, zoneless, SSR with render modes (landing prerendered, saved
  client-only, rest server-rendered), Tailwind v4. Pages: landing, feed
  (category tabs, search, infinite scroll), article detail, saved
  (localStorage), 404. API URL in src/environments. Brand colour teal; "Why it
  matters" is a teal block; corners rounded-md.)
  Ingestion tweaks made during phase 5:
  - Per-source "UseImages" in Ingestion:Sources (Livemint = false: its
    thumbnails are mostly AI graphics; existing Livemint image_url cleared).
    og:image fallback is skipped for ET /videoshow/ pages (text title cards).
  - Groq free tier = 8,000 tokens/min and 1,000 requests/day; one article ≈ 830
    tokens. Calls are spaced by Groq:MinSecondsBetweenCalls (8 s). To fill
    every category with ≥5 articles use --max 42; daily runs --max 18.
- Phase 6: done (backfill-ai [--dry-run] [--max 30] [--images] retries articles
  without AI text and fills missing images; cleanup [--dry-run] deletes
  articles > 30 days unless saved, unfinished > 7 days, rejections > 7 days
  (Cleanup section in appsettings.json); .github/workflows/ingest.yml runs
  daily at 00:30 UTC: ingest --max 18, backfill-ai --max 10, cleanup, with
  repository secrets SUPABASE_CONNECTION_STRING and GROQ_API_KEY; Dockerfile
  for the API (port 8080, settings via env vars); README.)
- Next: deploy the API and set its URL in
  client/finbrief-web/src/environments/environment.ts; deploy the frontend.

## TODO (later)

- Improve the text-only card design (cards without an image).
