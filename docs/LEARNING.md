# FinBrief – Learning Notes

A beginner-friendly diary of how FinBrief is built, one phase at a time.
Each phase covers: what was built and why, the files involved, the concepts, how a request flows,
the commands, the errors we hit, and interview questions.

> Phases 1 and 2 were done before these notes were started, so their commands are the
> standard ones that produce the files in the repo.

---

## Phase 1 – Solution scaffold

### What was built and why

An empty but working skeleton: one solution with four projects that reference each other correctly,
plus a `/health` endpoint so we can prove the API starts. Starting with structure first means every
later phase has an obvious place to put its code.

### Files

| File | What it does |
|---|---|
| `FinBrief.sln` | The "solution": a list of all projects, so `dotnet build FinBrief.sln` builds everything at once. |
| `src/FinBrief.Core/FinBrief.Core.csproj` | Class library for the core "things" of the app (entities, later DTOs). Depends on nothing. |
| `src/FinBrief.Infrastructure/FinBrief.Infrastructure.csproj` | Class library for talking to the outside world (database, later RSS and AI). References Core. |
| `src/FinBrief.Api/FinBrief.Api.csproj` | The ASP.NET Core Web API project. References Core and Infrastructure. |
| `src/FinBrief.Api/Program.cs` | The API's entry point. In phase 1 it only mapped `GET /health`. |
| `src/FinBrief.Api/Properties/launchSettings.json` | How `dotnet run` starts the API locally: the ports (`http://localhost:5132`) and `ASPNETCORE_ENVIRONMENT=Development`. |
| `src/FinBrief.Api/appsettings.json` / `appsettings.Development.json` | Settings such as log levels. The Development file only applies when running locally. |
| `src/FinBrief.Api/FinBrief.Api.http` | Ready-made requests you can send from VS Code / Visual Studio to test the API. |
| `src/FinBrief.Ingest/FinBrief.Ingest.csproj` + `Program.cs` | Console app the daily GitHub Action will run. Phase 1 only reads the command name (`ingest`, `backfill-ai`, `cleanup`) and prints "not implemented yet". |
| `.gitignore` | Tells git which files never to commit: build output (`bin/`, `obj/`), `node_modules/`, and anything that may hold secrets (`.env`, `*.pfx`…). |

### Concepts

**Solution and projects.** A project (`.csproj`) compiles into one program or library. A solution (`.sln`)
groups projects. We split the code into layers so each has one job:
`Api → Infrastructure → Core`. Core never references the others, so it stays simple and reusable.

**Project references.** `<ProjectReference Include="..\FinBrief.Core\FinBrief.Core.csproj" />` in
`FinBrief.Infrastructure.csproj` lets Infrastructure use classes from Core. The arrows only go one way.

**Minimal endpoint.** You can map a URL straight to a small function without a controller:
```csharp
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
```
Hosting platforms call `/health` to check that the app is alive.

**Top-level statements.** `Program.cs` has no `class Program` or `Main` method. C# generates them for you,
which keeps the entry point short.

### How a request flows

