using api.Infrastructure.Data;
using api.Web.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // Auto-migrate: apply pending EF Core migrations on startup.
    // Opt-in via configuration (default: false for safety).
    // Enable in appsettings.Development.json for local dev, or via App Service config for Azure.
    // For multi-instance deployments, use a migration job or init container instead.
    var autoMigrateEnabled = app.Configuration.GetValue("Database:AutoMigrate", false);
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        if (autoMigrateEnabled)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            logger.LogInformation("Auto-migration enabled. Applying database migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Auto-migration disabled via configuration (Database:AutoMigrate=false).");
        }
    }

    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseExceptionHandler(options => { });

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<NoindexMiddleware>();

app.MapOpenApi();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false // Liveness: no dependency checks
}).AllowAnonymous();

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") // Readiness: DB check only
}).AllowAnonymous();

app.MapGet("/api/health-auth", () => Results.Ok(new { status = "authenticated" }))
    .RequireAuthorization();

app.MapEndpoints();

app.Run();

public partial class Program { }
