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
    // Safe for single-instance deployments (current scale).
    // For multi-instance, switch to a migration job or init container.
    // Guarded by config so tests using Production environment can disable it.
    if (app.Configuration.GetValue("Database:AutoMigrate", true))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
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
