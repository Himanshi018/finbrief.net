// Program.cs is the entry point of the Web API.
// It registers services (database, controllers, CORS, OpenAPI), then sets up the request pipeline.

using FinBrief.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from user-secrets (development) or an environment variable (production).
// It is never written in appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing connection string 'ConnectionStrings:Default'. See README for how to set it with dotnet user-secrets.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();

// OpenAPI describes every endpoint as JSON; Scalar (below) turns that into a page you can test in the browser.
builder.Services.AddOpenApi();

// CORS lets the Angular app (a different origin, e.g. http://localhost:4200) call this API from the browser.
// The allowed origins live in appsettings.json under "Cors:AllowedOrigins".
const string CorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                // JSON description at /openapi/v1.json
    app.MapScalarApiReference();     // interactive test page at /scalar
}

app.UseCors(CorsPolicy);
app.MapControllers();

// Simple endpoint that hosting platforms (and you) can call to check the API is alive.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
