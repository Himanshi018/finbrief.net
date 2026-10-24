# FinBrief – Project Spec and Handover

FinBrief is a mobile-first finance news web app, like "Inshorts for finance".
Tagline: "Finance news explained simply."

This file is the complete handover. Someone with no chat history should be able to
continue from it alone. More detail on every part is in README.md (setup, deploying,
troubleshooting) and docs/LEARNING.md (beginner notes per phase).

Never write real secrets in this file, in code or in commits. Only setting names.

## HOW TO WORK WITH ME

- I'm a beginner in .NET and Angular. Explain simply, one step at a time, and
  briefly say what each step does.
- Wait for me after each step or phase before moving on.
- Keep the code simple and readable. Avoid unnecessary patterns (no MediatR, no
  CQRS, no generic repositories).
- After each change, update the PROGRESS section in CLAUDE.md and docs/LEARNING.md.
- After each phase, tell me exactly what to run to verify it.

## PRODUCT

Every story shows:
- A plain-language headline (no clickbait)
- A short summary (under ~45 words, complete sentences)
- "Why it matters": what the news means for the reader's money, job or the economy
- Key terms: 3–5 jargon words, each explained in one line

Audience: students, young professionals, early investors and startup enthusiasts
in India. India-first coverage.

Categories (slug → name, seeded in the database): markets → Markets,
economy → Economy, startups → Startups, fintech → Fintech,
ai-business → AI Business, global-finance → Global Finance.

It is NOT a stock tips / buy-sell signals app, NOT a general news aggregator,
NOT a finance terminal. Guiding question for every feature: "Does this help
users understand finance better?" Never give stock tips or market predictions,
not in the app and not in replies to users.

Legal rules: store only the title, a short description (≤280 chars), source,
link, date and image URL. Never store full article text. Always link back to
the original article. (Enforced: RssReader cuts descriptions to 280 chars and the
column is varchar(280).)

## TECH STACK

- Frontend: Angular 22 (standalone components, signals, zoneless, SSR with
  per-route render modes) + Tailwind v4. Tests with Vitest.
- Backend: ASP.NET Core Web API on .NET 10 (controllers), Scalar API page in development
- Database: PostgreSQL on Supabase, EF Core 10 + Npgsql, code-first migrations,
  snake_case names (EFCore.NamingConventions). dotnet-ef 10.0.13 pinned in
  dotnet-tools.json.
- News: 12 verified RSS feeds from Economic Times and Livemint, listed in
  src/FinBrief.Ingest/appsettings.json (Ingestion:Feeds). Livemint has no
  startups feed (404); startups come from ET startups + funding.
