using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task ReadyEndpoint_DoesNotRequireAuthentication()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/ready");

        // /ready checks DB — may return 503 if DB is unreachable,
        // but should never return 401/403 since it uses AllowAnonymous
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ReadyEndpoint_Exists_AndReturnsHealthCheckResult()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/ready");

        // Should return a health check result (200 or 503), not 404
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
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

    private HttpClient CreateAnonymousClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }
}
