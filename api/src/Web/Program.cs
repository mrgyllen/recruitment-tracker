using api.Infrastructure.Data;
using api.Web.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