- AI: Groq free tier via the OpenAI .NET SDK (custom endpoint
  https://api.groq.com/openai/v1), model openai/gpt-oss-120b, reasoning effort
  low, MaxOutputTokens 2048.
- Scheduling: GitHub Actions daily workflow running the ingestion console app
- Hosting (planned): API on Render (Docker), Angular SSR on Vercel
- Secrets: dotnet user-secrets in dev, environment variables in prod. Never
  hardcode or commit secrets.

## STRUCTURE

```
FinBrief.sln
dotnet-tools.json            – pins dotnet-ef
Dockerfile, .dockerignore    – API container (port 8080, or $PORT)
src/FinBrief.Core            – entities and DTOs (no dependencies, no interfaces yet)
src/FinBrief.Infrastructure  – Data/ (AppDbContext, ArticleQueries, Migrations),
                               Ai/ (AiPrompt, ArticleAiClient, AiResponseParser),
                               Ingestion/ (RssReader, NewsFilter, ImageFetcher,
                               IngestionService, BackfillService, CleanupService,
                               settings), DependencyInjection.cs
src/FinBrief.Api             – Program.cs, ArticlesController, CategoriesController
src/FinBrief.Ingest          – console app: ingest / backfill-ai / cleanup,
                               appsettings.json (feeds, Groq, Cleanup settings)
client/finbrief-web          – Angular app (src/app/core, pages, shared),
                               vercel.json, api/ssr.mjs, scripts/build.mjs
.github/workflows/ingest.yml – daily ingest job
docs/LEARNING.md             – beginner learning notes per phase
```

Database tables: categories, sources, articles (key_terms stored as one jsonb
column), article_rejections, users, saved_articles (users and saved_articles are
ready for login but unused). Migrations: InitialCreate, EnableRowLevelSecurity.
Any new table needs its own `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` in a migration.

## API

- GET /api/articles?category=&search=&cursor=&limit= → only articles with all
  AI fields filled, newest first, cursor pagination for infinite scroll
  (limit default 10, max 30; cursor = "<publishedAt ticks>_<id>", response has
  nextCursor; search is ILIKE on simple title, summary and original title)
- GET /api/articles/{id} → full brief + 4 related articles from the same category
- GET /api/categories → tabs in display order
- GET /health → {"status":"ok"}
- No ingest endpoint: ingestion only runs through the FinBrief.Ingest console app
  (GitHub Actions)
- CORS from Cors:AllowedOrigins (appsettings.json has http://localhost:4200;
  production uses Cors__AllowedOrigins__0, which REPLACES item 0; trailing "/"
  is trimmed). Scalar at /scalar and OpenAPI at /openapi/v1.json only in Development.
- If the PORT env var is set (Render: 10000), the API listens on it.

## INGESTION PIPELINE

1. Read RSS feeds (System.ServiceModel.Syndication)
2. Skip items older than 72 hours (Ingestion:MaxAgeHours)
3. Keyword filter (NewsFilter.cs): keep items with finance signals; block
   stock-tip clickbait patterns ("Prediction:", "could soar", "History Says",
   "multibagger", "stocks to buy", "target price", etc.)
4. Skip excluded stock-tip domains (Motley Fool, Zacks, Seeking Alpha,
   InvestorPlace, Simply Wall St, MarketBeat, Insider Monkey), URLs already
   stored, and URLs in article_rejections
5. Round-robin selection across categories so every category gets articles.
   Market-moving geopolitics (oil, sanctions, tariffs, shipping routes; checked
   on the title) goes to global-finance. Real estate counts as finance.
6. ONE AI call per article returning JSON:
   `{is_finance_relevant, reason, simple_title, summary, why_it_matters, key_terms}`
   - Prompt in src/FinBrief.Infrastructure/Ai/AiPrompt.cs
   - gpt-oss is a reasoning model: reasoning effort low, MaxOutputTokens 2048
   - Tolerant JSON parsing (strip code fences, strip labels like "**Why It Matters:**")
   - Calls spaced by Groq:MinSecondsBetweenCalls (8 s). Groq free tier = 8,000
     tokens/min and 1,000 requests/day; one article ≈ 830 tokens.
7. Relevant → articles. Not relevant → article_rejections (never checked again).
8. Fail fast: on quota, auth or model errors, print one clear message and stop AI
   calls for the run (exit code 1). If the AI step fails for an article, save it
   with empty AI fields (hidden from the feed) so backfill can fill it later.
9. Images: RSS media/enclosure tags, else the article page's og:image (AngleSharp).
   If none, leave null. No placeholder, illustrated or AI-generated images.
   Per-source switch Ingestion:Sources:<name>:UseImages (Livemint = false).
   og:image is skipped for ET /videoshow/ pages (text title cards).

Commands (defaults: ingest --max 6, backfill-ai --max 30; --max allowed 1–100):
```
dotnet run --project src/FinBrief.Ingest -- ingest [--dry-run] [--max 6]
dotnet run --project src/FinBrief.Ingest -- backfill-ai [--dry-run] [--max 30] [--images]
dotnet run --project src/FinBrief.Ingest -- cleanup [--dry-run]
```
Cleanup (Cleanup section in appsettings.json): deletes articles > 30 days unless
in saved_articles, unfinished (no AI text) > 7 days, rejections > 7 days.
Each run prints clear logs and a summary.

## FRONTEND (mobile-first)

- /  landing page with the promise and an example card (prerendered)
- /feed  infinite scroll, category tabs, search (server-rendered)
- /articles/:id  simple title, summary, Why it matters, Terms explained,
  original headline, "Read full article on {source}" link, related articles
  (server-rendered; sets page title and meta description)
- /saved  saved articles in localStorage (client-only render; login comes later)
- ** → 404 page
- Card: image, category + relative time, simple title, summary, highlighted
  "Why it matters" block (teal), key-term chips (tap to see meaning), "Read full
  brief", save button. Brand colour teal, corners rounded-md.
- Loading, empty and error states on every page (shared/page-states.ts)
- API base URL: src/environments/environment.development.ts
  (http://localhost:5132); production gets it from the API_BASE_URL env var at
  build time (scripts/build.mjs → ng build --define).
- Angular CLI 22 needs Node 22.22+ or 24.15+; package.json engines = 24.x.

## OTHER

- .gitignore for .NET + Angular, also ignoring .claude/, AGENTS.md and any file
  with secrets (.env*, appsettings.Production.json, secrets.json, keys).
  CLAUDE.md is committed.
- Beginner-friendly README with setup, deploying and troubleshooting
- Dockerfile for the API

## PROGRESS (as of 2026-10-24)

- Phases 1–6: done and committed.
  1. Scaffold: solution with Core / Infrastructure / Api / Ingest projects,
     everything builds, GET /health works.
  2. Database: EF Core entities and AppDbContext, InitialCreate migration
     applied to Supabase, 6 categories + 2 sources seeded, connection string in
     user-secrets.
  3. API: articles (cursor pagination, category filter, search), article
     detail with 4 related, categories; CORS for localhost:4200; Scalar at
     /scalar; EnableRowLevelSecurity migration on all tables incl.
     __EFMigrationsHistory.
  4. Ingestion: console app with 12 verified ET/Livemint feeds, keyword filters,
     round-robin selection, one Groq call per article, tolerant JSON parsing,
     fail-fast, og:image fallback, --dry-run. POST /api/ingest was removed.
  5. Angular 22 app: landing, feed (tabs, search, infinite scroll), article
     detail, saved (localStorage), 404; signals, zoneless, SSR render modes,
     Tailwind v4. Also: per-source UseImages (Livemint off), 8 s between Groq calls.
  6. backfill-ai and cleanup commands, GitHub Actions workflow, Dockerfile, README.
- GitHub Actions daily ingest: WORKING. Runs at 00:30 UTC = 06:00 IST:
  ingest --max 18, then backfill-ai --max 10, then cleanup. Can also be run by
  hand (Actions → Daily ingest → Run workflow, "max" input). Repository secrets:
  SUPABASE_CONNECTION_STRING and GROQ_API_KEY. The Supabase pooler host must be
  aws-0-ap-south-1.pooler.supabase.com (NOT aws-1).
- Deployment prep: done (code ready; Render + Vercel step-by-step in README
  "Deploying"). Program.cs listens on $PORT; vercel.json + api/ssr.mjs run SSR;
  scripts/build.mjs bakes in API_BASE_URL.
- Deployment: NOT done. The user will do Render and Vercel themselves later
  (see DEPLOYMENT CHECKLIST). Render: only signed in. Vercel: account exists,
  project not imported.
- Repo: github.com/Himanshi018/finbrief.net (branch main). It is PUBLIC. It was
  checked and contains no secrets; keep it that way (check every diff before
  committing).

## HOW TO RUN LOCALLY

Needs .NET 10 SDK, Node 22.22+ or 24.15+, a Supabase project and a Groq key.
Run from the repository folder.

Git Bash only: dotnet must be on PATH. Add this line to ~/.bashrc, then open a
new terminal:
```bash
export PATH="$PATH:/c/Program Files/dotnet:$HOME/.dotnet/tools"
```

1. Restore the EF tool: `dotnet tool restore`
2. Secrets (the API and Ingest share one UserSecretsId, so set them once on the API):
   ```
   dotnet user-secrets set "ConnectionStrings:Default" "<Npgsql connection string>" --project src/FinBrief.Api
   dotnet user-secrets set "Groq:ApiKey" "<Groq key>" --project src/FinBrief.Api
   dotnet user-secrets list --project src/FinBrief.Api
   ```
   Connection string format (session pooler, key=value, not a postgresql:// URL):
   `Host=aws-0-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require`
   In production the same settings are env vars: ConnectionStrings__Default, Groq__ApiKey.
3. Database: `dotnet ef database update --project src/FinBrief.Infrastructure --startup-project src/FinBrief.Api`
4. API (terminal 1): `dotnet run --project src/FinBrief.Api --launch-profile http`
   - http://localhost:5132/health → {"status":"ok"}
   - http://localhost:5132/scalar (Development only)
5. Angular (terminal 2): `cd client/finbrief-web`, `npm install` (first time),
   `npm start` → http://localhost:4200. Tests: `npx ng test --watch=false`.
6. Ingestion (always --dry-run first):
   ```
   dotnet run --project src/FinBrief.Ingest -- ingest --dry-run
   dotnet run --project src/FinBrief.Ingest -- ingest --max 18        # daily size (3 per category)
   dotnet run --project src/FinBrief.Ingest -- ingest --max 42        # fill every category with ≥5
   dotnet run --project src/FinBrief.Ingest -- backfill-ai --max 10 --images
   dotnet run --project src/FinBrief.Ingest -- cleanup --dry-run
   ```
   Stop the API before building the whole solution (Windows locks its DLLs).
7. Commit and push:
   ```
   git status
   git diff            # check there are no secrets
   git add <files>
   git commit -m "Short message"
   git push origin main
   ```

## DEPLOYMENT CHECKLIST (to do later, in order)

Full click-by-click steps are in README "Deploying".

1. Render web service: New → Web Service → repo finbrief.net, Language Docker,
   branch main, Root Directory empty, Region Singapore, Instance Type Free,
   Health Check Path /health.
   Env vars: ConnectionStrings__Default (Npgsql format, aws-0 pooler),
   ASPNETCORE_ENVIRONMENT=Production. Do not add PORT (Render sets it).
   Check https://<render-url>/health and /api/categories.
2. Vercel: Add New → Project → repo finbrief.net, Root Directory
   client/finbrief-web, Framework Preset Other (vercel.json sets build/output).
   Env vars: API_BASE_URL = Render URL with https:// and no trailing /;
   NG_ALLOWED_HOSTS = Vercel host name without https:// (e.g. finbrief.vercel.app).
   If the real Vercel domain differs, fix NG_ALLOWED_HOSTS and redeploy
   (otherwise every page is "Bad Request").
3. Back on Render: Cors__AllowedOrigins__0 = the Vercel URL with https://, then
   "Save, rebuild, and deploy".
4. Final test on the Vercel URL: feed loads, key-term chips open, article page
   works, saved page works, category tabs work, no CORS errors in the browser
   console (F12).
5. Known: Render's free tier sleeps after ~15 idle minutes, so the first load can
   take about a minute.
6. Afterwards: update PROGRESS here and add a section to docs/LEARNING.md.

## KNOWN ISSUES / TODO

- Text-only cards (no image) need a better design.
- Livemint images are off (Ingestion:Sources:Livemint:UseImages = false) because
  its thumbnails are AI graphics. Only real photos are allowed, never
  AI-generated or placeholder images.
- The AI sometimes picks non-jargon key terms (e.g. "oil price pressures").
  Tighten the key_terms rule in AiPrompt.cs so it only picks real finance terms
  (e.g. repo rate, IPO, FPI), and maybe reject phrases in AiResponseParser.
- Saved articles live only in localStorage, but cleanup only protects articles
  in the saved_articles table. After 30 days a saved story's detail page shows
  "Story not found" (the Saved list still shows the stored copy). Fix with login
  + server-side saves, or by keeping longer.
- No backend tests at all; the frontend has only app.spec.ts and time-ago.spec.ts.
  No CI build/test workflow on push (only the daily ingest).
- No robots.txt, sitemap or OpenGraph/Twitter tags yet (article page sets only
  title and meta description), so SEO and link previews are limited.
- No rate limiting on the public API; search uses ILIKE without a text index
  (fine at current size).
- Groq free tier limits (8,000 tokens/min, 1,000 requests/day) cap how many
  articles can be processed per day.
- GitHub pauses scheduled workflows in public repos after 60 days without repo
  activity; re-enable on the Actions tab. Supabase free projects pause after a
  period of inactivity (the daily job keeps it active).
- A Groq key was once pasted into a chat. Treat any pasted key as leaked:
  rotate it in the Groq console and update user-secrets and GROQ_API_KEY.
- users table exists but nothing uses it yet; FinBrief.Core has no interfaces.

## ROADMAP (after deploy)

Before public launch:
- Feedback link (Tally or Google Form)
- Analytics
- Better empty / loading / error states
- Login (Supabase Auth or ASP.NET Identity) with saved-article sync
- Daily Brief page (top 5 stories of the day)

After launch:
- Better related articles
- Share buttons + OpenGraph images
- Daily email newsletter
- Personalization (favourite categories)
- Product analytics

## LAUNCH PLAN

1. LinkedIn "building in public" post with 2–3 card screenshots
2. Deploy (Render + Vercel)
3. Private beta with LinkedIn commenters and 10–15 friends; watch 2 people use it
4. Public launch on LinkedIn, Peerlist Launchpad, r/SideProject, r/developersIndia
5. Daily "Why it matters" post
6. Product Hunt later

Rule: never give stock tips or market predictions, even in replies.

## LESSONS LEARNED / COMMON ERRORS

| Problem | Cause and fix |
|---|---|
| `dotnet: command not found` in Git Bash | dotnet not on PATH. Add `export PATH="$PATH:/c/Program Files/dotnet:$HOME/.dotnet/tools"` to ~/.bashrc, open a new terminal. |
| Browser 404 on /health, but curl works | Browser had a different URL (wrong port, cached page). Check the exact address or use a private window. |
| Npgsql rejects the Supabase connection string | Supabase shows a `postgresql://` URL; Npgsql needs `Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require`. |
| "tenant/user not found" | Wrong pooler host: aws-1 instead of aws-0. Use aws-0-ap-south-1.pooler.supabase.com. |
| Supabase tables shown as UNRESTRICTED | RLS off, so tables were open through Supabase's REST API. Fixed by the EnableRowLevelSecurity migration (our app connects as owner and is unaffected). |
| New migration was empty | RLS isn't part of the EF model; add SQL by hand with `migrationBuilder.Sql(...)`. |
| /scalar returns 404 | Only mapped in Development; run with `--launch-profile http`. |
| Build error "file is locked by FinBrief.Api" (MSB3021) | API still running. Stop it (Ctrl+C) or build only one project. |
| appsettings.json not found when running Ingest from repo root | Fixed with `ContentRootPath = AppContext.BaseDirectory` and CopyToOutputDirectory. |
| Keyword filter dropped real finance stories | Print blocked headlines; added money, income, saving, spend, subsid, energy. |
| Off-topic or missing feeds | Livemint startups is 404, Livemint technology is gadgets. Verify each feed returns 200 with recent items. |
| Groq "quota used up" on big runs | Back-to-back calls hit 8,000 tokens/min (HTTP 429). Fix: 8 s between calls, retries for short waits. |
| Livemint thumbnails were AI graphics | UseImages = false for Livemint, existing image_url set to NULL. |
| Text-heavy title card from og:image | ET /videoshow/ pages skipped for og:image. |
| Angular CLI 22 fails on old Node | Needs Node 22.22+ or 24.15+; upgrade Node. |
| `[class.text-[15px]]` doesn't work | Binding names can't contain `[ ]`; use `[class]="cond ? 'a' : 'text-[15px]'"`. |
| Port 4200 already in use | Another `ng serve` is running; use it, or `npm start -- --port 4300`. |
| `dotnet run` in CI switched to Development | Workflow uses `--no-launch-profile` and `DOTNET_ENVIRONMENT: Production`. |
| GitHub Actions can't reach Supabase direct connection | Use the session pooler host. |
| GitHub Actions step failed with exit code 134 | A wrong SUPABASE_CONNECTION_STRING secret (wrong format/host) crashed the app. Re-enter the secret in Npgsql key=value format with the aws-0 host. |
| Cors__AllowedOrigins__0 removed localhost | An env var list item replaces item 0. Use __1 to add another origin. |
| SSR returns HTTP 400 "Header host ... is not allowed" | Set NG_ALLOWED_HOSTS to the host name (localhost locally, Vercel host in production). |
| Trailing "/" breaks CORS | Browsers send origins without "/"; API and build.mjs now trim it. |
| Log says "Overriding HTTP_PORTS '8080'" on Render | Expected: PORT (10000) wins over the Dockerfile's 8080. |
| An API key was pasted into a chat | Treat it as leaked: rotate it and set the new one. |
