using FinBrief.Infrastructure.Data;
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
}
