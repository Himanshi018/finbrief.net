// FinBrief.Ingest is a console app that GitHub Actions runs once a day.
// Usage (real logic arrives in phases 4 and 6):
//   dotnet run --project src/FinBrief.Ingest -- ingest [--dry-run] [--max 20]
//   dotnet run --project src/FinBrief.Ingest -- backfill-ai [--dry-run] [--max 30] [--images]
//   dotnet run --project src/FinBrief.Ingest -- cleanup [--dry-run]

var command = args.Length > 0 ? args[0] : "";

switch (command)
{
    case "ingest":
    case "backfill-ai":
    case "cleanup":
        Console.WriteLine($"'{command}' is not implemented yet.");
        return 0;
    default:
        Console.WriteLine("Usage: FinBrief.Ingest <ingest|backfill-ai|cleanup> [--dry-run] [--max N] [--images]");
        return 1;
}
