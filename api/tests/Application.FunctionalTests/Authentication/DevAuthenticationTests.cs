using System.Net;
using api.Web.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace api.Application.FunctionalTests.Authentication;

[TestFixture]
public class DevAuthenticationTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Use "Testing" to skip InitialiseDatabaseAsync() which needs real SQL Server.
                // Dev auth is re-added below since non-Development registers JWT bearer.
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:apiDb",
                    "Server=localhost;Database=fakedb;User Id=sa;Password=FakePass123!;TrustServerCertificate=True");
                builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
                builder.UseSetting("AzureAd:TenantId", "fake-tenant-id");
                builder.UseSetting("AzureAd:ClientId", "fake-client-id");
                builder.UseSetting("AzureAd:Audience", "api://fake-client-id");
                builder.UseSetting("Database:AutoMigrate", "false");
                builder.ConfigureTestServices(services =>
                {
                    services.AddDevelopmentAuthentication();
                });
            });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Request_WithDevAuthHeaders_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", "dev-user-a");
        client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Alice Dev");

        var response = await client.GetAsync("/api/health-auth");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Request_WithoutAuthHeaders_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/health-auth");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Request_WithDevHeadersButMissingUserId_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Alice Dev");

        var response = await client.GetAsync("/api/health-auth");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Request_WithDevHeaders_InProductionEnvironment_Returns401()
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
                    "Server=localhost,19876;Database=fakedb;User Id=sa;Password=FakePass123!;TrustServerCertificate=True;Connect Timeout=1");
            });

        var client = prodFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", "dev-user-a");
        client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Alice Dev");

        var response = await client.GetAsync("/api/health-auth");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Response_AlwaysIncludesNoindexHeader()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", "dev-user-a");
        client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Alice Dev");

        var response = await client.GetAsync("/api/health-auth");

        response.Headers.GetValues("X-Robots-Tag").Should().Contain("noindex");
    }
}
