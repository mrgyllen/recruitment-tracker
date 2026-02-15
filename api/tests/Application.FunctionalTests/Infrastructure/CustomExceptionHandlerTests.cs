using System.Net;
using System.Net.Http.Json;
using api.Domain.Exceptions;
using api.Web.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Infrastructure;

[TestFixture]
public class CustomExceptionHandlerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

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
                    services.AddSingleton<IStartupFilter, ExceptionThrowingStartupFilter>();
                });
            });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Dev-User-Id", "test-user");
        _client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Test User");
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task ValidationException_Returns400WithValidationProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/validation");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
        problem.Errors.Should().ContainKey("Name");
        problem.Errors["Name"].Should().Contain("Name is required");
    }

    [Test]
    public async Task NotFoundException_Returns404WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("The specified resource was not found.");
        problem.Detail.Should().Contain("TestEntity");
    }

    [Test]
    public async Task ForbiddenAccessException_Returns403WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/forbidden");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(403);
        problem.Title.Should().Be("Forbidden");
    }

    [Test]
    public async Task UnauthorizedAccessException_Returns401WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/unauthorized");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(401);
        problem.Title.Should().Be("Unauthorized");
    }

    [Test]
    public async Task RecruitmentClosedException_Returns400WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/recruitment-closed");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("Recruitment is closed");
        problem.Detail.Should().Contain("closed and cannot be modified");
    }

    [Test]
    public async Task DomainRuleViolationException_Returns400WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/domain-rule-violation");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("Domain rule violation");
        problem.Detail.Should().Be("Test domain rule violated");
    }

    [Test]
    public async Task StepHasOutcomesException_Returns409WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/step-has-outcomes");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
        problem.Title.Should().Be("Cannot remove -- outcomes recorded at this step");
        problem.Detail.Should().Contain("cannot be removed");
    }

    [Test]
    public async Task DuplicateStepNameException_Returns409WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/test-exceptions/duplicate-step-name");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(409);
        problem.Title.Should().Be("Duplicate step name");
        problem.Detail.Should().Contain("Duplicate Name");
    }

    /// <summary>
    /// Startup filter that injects exception-throwing middleware into the pipeline.
    /// Uses IStartupFilter to wrap the app's Configure, ensuring the test middleware
    /// runs after UseExceptionHandler (so exceptions are caught by CustomExceptionHandler).
    /// </summary>
    private class ExceptionThrowingStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                app.UseMiddleware<ExceptionThrowingMiddleware>();
            };
        }
    }

    /// <summary>
    /// Middleware that throws specific exceptions for test paths under /api/test-exceptions/.
    /// Used to test the CustomExceptionHandler middleware pipeline without requiring
    /// database setup or real endpoint handlers.
    /// </summary>
    private class ExceptionThrowingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionThrowingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            if (path is null || !path.StartsWith("/api/test-exceptions/"))
            {
                return _next(context);
            }

            var exceptionType = path.Replace("/api/test-exceptions/", "");

            throw exceptionType switch
            {
                "validation" => CreateValidationException(),
                "not-found" => new NotFoundException("TestEntity", "test-id"),
                "forbidden" => new ForbiddenAccessException(),
                "unauthorized" => new UnauthorizedAccessException(),
                "recruitment-closed" => new RecruitmentClosedException(Guid.NewGuid()),
                "domain-rule-violation" => new DomainRuleViolationException("Test domain rule violated"),
                "step-has-outcomes" => new StepHasOutcomesException(Guid.NewGuid()),
                "duplicate-step-name" => new DuplicateStepNameException("Duplicate Name"),
                _ => new InvalidOperationException($"Unknown test exception type: {exceptionType}")
            };
        }

        private static ValidationException CreateValidationException()
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>
            {
                new("Name", "Name is required")
            };
            return new ValidationException(failures);
        }
    }
}
