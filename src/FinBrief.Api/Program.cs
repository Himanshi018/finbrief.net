// Program.cs is the entry point of the Web API.
// Phase 1: just a health check. Database, controllers, CORS and Swagger come in later phases.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

// Simple endpoint that hosting platforms (and you) can call to check the API is alive.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
