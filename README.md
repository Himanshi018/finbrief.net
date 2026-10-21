# FinBrief

**Finance news explained simply.** A mobile-first finance news app for India, like "Inshorts for finance".

Every story shows:
- a plain-language headline (no clickbait)
- a short summary (under ~45 words)
- **Why it matters**: what the news means for your money, your job or the economy
- 3–5 key terms, each explained in one line

FinBrief explains the news. It does not give stock tips or investment advice, never stores full articles,
and always links back to the original publisher.

---

## How it works

```
  Economic Times / Livemint RSS
              │
              ▼
  FinBrief.Ingest (console app, daily via GitHub Actions)
    read feeds → filter → pick per category → Groq AI explains each story
              │
              ▼
  PostgreSQL on Supabase
              │
              ▼
  FinBrief.Api (ASP.NET Core Web API)  ──►  finbrief-web (Angular, server-side rendered)
```

| Folder | What's inside |
|---|---|
| `src/FinBrief.Core` | Entities and DTOs (no dependencies) |
| `src/FinBrief.Infrastructure` | Database (EF Core), RSS reader, AI client, ingestion/backfill/cleanup services |
| `src/FinBrief.Api` | Web API: `/api/articles`, `/api/articles/{id}`, `/api/categories`, `/health` |
| `src/FinBrief.Ingest` | Console app: `ingest`, `backfill-ai`, `cleanup` |
| `client/finbrief-web` | Angular app: landing, feed, article, saved |
| `.github/workflows/ingest.yml` | Daily ingestion job |
| `docs/LEARNING.md` | Step-by-step notes on how and why everything was built |

---

## What you need

