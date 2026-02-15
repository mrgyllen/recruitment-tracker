using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;

namespace api.Application.FunctionalTests.Endpoints;

/// <summary>
/// Tests for /health (liveness) and /ready (readiness) endpoints.
/// Uses self-contained WebApplicationFactory (no Testcontainers dependency)
/// following the same pattern as DevAuthenticationTests.
/// </summary>
[TestFixture]
public class HealthCheckEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:apiDb",
                    "Server=(localdb)\\mssqllocaldb;Database=apiTestDb;Trusted_Connection=True");
            });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task HealthEndpoint_Returns200_WhenApiIsRunning()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task HealthEndpoint_Returns200_WithoutAuthHeaders()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ReadyEndpoint_Returns200_WithoutAuthHeaders()
    {
        using var healthyFactory = CreateFactoryWithHealthyDbCheck("Development");

        var client = healthyFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/ready");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ReadyEndpoint_Returns200_WhenDatabaseIsAvailable()
    {
        using var healthyFactory = CreateFactoryWithHealthyDbCheck("Development");

        var client = healthyFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ReadyEndpoint_Returns503_WhenDatabaseIsUnreachable()
    {
        using var unreachableFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:apiDb",
                    "Server=localhost,19999;Database=nonexistent;User Id=sa;Password=fake;TrustServerCertificate=True;Connect Timeout=2");
            });

        var client = unreachableFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/ready");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task HealthEndpoint_Returns200_InProductionEnvironment()
    {
        using var prodFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
                builder.UseSetting("AzureAd:TenantId", "fake-tenant-id");
                builder.UseSetting("AzureAd:ClientId", "fake-client-id");
                builder.UseSetting("AzureAd:Audience", "api://fake-client-id");
                builder.UseSetting("Database:AutoMigrate", "false");
                builder.UseSetting("ConnectionStrings:apiDb",
                    "Server=(localdb)\\mssqllocaldb;Database=apiTestDb;Trusted_Connection=True");
            });

        var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task ReadyEndpoint_Returns200_InProductionEnvironment()
    {
        using var prodFactory = CreateFactoryWithHealthyDbCheck("Production");

        var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HttpClient CreateAnonymousClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <summary>
    /// Creates a factory that replaces the real DB health check with an always-healthy check.
    /// This allows testing /ready endpoint behavior without a real database connection.
    /// </summary>
    private static WebApplicationFactory<Program> CreateFactoryWithHealthyDbCheck(string environment)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting("ConnectionStrings:apiDb",
                    "Server=(localdb)\\mssqllocaldb;Database=apiTestDb;Trusted_Connection=True");

                if (environment == "Production")
                {
                    builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
                    builder.UseSetting("AzureAd:TenantId", "fake-tenant-id");
                    builder.UseSetting("AzureAd:ClientId", "fake-client-id");
                    builder.UseSetting("AzureAd:Audience", "api://fake-client-id");
                    builder.UseSetting("Database:AutoMigrate", "false");
                }

                builder.ConfigureTestServices(services =>
                {
                    // Replace the real DB health check with one that always returns Healthy
                    services.Configure<HealthCheckServiceOptions>(options =>
                    {
                        options.Registrations.Clear();
                        options.Registrations.Add(new HealthCheckRegistration(
                            "ready-db",
                            _ => new AlwaysHealthyCheck(),
                            failureStatus: null,
                            tags: new[] { "ready" }));
                    });
                });
            });
    }

    private class AlwaysHealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Test: always healthy"));
        }
    }
}