`GET http://localhost:5132/health`
1. Kestrel (ASP.NET Core's built-in web server) receives the HTTP request.
2. Routing matches the path `/health` to the lambda registered with `MapGet`.
3. The lambda returns `Results.Ok(...)`, which is serialized to JSON: `{"status":"ok"}` with status 200.

### Commands

```powershell
dotnet new sln -n FinBrief                         # create the empty solution file
dotnet new classlib -o src/FinBrief.Core           # create a class library project
dotnet new classlib -o src/FinBrief.Infrastructure
dotnet new webapi --use-controllers -o src/FinBrief.Api   # create a Web API project
dotnet new console -o src/FinBrief.Ingest          # create a console app
dotnet sln add src/FinBrief.Core src/FinBrief.Infrastructure src/FinBrief.Api src/FinBrief.Ingest   # add projects to the solution
dotnet add src/FinBrief.Infrastructure reference src/FinBrief.Core                                   # Infrastructure can use Core
dotnet add src/FinBrief.Api reference src/FinBrief.Core src/FinBrief.Infrastructure
dotnet add src/FinBrief.Ingest reference src/FinBrief.Core src/FinBrief.Infrastructure
dotnet build FinBrief.sln                          # compile everything
dotnet run --project src/FinBrief.Api              # start the API, then open http://localhost:5132/health
```

### Errors we hit

- **`dotnet: command not found` in Git Bash.** .NET was installed, but Git Bash didn't know where it was
  because the folder wasn't on its `PATH` (the list of folders the shell searches for commands).
  Fix: add both folders to `~/.bashrc`, then open a new terminal:
  ```bash
  export PATH="$PATH:/c/Program Files/dotnet:$HOME/.dotnet/tools"
  ```
  `/c/Program Files/dotnet` holds `dotnet` itself; `~/.dotnet/tools` holds globally installed .NET tools.
- **Browser showed 404 on `/health`, but `curl` worked.** The API was fine: the browser had opened a different
  URL (wrong port or an old cached page). If `curl http://localhost:5132/health` returns `{"status":"ok"}`,
  check the exact address in the browser bar, or try a private window.

### Interview questions

1. **Why split the app into Core, Infrastructure and Api projects?**
   Each layer has one job. Core holds the business shapes and depends on nothing; Infrastructure holds
   database/external code; Api only handles HTTP. That makes code easier to find, test and change.
2. **What is the difference between a solution and a project?**
   A project builds one assembly (DLL or EXE). A solution is just a container that groups projects so
   they can be built and opened together.
3. **What is Kestrel?**
   The cross-platform web server built into ASP.NET Core. It listens on a port and passes requests into
   the app's pipeline.
4. **Why have a `/health` endpoint?**
   Hosting platforms and monitors call it to check the app is running, without touching real data.

---

## Phase 2 – Database: entities, DbContext, migration, seed data

### What was built and why

The database layer. We described each table as a C# class (an *entity*), told EF Core the rules
(lengths, unique columns, relationships), and generated a *migration* that created the tables in
PostgreSQL on Supabase, along with the 6 categories and 2 news sources.
The connection string lives in user-secrets, never in a committed file.

### Files

| File | What it does |
|---|---|
| `src/FinBrief.Core/Entities/Article.cs` | One news story: publisher fields (title, ≤280-char description, source, URL, date, image) + AI fields (simple title, summary, why it matters, key terms). |
| `src/FinBrief.Core/Entities/KeyTerm.cs` | One jargon word + meaning. Stored as JSON inside the article row, not as a separate table. |
| `src/FinBrief.Core/Entities/Category.cs` | A category such as Markets. Its `Slug` (`ai-business`) is used in URLs. |
| `src/FinBrief.Core/Entities/Source.cs` | A publisher we link back to (Economic Times, Livemint). |
| `src/FinBrief.Core/Entities/ArticleRejection.cs` | A URL the AI said is not finance news, so we never pay to check it again. |
| `src/FinBrief.Core/Entities/User.cs`, `SavedArticle.cs` | Ready for a future login feature. |
| `src/FinBrief.Infrastructure/Data/AppDbContext.cs` | EF Core's gateway to the database: one `DbSet` per table, plus rules and seed data in `OnModelCreating`. |
| `src/FinBrief.Infrastructure/Data/Migrations/*_InitialCreate.cs` | Generated code that creates all tables, indexes and seed rows. |
| `src/FinBrief.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` | EF's record of what the model looks like now, used to work out the next migration. |
| `src/FinBrief.Infrastructure/DependencyInjection.cs` | `AddInfrastructure(connectionString)`: one method that registers the DbContext, used by both the API and (later) Ingest. |
| `src/FinBrief.Infrastructure/FinBrief.Infrastructure.csproj` | Added the Npgsql (PostgreSQL) provider and EFCore.NamingConventions packages. |
| `src/FinBrief.Api/FinBrief.Api.csproj` | Added `Microsoft.EntityFrameworkCore.Design` (needed by `dotnet ef`) and a `UserSecretsId`. |
| `src/FinBrief.Api/Program.cs` | Reads the connection string and calls `AddInfrastructure`. Stops with a clear error if it's missing. |
| `dotnet-tools.json` | Pins the `dotnet-ef` command-line tool to version 10.0.13 for this repo. |

### Concepts

**Entity.** A plain C# class that maps to a table; each property becomes a column.
`public string Url { get; set; } = "";` in `Article` becomes the `url` column.

**DbContext and DbSet.** The DbContext opens connections, runs queries and saves changes.
Each `DbSet` is a table you can query with LINQ:
`db.Categories.OrderBy(c => c.SortOrder)` becomes `SELECT ... FROM categories ORDER BY sort_order`.

**Fluent configuration.** Rules written in `OnModelCreating` instead of attributes on the entity, so Core
stays free of database details. Example: `b.HasIndex(a => a.Url).IsUnique();` means the same story can
never be stored twice.

**Code-first migrations.** You change C# classes, then `dotnet ef migrations add` compares them to the
snapshot and writes the SQL steps for you. `dotnet ef database update` runs any migrations the database
hasn't seen yet. Applied migrations are recorded in the `__EFMigrationsHistory` table.

**Seed data.** `HasData(...)` puts fixed rows into a migration, for example
`new Category { Id = 1, Slug = "markets", Name = "Markets", SortOrder = 1 }`.
Changing seed data later means adding a new migration.

**Dependency injection (DI).** Instead of writing `new AppDbContext(...)` everywhere, we register it once,
and ASP.NET Core hands a ready instance to any class that asks for it in its constructor.
`services.AddDbContext<AppDbContext>(...)` in `DependencyInjection.cs` is the registration.

**Snake-case naming.** `UseSnakeCaseNamingConvention()` turns `PublishedAt` into the column
`published_at`, which is the normal Postgres style and easier to type in Supabase's SQL editor.

**JSON column.** `b.OwnsMany(a => a.KeyTerms, kt => kt.ToJson("key_terms"))` stores the list of key terms
as one `jsonb` value, e.g. `[{"term":"repo rate","meaning":"..."}]`, instead of a separate table.

**User-secrets.** A per-user file outside the repo (in your Windows profile) for development secrets.
`builder.Configuration.GetConnectionString("Default")` reads it the same way it reads appsettings,
so the password never ends up in git. In production, an environment variable
(`ConnectionStrings__Default`) supplies the same value.

### How the app starts (no new endpoints this phase)

1. `WebApplication.CreateBuilder` loads configuration: appsettings.json, then appsettings.Development.json,
   then user-secrets (Development only), then environment variables. Later sources win.
2. `GetConnectionString("Default")` reads the Supabase connection string, or throws a clear error if missing.
3. `AddInfrastructure` registers `AppDbContext` with DI, configured for Npgsql + snake_case.
4. Whenever a class later asks for `AppDbContext`, DI creates one per HTTP request and disposes it afterwards.

### Commands

```powershell
dotnet add src/FinBrief.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL   # EF Core provider for PostgreSQL
dotnet add src/FinBrief.Infrastructure package EFCore.NamingConventions               # snake_case names
dotnet add src/FinBrief.Api package Microsoft.EntityFrameworkCore.Design              # needed by dotnet ef
dotnet new tool-manifest                       # create dotnet-tools.json
dotnet tool install dotnet-ef                  # install the EF command-line tool locally for this repo
dotnet user-secrets init --project src/FinBrief.Api                                     # adds UserSecretsId to the csproj
dotnet user-secrets set "ConnectionStrings:Default" "<supabase connection string>" --project src/FinBrief.Api
dotnet ef migrations add InitialCreate --project src/FinBrief.Infrastructure --startup-project src/FinBrief.Api --output-dir Data/Migrations
dotnet ef database update --project src/FinBrief.Infrastructure --startup-project src/FinBrief.Api   # create the tables in Supabase
```

`--project` is where the migrations live (Infrastructure). `--startup-project` is the app EF runs to read
configuration and the connection string (Api).

### Errors we hit

- **Npgsql rejected the Supabase connection string.** Supabase shows a URL like
  `postgresql://user:password@host:5432/postgres`, but Npgsql expects key=value pairs. Fix: convert it:
  ```
  Host=<host>;Port=5432;Database=postgres;Username=<user>;Password=<password>;SSL Mode=Require
  ```
- **"tenant/user not found".** The connection used the wrong Supabase pooler host (`aws-1-...` instead of
  `aws-0-...`). The pooler couldn't find our project on that host. Fix: copy the exact host from
  Supabase → Connect, and update the user-secret.
- **Supabase showed every table as UNRESTRICTED.** Row level security was off, so the tables were reachable
  through Supabase's public REST API. Fix: the `EnableRowLevelSecurity` migration in phase 3.

### Interview questions

1. **What is a migration and why use one instead of writing SQL by hand?**
   A versioned, generated script that moves the database schema from one state to the next. It is
   stored in git, so every environment gets exactly the same changes in the same order.
2. **What is dependency injection?**
   Classes ask for what they need in their constructor and a container provides it. This removes
   hard-coded `new` calls, makes lifetimes (per request, singleton) explicit, and makes testing easier.
3. **What is the lifetime of a DbContext in ASP.NET Core and why?**
   Scoped: one per HTTP request. A DbContext is not thread-safe and tracks changes, so it should be
   short-lived.
4. **Where do you keep secrets in development and production?**
   User-secrets locally, environment variables (or a secret store) in production. Never appsettings.json
   or git.
5. **Why store key terms as JSON instead of a separate table?**
   They are always read together with their article and never queried on their own, so one `jsonb`
   column is simpler and needs no join.

---

## Phase 3 – API endpoints, Scalar, CORS and row level security

### What was built and why

The read API the Angular app will use (articles feed, article detail, categories), a browser page
(Scalar) to try the endpoints, CORS so the Angular dev server may call the API, and a migration that
switches on row level security so Supabase's public REST API can't touch our tables.

The API is read-only. New articles are only added by the FinBrief.Ingest console app (phase 4), which
GitHub Actions runs on a schedule. (A secret-protected `POST /api/ingest` endpoint was planned but dropped,
since nothing would call it.)

### Files

| File | What it does |
|---|---|
| `src/FinBrief.Api/Controllers/ArticlesController.cs` | `GET /api/articles` (feed with category, search and cursor paging) and `GET /api/articles/{id}` (full brief + 4 related). |
| `src/FinBrief.Api/Controllers/CategoriesController.cs` | `GET /api/categories`, in display order. |
| `src/FinBrief.Core/Dtos/ArticleDtos.cs` | The JSON shapes the API returns: `ArticleCardDto`, `ArticleDetailDto`, `ArticlePageDto`, `CategoryDto`, `KeyTermDto`. |
| `src/FinBrief.Infrastructure/Data/ArticleQueries.cs` | Shared filters: `WhereBriefIsReady()` (hide articles without AI text) and `NewestFirst()`. |
| `src/FinBrief.Core/Entities/ArticleRejection.cs` | `Reason` is now cut to 500 characters so a long AI answer can't break the insert. |
| `src/FinBrief.Infrastructure/Data/AppDbContext.cs` | Uses `ArticleRejection.ReasonMaxLength` instead of a repeated `500`. |
| `src/FinBrief.Infrastructure/Data/Migrations/*_EnableRowLevelSecurity.cs` | Turns on RLS for all 6 tables and `__EFMigrationsHistory`. |
| `src/FinBrief.Api/Program.cs` | Adds OpenAPI + Scalar (Development only) and the CORS policy. |
| `src/FinBrief.Api/appsettings.json` | `Cors:AllowedOrigins` = `http://localhost:4200` (Angular dev server). |
| `src/FinBrief.Api/FinBrief.Api.csproj` | Adds the `Scalar.AspNetCore` package. |
| `src/FinBrief.Api/FinBrief.Api.http` | Sample requests for every endpoint. |
| `.gitignore` | No longer ignores `CLAUDE.md`, so the project spec is committed. |

### Concepts

**Controllers.** A class whose methods handle HTTP requests. `[Route("api/categories")]` on the class
plus `[HttpGet]` on a method means `GET /api/categories` runs `GetCategories()`. `[ApiController]` adds
helpful defaults such as automatic 400 responses for bad input.

**DTOs.** The exact shape sent to the browser, separate from the entity. `ArticleCardDto` leaves out
the publisher's `Description` and `CreatedAt`, so the database can change without breaking the app.

**Constructor injection in a controller.** `public class CategoriesController(AppDbContext db)` asks DI for
a DbContext. This uses a C# 12 *primary constructor*: `db` is available in every method.

**AsNoTracking.** `db.Articles.AsNoTracking()` tells EF we only read, so it skips change tracking. That's
faster and uses less memory for read-only endpoints.

**Cursor pagination.** Instead of "page 3", the client sends back a pointer to the last item it saw.
Our cursor is `"<PublishedAt ticks>_<Id>"`, and the next page is
`PublishedAt < cursorDate || (PublishedAt == cursorDate && Id < cursorId)`.
New articles arriving at the top don't cause duplicates or gaps, which matters for infinite scroll.
We fetch `limit + 1` rows; if the extra row exists, there is a next page.

**ILIKE search.** `EF.Functions.ILike(a.Summary!, "%rbi%")` becomes Postgres `ILIKE`, a case-insensitive
pattern match. We escape `%` and `_` from user input so they're searched literally.

**CORS.** Browsers block a page on `localhost:4200` from reading responses from `localhost:5132`
unless the API says it's allowed. `policy.WithOrigins(allowedOrigins)` adds the
`Access-Control-Allow-Origin: http://localhost:4200` header. CORS protects browsers only; it is not
authentication.

**OpenAPI and Scalar.** `AddOpenApi()` generates a JSON description of every endpoint at
`/openapi/v1.json`. `MapScalarApiReference()` renders it as an interactive page at `/scalar` where you
can send requests. Both are mapped only `if (app.Environment.IsDevelopment())`.

**Row level security (RLS).** A Postgres feature that filters which rows each database role may see.
Supabase auto-publishes every `public` table through a REST API using the `anon` role.
`ALTER TABLE public."articles" ENABLE ROW LEVEL SECURITY;` with no policies means that API sees nothing.
Our app logs in as the table owner, and owners skip RLS, so it keeps working.

**Raw SQL in a migration.** When EF has no C# method for something, `migrationBuilder.Sql("...")` runs
plain SQL. The RLS migration was generated empty (the model didn't change) and filled in by hand.

### How a request flows

`GET /api/articles?category=markets&limit=10`
1. Kestrel receives the request; the CORS middleware adds the allow-origin header if it came from the Angular app.
2. Routing matches `api/articles` + `GET` to `ArticlesController.GetArticles`.
3. DI creates the controller and passes in a new `AppDbContext` for this request.
4. Model binding fills the parameters from the query string: `category = "markets"`, `limit = 10`
   (clamped to 1–30).
5. The method builds a LINQ query step by step: `WhereBriefIsReady()` → category filter → optional search →
   optional cursor → `NewestFirst()` → `Take(limit + 1)`. Nothing runs yet.
6. `ToListAsync()` makes EF translate it all into one SQL `SELECT` and send it to Supabase.
7. The rows become `Article` objects, are mapped to `ArticleCardDto`s, and `nextCursor` is set if there is
   another page.
8. ASP.NET Core serializes the `ArticlePageDto` to JSON (camelCase): `{"items":[...],"nextCursor":"..."}`.
9. The request ends and DI disposes the DbContext.

### Commands

```powershell
dotnet add src/FinBrief.Api package Scalar.AspNetCore     # the Scalar API reference page
dotnet tool restore                                       # install dotnet-ef from dotnet-tools.json
dotnet ef migrations add EnableRowLevelSecurity --project src/FinBrief.Infrastructure --startup-project src/FinBrief.Api --output-dir Data/Migrations
dotnet ef migrations script InitialCreate EnableRowLevelSecurity --project src/FinBrief.Infrastructure --startup-project src/FinBrief.Api   # preview the SQL without running it
dotnet ef database update --project src/FinBrief.Infrastructure --startup-project src/FinBrief.Api   # apply the RLS migration
dotnet run --project src/FinBrief.Api --launch-profile http   # then open http://localhost:5132/scalar
```

### Errors and gotchas

- **Tables marked UNRESTRICTED in Supabase (from phase 2).** Fixed by this phase's RLS migration. After
  `dotnet ef database update`, the label disappears in Supabase's table editor.
- **The new migration was empty.** `dotnet ef migrations add EnableRowLevelSecurity` generated empty
  `Up`/`Down` methods. That's expected: EF only writes code for model changes, and RLS isn't part of the
  model. We added the SQL by hand with `migrationBuilder.Sql(...)`.
  (It also proved that the `Reason` truncation didn't change the database schema.)
- **`/scalar` returns 404.** Scalar is only mapped in Development. Run with `--launch-profile http`, which sets
  `ASPNETCORE_ENVIRONMENT=Development`.

### Interview questions

1. **Why cursor pagination instead of page numbers (offset)?**
   With offsets, new items at the top shift everything down, so users see duplicates; deep pages are also
   slower because the database skips rows. A cursor continues exactly after the last item seen.
2. **Why does the cursor include the Id and not just the date?**
   Two articles can share the same publish time. The Id breaks the tie, so the order is stable and no
   article is skipped.
3. **Is CORS a security feature for the API?**
   It protects users' browsers, not the API. Tools like curl ignore CORS completely, so endpoints that
   change data still need real authentication. (Ours are all read-only.)
4. **What does row level security do on Supabase and why do we need it?**
   It restricts rows per database role. Without it, anyone with the project's public anon key could read
   or change our tables through Supabase's REST API.
5. **Why return DTOs instead of entities?**
   They control exactly what is exposed, avoid leaking internal fields, avoid serialization loops from
   navigation properties, and let the database change without breaking clients.

---

## Phase 4 – RSS + AI ingestion console app

### What was built and why

The part that fills the database. `FinBrief.Ingest ingest` reads 12 Economic Times and Livemint RSS feeds,
throws away old, duplicate and stock-tip items, picks a fair mix across categories, asks the AI (Groq,
model `openai/gpt-oss-120b`) to explain each one simply, and saves the result.
`--dry-run` runs every step except saving, so you can check the output first.

We also removed the planned `POST /api/ingest` endpoint: GitHub Actions will run this console app
directly, so the API stays read-only and needs no secret.

### Files

| File | What it does |
|---|---|
| `src/FinBrief.Ingest/Program.cs` | Reads the command (`ingest`, flags `--dry-run`, `--max N`), loads configuration, registers services, runs the pipeline, prints the report, and returns exit code 1 on problems. |
| `src/FinBrief.Ingest/ReportPrinter.cs` | Prints every selected article (original title, simple title, summary, why it matters, key terms, image) and a summary of the counts. |
| `src/FinBrief.Ingest/appsettings.json` | The 12 feed URLs with their category, the 72-hour limit, Groq model settings, and log levels. No secrets. |
| `src/FinBrief.Ingest/Properties/launchSettings.json` | Sets `DOTNET_ENVIRONMENT=Development` for `dotnet run`, so user-secrets are loaded. |
| `src/FinBrief.Ingest/FinBrief.Ingest.csproj` | Same `UserSecretsId` as the API; copies `appsettings.json` to the output folder; adds `Microsoft.Extensions.Hosting`. |
| `src/FinBrief.Infrastructure/Ingestion/IngestionService.cs` | The pipeline: read feeds → filter → skip known URLs → round-robin pick → image → AI → save. |
| `src/FinBrief.Infrastructure/Ingestion/RssReader.cs` | Downloads a feed and turns each `<item>` into a clean `FeedItem` (HTML removed, description ≤ 280 chars, UTC date, image from `<enclosure>` or `<media:content>`). |
| `src/FinBrief.Infrastructure/Ingestion/NewsFilter.cs` | Keyword rules: finance signals, stock-tip clickbait patterns, excluded domains, and the "geopolitics → global-finance" rule. |
| `src/FinBrief.Infrastructure/Ingestion/ImageFetcher.cs` | If the RSS item has no image, reads the article page's `og:image` with AngleSharp. |
| `src/FinBrief.Infrastructure/Ingestion/FeedItem.cs` | One cleaned-up RSS story (only the fields we may legally store). |
| `src/FinBrief.Infrastructure/Ingestion/IngestReport.cs` | Counters and per-article outcomes of a run. |
| `src/FinBrief.Infrastructure/Ingestion/IngestionSettings.cs` | Classes that `appsettings.json` sections are loaded into (`IngestionSettings`, `FeedSettings`, `GroqSettings`). |
| `src/FinBrief.Infrastructure/Ai/ArticleAiClient.cs` | One Groq call per article through the OpenAI SDK; decides which errors stop the run and which only skip one article. |
| `src/FinBrief.Infrastructure/Ai/AiPrompt.cs` | The instructions sent to the AI. |
| `src/FinBrief.Infrastructure/Ai/AiResponseParser.cs` | Tolerant JSON parsing of the AI's reply. |
| `src/FinBrief.Infrastructure/DependencyInjection.cs` | New `AddIngestion(...)` that registers all of the above. |
| `src/FinBrief.Infrastructure/FinBrief.Infrastructure.csproj` | Adds `System.ServiceModel.Syndication`, `OpenAI`, `AngleSharp`, `Microsoft.Extensions.Http`. |

### Concepts

**Generic Host in a console app.** `Host.CreateApplicationBuilder(...)` gives a console app the same
configuration, logging and DI that ASP.NET Core has. We then ask DI for the pipeline:
`scope.ServiceProvider.GetRequiredService<IngestionService>()`.

**Configuration layers.** Later sources override earlier ones: `appsettings.json` → user-secrets
(Development only) → environment variables. The key `Groq:ApiKey` in user-secrets is the environment
variable `Groq__ApiKey` (double underscore = colon), which is how GitHub Actions will supply it.

**Shared user-secrets.** User-secrets are stored per `UserSecretsId`, not per project. Giving the Ingest
project the API's id (`0799f371-...`) means one `dotnet user-secrets set` works for both apps.

**Binding settings to classes.** `builder.Configuration.GetSection("Ingestion").Get<IngestionSettings>()`
turns the JSON section into a C# object, so the code uses `settings.MaxAgeHours` instead of strings.

**IHttpClientFactory (typed clients).** `services.AddHttpClient<RssReader>(...)` hands `RssReader` a ready
`HttpClient` with our User-Agent and a 20 s timeout, and reuses connections correctly. Creating
`new HttpClient()` everywhere can exhaust sockets.

**RSS with SyndicationFeed.** `SyndicationFeed.Load(reader)` parses RSS/Atom into objects, so we read
`item.Title.Text`, `item.PublishDate` and `item.Links` instead of raw XML. Livemint's image is a Media RSS
extension, read through `item.ElementExtensions`.

**Regular expressions with `[GeneratedRegex]`.** Patterns such as `could soar|multibagger|stocks? to (buy|watch|sell)`
are compiled at build time. `\b(invest)\w*` means "a word starting with invest", so it matches investors too.

**Round-robin selection.** Each category gets a queue of its newest candidates, and we take one from each
in turn until `--max` is reached. Categories with the fewest stored articles go first, so with `--max 3`
the small categories still get filled over several runs.

**Calling an OpenAI-compatible API.** Groq speaks the same protocol as OpenAI, so the official SDK works by
changing the address: `new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") }`.
gpt-oss is a reasoning model: `ReasoningEffortLevel = Low` keeps its thinking short, and
`MaxOutputTokenCount = 2048` leaves room for thinking + answer.

**Fail fast vs. retry.** A bad key (401), unknown model (404) or used-up quota (429 with a long wait) throws
`AiStopException`: one message, no more AI calls this run. A short 429 wait or a 5xx error is retried.
Anything else throws `AiArticleException`: that article is saved without AI text and `backfill-ai` retries it later.

**Tolerant parsing.** Models sometimes wrap JSON in code fences or add "Here is the JSON:".
`AiResponseParser` keeps only the part between the first `{` and the last `}`, accepts `simple_title` or
`simpleTitle`, and strips labels like `**Why It Matters:**`.

**Dry run.** A flag that runs the real code path but skips the writes:
`if (!dryRun && !await SaveAsync(...))`. It is the safe way to test against live data.

**Exit codes.** `return 1;` from `Program.cs` tells the caller something went wrong. GitHub Actions marks
the run as failed, so you notice.

### How an ingest run flows

`dotnet run --project src/FinBrief.Ingest -- ingest --dry-run`
1. `Program.cs` reads the command and flags, builds the host, and loads `appsettings.json` + user-secrets.
2. It checks the connection string (and, for real runs, the Groq key), then registers services with DI.
3. `IngestionService.RunAsync` loads the category slugs from the database.
4. **Read:** `RssReader` downloads each of the 12 feeds; a failing feed is logged and skipped.
5. **Filter:** drop duplicates, items older than 72 h, excluded domains, stock-tip titles, and items with no
   finance words. Titles about oil/sanctions/tariffs/shipping are moved to `global-finance`.
6. **Skip known URLs:** one query each against `articles` and `article_rejections`.
7. **Select:** round-robin across categories, up to `--max` (default 6).
8. For each selected item: find an image (RSS → og:image → none), call the AI once, then decide:
   relevant → article, not relevant → rejection, AI failed → article without AI text.
9. In a real run each result is saved immediately with `SaveChangesAsync`; in a dry run nothing is saved.
10. `ReportPrinter` prints the articles and the summary, and the program returns 0 or 1.

### Commands

```powershell
dotnet add src/FinBrief.Infrastructure package System.ServiceModel.Syndication   # RSS parsing
dotnet add src/FinBrief.Infrastructure package OpenAI                            # official OpenAI SDK (used with Groq)
dotnet add src/FinBrief.Infrastructure package AngleSharp                        # HTML parsing for og:image
dotnet add src/FinBrief.Infrastructure package Microsoft.Extensions.Http         # AddHttpClient
dotnet add src/FinBrief.Ingest package Microsoft.Extensions.Hosting              # host, config, DI, logging for the console app
dotnet user-secrets set "Groq:ApiKey" "<your key>" --project src/FinBrief.Api    # shared with Ingest (same UserSecretsId)
dotnet run --project src/FinBrief.Ingest -- ingest --dry-run                     # everything except saving
dotnet run --project src/FinBrief.Ingest -- ingest --max 3                       # real run, 3 articles
```

The `--` separates `dotnet run`'s own options from the arguments passed to our program.

### Errors and gotchas

- **Build failed with "file is locked by FinBrief.Api" (MSB3021).** The API was still running, and Windows
  doesn't let you overwrite a DLL that a running program uses. Stop the API (Ctrl+C) before building the
  whole solution, or build just one project: `dotnet build src/FinBrief.Ingest`.
- **Livemint has no startups feed.** `https://www.livemint.com/rss/startups` returns 404. Startups come from
  ET's startups and funding feeds instead. Always check a feed returns 200 with recent items before adding it.
- **Some feeds were mostly off-topic.** Livemint `technology` is mainly gadget deals and ET `news/international`
  is general world news, so we left them out and used ET `world/business` for global finance.
- **The keyword filter dropped real finance stories** (farmers' income, utility bills). We printed every
  blocked headline, found these, and added `money`, `income`, `saving`, `spend`, `subsid` and `energy` to
  the finance words. Check what a filter removes, not just what it keeps.
- **`appsettings.json` not found when running from the repo root.** A console app looks in the current folder by
  default. Prevented with `ContentRootPath = AppContext.BaseDirectory` and copying the file to the output
  folder (`CopyToOutputDirectory` in the csproj).
- **An API key was pasted into a chat.** Anything pasted into a chat, issue or commit should be treated as
  leaked: create a new key in the Groq console and set it again with `dotnet user-secrets set`.

### Interview questions

1. **Why use IHttpClientFactory instead of `new HttpClient()`?**
   It reuses and recycles connections, so you avoid socket exhaustion and stale DNS, and it keeps
   configuration (headers, timeouts) in one place.
2. **How does the app read secrets locally and in CI without code changes?**
   Configuration is layered. Locally, user-secrets supply `Groq:ApiKey`; in CI, the environment variable
   `Groq__ApiKey` supplies the same key, and environment variables override earlier sources.
3. **Why one AI call per article, and why stop on some errors?**
   One call returns the relevance decision and all text fields together, which keeps cost and rate limits
   low. Errors like a bad key or used-up quota would fail for every article, so continuing only wastes time.
4. **Why does the AI reply need tolerant parsing?**
   LLMs don't always follow the format exactly (code fences, extra text, different key names). Parsing
   tolerantly turns small format slips into usable data instead of failed articles.
5. **What is a dry run and why build one?**
   A mode that executes the real logic but skips writes. It lets you test against live feeds and the real
   AI safely, and see exactly what would be saved before it is.