| Tool | Version | Check with |
|---|---|---|
| .NET SDK | 10 | `dotnet --version` |
| Node.js | 22.22+ or 24.15+ | `node -v` |
| A Supabase project | free tier is fine | [supabase.com](https://supabase.com) |
| A Groq API key | free tier is fine | [console.groq.com/keys](https://console.groq.com/keys) |

---

## Setup (first time)

Run these from the repository folder.

### 1. Install the EF Core command-line tool

```powershell
dotnet tool restore
```
This installs `dotnet-ef` (the version pinned in `dotnet-tools.json`) for this repository.

### 2. Get your Supabase connection string

In Supabase: **Connect** → **Session pooler**. Supabase shows a URL like
`postgresql://postgres.abcd:PASSWORD@aws-0-ap-south-1.pooler.supabase.com:5432/postgres`.

.NET's PostgreSQL driver needs it as key=value pairs instead:

```
Host=aws-0-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.abcd;Password=YOUR_PASSWORD;SSL Mode=Require
```

Copy the host exactly as Supabase shows it (`aws-0` vs `aws-1` matters).
Use the **pooler**, not the "direct connection": GitHub Actions can't reach the direct one.

### 3. Store your secrets (never in appsettings.json)

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require" --project src/FinBrief.Api
dotnet user-secrets set "Groq:ApiKey" "gsk_..." --project src/FinBrief.Api
```
User-secrets are stored in your Windows/Mac user profile, outside the repository, so they are never committed.
The API and the Ingest app share the same secrets.

### 4. Create the database tables

```powershell
dotnet ef database update --project src/FinBrief.Infrastructure --startup-project src/FinBrief.Api
```
This creates the tables, adds the 6 categories and 2 sources, and turns on row level security.

### 5. Fetch the first articles

```powershell
dotnet run --project src/FinBrief.Ingest -- ingest --dry-run   # preview: nothing is saved
dotnet run --project src/FinBrief.Ingest -- ingest --max 42    # about 7 per category, takes ~6 minutes
```

### 6. Install the frontend packages

```powershell
cd client/finbrief-web
npm install
```

---

## Running it

Use two terminals.

**Terminal 1: the API** (from the repository folder)
```powershell
dotnet run --project src/FinBrief.Api --launch-profile http
```
- Health check: http://localhost:5132/health
- Try the API in the browser: http://localhost:5132/scalar

**Terminal 2: the website**
```powershell
cd client/finbrief-web
npm start
```
Open http://localhost:4200. Use your browser's phone view (F12 → device toolbar) to see the mobile layout.

---

## Ingest commands

| Command | What it does |
|---|---|
| `ingest [--dry-run] [--max 6]` | Reads the feeds, picks up to `--max` new stories spread over all categories, asks the AI to explain each, saves them. |
| `backfill-ai [--dry-run] [--max 30] [--images]` | Retries articles whose AI step failed (they are hidden from the feed until then). `--images` also looks for missing images. |
| `cleanup [--dry-run]` | Deletes articles older than 30 days (unless saved), unfinished articles older than 7 days, and rejected URLs older than 7 days. |

Run them with `dotnet run --project src/FinBrief.Ingest -- <command>`. Always try `--dry-run` first.

Settings are in `src/FinBrief.Ingest/appsettings.json`:
- **`Ingestion:Feeds`**: the RSS feeds and which category each belongs to.
- **`Ingestion:Sources`**: per publisher, `"UseImages": false` turns images off (Livemint is off because its thumbnails are mostly AI-generated graphics).
- **`Groq`**: the model, and `MinSecondsBetweenCalls` (8 s keeps you under Groq's free-tier limit of 8,000 tokens per minute).
- **`Cleanup`**: how many days to keep things.

---

## Daily updates with GitHub Actions

`.github/workflows/ingest.yml` runs every day at 06:00 India time (00:30 UTC). It runs `ingest --max 18`,
then `backfill-ai --max 10`, then `cleanup`.

### Add these two repository secrets

On GitHub: your repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.

| Name | Value |
|---|---|
| `SUPABASE_CONNECTION_STRING` | The key=value connection string from setup step 2 (session pooler, port 5432) |
| `GROQ_API_KEY` | Your Groq key (`gsk_...`) |

Names must match exactly. GitHub hides secret values in logs.

### Run it once by hand to test

**Actions** tab → **Daily ingest** → **Run workflow**. You can change "Max new articles" there.
A red ✗ means a step failed; open it to see the message (for example a wrong connection string or a used-up
Groq quota).

> Scheduled workflows only run on the default branch (`main`). In public repositories, GitHub pauses them
> after 60 days without repository activity; re-enable them on the Actions tab if that happens.

---

## Running the API with Docker

```powershell
docker build -t finbrief-api .
docker run -p 8080:8080 `
  -e ConnectionStrings__Default="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require" `
  -e Cors__AllowedOrigins__0="https://your-frontend.example.com" `
  finbrief-api
```
Then open http://localhost:8080/health.

- Settings are passed as environment variables. A double underscore (`__`) stands for `:` in .NET settings.
- Scalar (`/scalar`) is only available in Development, not in the container.
- When the API is deployed, put its URL in `client/finbrief-web/src/environments/environment.ts`.

---

## Running the tests

```powershell
cd client/finbrief-web
npx ng test --watch=false
```

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `Missing connection string 'ConnectionStrings:Default'` | Do setup step 3. In GitHub Actions, check the `SUPABASE_CONNECTION_STRING` secret. |
| Connection error / "tenant/user not found" | The host is wrong. Copy it again from Supabase → Connect → Session pooler, in key=value format. |
| Build error "file is locked by FinBrief.Api" | The API is still running. Stop it (Ctrl+C) and build again. |
| `Groq rejected the API key` | Check `Groq:ApiKey` (or the `GROQ_API_KEY` secret). |
| `Groq kept rate-limiting us` | Use a smaller `--max` or a larger `Groq:MinSecondsBetweenCalls`. |
| The feed is empty | Run `ingest`. Only articles with all AI fields filled are shown. |
| The website can't reach the API | Is the API running on port 5132? Is `http://localhost:4200` in `Cors:AllowedOrigins`? |
| `dotnet: command not found` in Git Bash | Add `export PATH="$PATH:/c/Program Files/dotnet:$HOME/.dotnet/tools"` to `~/.bashrc`. |

More background on every part is in [docs/LEARNING.md](docs/LEARNING.md).
