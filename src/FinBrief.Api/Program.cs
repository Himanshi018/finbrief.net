// Program.cs is the entry point of the Web API.
// Controllers, CORS and Swagger come in phase 3.

using FinBrief.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from user-secrets (development) or an environment variable (production).
// It is never written in appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing connection string 'ConnectionStrings:Default'. See README for how to set it with dotnet user-secrets.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

// Simple endpoint that hosting platforms (and you) can call to check the API is alive.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
