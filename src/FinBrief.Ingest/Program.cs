// FinBrief.Ingest is a console app that GitHub Actions runs once a day (.github/workflows/ingest.yml).
// Usage:
//   dotnet run --project src/FinBrief.Ingest -- ingest [--dry-run] [--max 6]
//   dotnet run --project src/FinBrief.Ingest -- backfill-ai [--dry-run] [--max 30] [--images]
//   dotnet run --project src/FinBrief.Ingest -- cleanup [--dry-run]

using FinBrief.Infrastructure;
using FinBrief.Infrastructure.Ingestion;
using FinBrief.Ingest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const int DefaultIngestMax = 6;     // small on purpose: one article per category, few AI calls
const int DefaultBackfillMax = 30;
const string Usage = "Usage: FinBrief.Ingest <ingest|backfill-ai|cleanup> [--dry-run] [--max N] [--images]";

var command = args.FirstOrDefault() ?? "";
var dryRun = args.Contains("--dry-run");
var fillImages = args.Contains("--images");
if (!TryReadMax(args, out var max))
{
    Console.Error.WriteLine("--max needs a whole number between 1 and 100, e.g. --max 6");
    return 1;
}

if (command is not ("ingest" or "backfill-ai" or "cleanup"))
{
    Console.WriteLine(Usage);
    return 1;
}

// The host builder loads configuration in this order (later wins):
//   appsettings.json → appsettings.{Environment}.json → user-secrets (Development only) → environment variables.
// Locally, launchSettings.json sets DOTNET_ENVIRONMENT=Development, so the API's user-secrets are used.
// In GitHub Actions, environment variables such as ConnectionStrings__Default and Groq__ApiKey are used.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    // Look for appsettings.json next to the program, not in the folder you ran the command from.
    ContentRootPath = AppContext.BaseDirectory,
});

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing connection string 'ConnectionStrings:Default'. Set it with dotnet user-secrets (see README) "
                            + "or the environment variable ConnectionStrings__Default.");
    return 1;
}

var ingestionSettings = builder.Configuration.GetSection("Ingestion").Get<IngestionSettings>() ?? new IngestionSettings();
var groqSettings = builder.Configuration.GetSection("Groq").Get<GroqSettings>() ?? new GroqSettings();
var cleanupSettings = builder.Configuration.GetSection("Cleanup").Get<CleanupSettings>() ?? new CleanupSettings();
var hasGroqKey = !string.IsNullOrWhiteSpace(groqSettings.ApiKey);

// Real ingest/backfill runs need the AI. Dry runs work without it and just list what they would do.
if (command is "ingest" or "backfill-ai" && !dryRun && !hasGroqKey)
{
    Console.Error.WriteLine("Missing Groq API key 'Groq:ApiKey'. Set it with dotnet user-secrets "
                            + "or the environment variable Groq__ApiKey. (A --dry-run works without it.)");
    return 1;
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddIngestion(ingestionSettings, groqSettings, cleanupSettings);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

// A non-zero exit code makes the GitHub Actions run show as failed, so problems get noticed.
// A missing key in a dry run is expected, so it doesn't count as a failure.
bool AiFailed(string? stopReason) => stopReason is not null && hasGroqKey;

switch (command)
{
    case "ingest":
    {
        var service = scope.ServiceProvider.GetRequiredService<IngestionService>();
        var report = await service.RunAsync(dryRun, max ?? DefaultIngestMax);
        ReportPrinter.Print(report);
        return report.FeedsRead == 0 || AiFailed(report.AiStopReason) ? 1 : 0;
    }

    case "backfill-ai":
    {
        var service = scope.ServiceProvider.GetRequiredService<BackfillService>();
        var report = await service.RunAsync(dryRun, max ?? DefaultBackfillMax, fillImages);
        ReportPrinter.Print(report);
        return AiFailed(report.AiStopReason) ? 1 : 0;
    }

    default: // "cleanup"
    {
        var service = scope.ServiceProvider.GetRequiredService<CleanupService>();
        var report = await service.RunAsync(dryRun);
        ReportPrinter.Print(report, cleanupSettings);
        return 0;
    }
}

// Reads "--max 20". Returns false only when --max is present but not a valid number.
static bool TryReadMax(string[] args, out int? max)
{
    max = null;
    var index = Array.IndexOf(args, "--max");
    if (index < 0)
    {
        return true;
    }

    if (index + 1 < args.Length && int.TryParse(args[index + 1], out var value) && value is >= 1 and <= 100)
    {
        max = value;
        return true;
    }

    return false;
}
