using FinBrief.Infrastructure.Ingestion;

namespace FinBrief.Ingest;

// Prints the result of a run: every selected article, then a summary of the numbers.
public static class ReportPrinter
{
    public static void Print(IngestReport report)
    {
        Console.WriteLine();
        Console.WriteLine(report.DryRun
            ? "==================== DRY RUN: nothing was saved ===================="
            : "==================== INGEST RESULT ====================");

        var number = 1;
        foreach (var p in report.Processed)
        {
            PrintItem(number++, p, report.DryRun);
        }

        Console.WriteLine();
        Console.WriteLine("-------------------- Summary --------------------");
        Console.WriteLine($"Feeds read:              {report.FeedsRead} ({report.FeedsFailed} failed)");
        Console.WriteLine($"Items in feeds:          {report.ItemsFound}");
        Console.WriteLine($"  same story twice:      {report.DuplicatesAcrossFeeds}");
        Console.WriteLine($"  too old:               {report.TooOld}");
        Console.WriteLine($"  excluded domain:       {report.ExcludedDomain}");
        Console.WriteLine($"  stock tips/clickbait:  {report.StockTipClickbait}");
        Console.WriteLine($"  no finance signal:     {report.NoFinanceSignal}");
        Console.WriteLine($"  already stored:        {report.AlreadyStored}");
        Console.WriteLine($"  already rejected:      {report.AlreadyRejected}");
        Console.WriteLine($"New candidates:          {report.Candidates}");
        Console.WriteLine($"Selected:                {report.Processed.Count}");

        var verb = report.DryRun ? "Would add" : "Added";
        Console.WriteLine($"{verb} with AI text:     {report.Added}");
        Console.WriteLine($"{verb} without AI text:  {report.AddedWithoutAi}  (hidden until backfill-ai)");
        Console.WriteLine($"{(report.DryRun ? "Would reject" : "Rejected")} by AI:        {report.Rejected}");

        var failed = report.Processed.Count(p => p.Outcome == IngestOutcome.SaveFailed);
        if (failed > 0)
        {
            Console.WriteLine($"Save failed:             {failed}");
        }

        if (report.AiStopReason is not null)
        {
            Console.WriteLine();
            Console.WriteLine($"AI: {report.AiStopReason}");
        }
    }

    private static void PrintItem(int number, ProcessedItem p, bool dryRun)
    {
        var item = p.Item;
        var age = DateTime.UtcNow - item.PublishedAt;

        Console.WriteLine();
        Console.WriteLine($"[{number}] {Outcome(p.Outcome, dryRun)} · {item.CategorySlug} · {item.SourceName} · {age.TotalHours:0}h ago");
        Console.WriteLine($"    Original: {item.Title}");
        Console.WriteLine($"    Link:     {item.Url}");
        Console.WriteLine($"    Image:    {p.ImageFrom}{(p.ImageUrl is null ? "" : " " + p.ImageUrl)}");

        if (p.Brief is { IsFinanceRelevant: true } brief)
        {
            Console.WriteLine($"    Simple:   {brief.SimpleTitle}");
            Console.WriteLine($"    Summary:  {brief.Summary}");
            Console.WriteLine($"    Why it matters: {brief.WhyItMatters}");
            Console.WriteLine("    Key terms:");
            foreach (var term in brief.KeyTerms)
            {
                Console.WriteLine($"      - {term.Term}: {term.Meaning}");
            }
        }
        else if (p.Brief is { } rejected)
        {
            Console.WriteLine($"    Not finance: {rejected.Reason}");
        }
        else if (p.AiError is not null)
        {
            Console.WriteLine($"    AI failed: {p.AiError}");
        }
    }

    private static string Outcome(IngestOutcome outcome, bool dryRun) => (outcome, dryRun) switch
    {
        (IngestOutcome.Added, true) => "WOULD ADD",
        (IngestOutcome.AddedWithoutAi, true) => "WOULD ADD (no AI text)",
        (IngestOutcome.Rejected, true) => "WOULD REJECT",
        (IngestOutcome.Added, false) => "ADDED",
        (IngestOutcome.AddedWithoutAi, false) => "ADDED (no AI text)",
        (IngestOutcome.Rejected, false) => "REJECTED",
        _ => "SAVE FAILED",
    };
}
