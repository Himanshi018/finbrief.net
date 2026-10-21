using FinBrief.Infrastructure.Ai;
using FinBrief.Infrastructure.Data;
using FinBrief.Infrastructure.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinBrief.Infrastructure;

// One place to register everything Infrastructure provides.
// Both the API and the Ingest console app call this, so they use the database the same way.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            // C# "PublishedAt" becomes the Postgres column "published_at" (easier to query in Supabase).
            .UseSnakeCaseNamingConvention());

        return services;
    }

    // Everything the console commands need: RSS reader, image fetcher, AI client, and the
    // ingest, backfill and cleanup services.
    public static IServiceCollection AddIngestion(
        this IServiceCollection services,
        IngestionSettings ingestionSettings,
        GroqSettings groqSettings,
        CleanupSettings cleanupSettings)
    {
        services.AddSingleton(ingestionSettings);
        services.AddSingleton(groqSettings);
        services.AddSingleton(cleanupSettings);

        // AddHttpClient<T> gives each class a ready HttpClient and manages the underlying connections for us.
        services.AddHttpClient<RssReader>(ConfigureWebClient);
        services.AddHttpClient<ImageFetcher>(ConfigureWebClient);

        services.AddSingleton<ArticleAiClient>();
        services.AddScoped<IngestionService>();
        services.AddScoped<BackfillService>();
        services.AddScoped<CleanupService>();

        return services;
    }

    private static void ConfigureWebClient(HttpClient client)
    {
        // Identify ourselves honestly to the publishers.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FinBriefBot/1.0 (finance news summaries; links back to the source)");
        client.Timeout = TimeSpan.FromSeconds(20);
    }
}
