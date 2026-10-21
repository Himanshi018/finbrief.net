// FinBrief.Ingest is a console app that GitHub Actions runs once a day.
// Usage:
//   dotnet run --project src/FinBrief.Ingest -- ingest [--dry-run] [--max 6]
//   dotnet run --project src/FinBrief.Ingest -- backfill-ai [--dry-run] [--max 30] [--images]   (phase 6)
//   dotnet run --project src/FinBrief.Ingest -- cleanup [--dry-run]                             (phase 6)

using FinBrief.Infrastructure;
using FinBrief.Infrastructure.Ingestion;
using FinBrief.Ingest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const int DefaultIngestMax = 6;   // small on purpose: one article per category, few AI calls

var command = args.FirstOrDefault() ?? "";
var dryRun = args.Contains("--dry-run");
if (!TryReadMax(args, out var max))
{
    Console.Error.WriteLine("--max needs a whole number between 1 and 100, e.g. --max 6");
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

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddIngestion(ingestionSettings, groqSettings);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

switch (command)
{
    case "ingest":
        // A real run without AI would only save hidden articles, so it needs the key.
        // A dry run works without it and just lists what was selected.
        if (!dryRun && string.IsNullOrWhiteSpace(groqSettings.ApiKey))
        {
            Console.Error.WriteLine("Missing Groq API key 'Groq:ApiKey'. Set it with dotnet user-secrets "
                                    + "or the environment variable Groq__ApiKey. (A --dry-run works without it.)");
            return 1;
        }

        var ingestion = scope.ServiceProvider.GetRequiredService<IngestionService>();
        var report = await ingestion.RunAsync(dryRun, max ?? DefaultIngestMax);
        ReportPrinter.Print(report);

        // A non-zero exit code makes the GitHub Actions run show as failed, so problems get noticed.
        var aiFailed = report.AiStopReason is not null && !(dryRun && string.IsNullOrWhiteSpace(groqSettings.ApiKey));
        return report.FeedsRead == 0 || aiFailed ? 1 : 0;

    case "backfill-ai":
    case "cleanup":
        Console.WriteLine($"'{command}' is not implemented yet (phase 6).");
        return 0;

    default:
        Console.WriteLine("Usage: FinBrief.Ingest <ingest|backfill-ai|cleanup> [--dry-run] [--max N] [--images]");
        return 1;
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
