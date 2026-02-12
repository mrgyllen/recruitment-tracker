# SSO Authentication Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Set up Microsoft Entra ID SSO authentication with a dev auth bypass, httpClient with token attachment, identity services (ICurrentUserService/ITenantContext), and noindex enforcement.

**Architecture:** SPA (React 19) authenticates via MSAL v5 (Auth Code + PKCE) against Entra ID. JWT access tokens flow to ASP.NET Core backend validated by Microsoft.Identity.Web. A dev auth bypass enables local development without an Entra tenant by sending user identity via custom HTTP headers. Identity is extracted into ICurrentUserService/ITenantContext for downstream consumption.

**Tech Stack:** @azure/msal-browser 5.1.0, @azure/msal-react 5.0.2, Microsoft.Identity.Web 4.3.0, ASP.NET Core .NET 10, React 19, TypeScript, Vitest, NUnit, NSubstitute, FluentAssertions

---

## Task 1: Install and Configure MSAL Packages (Spike)

**Testing mode: Spike** -- MSAL configuration is declarative; verify it initializes without errors.

**Files:**
- Create: `web/src/features/auth/msalConfig.ts`
- Create: `web/.env.example`
- Create: `web/.env.development`
- Modify: `web/package.json` (install packages)

**Step 1: Install MSAL packages**

Run:
```bash
cd web && npm install @azure/msal-browser@5.1.0 @azure/msal-react@5.0.2
```

**Step 2: Create `.env.example`**

```
# Entra ID / MSAL Configuration
VITE_ENTRA_CLIENT_ID=<your-client-id>
VITE_ENTRA_AUTHORITY=https://login.microsoftonline.com/<your-tenant-id>
VITE_ENTRA_REDIRECT_URI=http://localhost:5173

# Auth mode: "production" (real Entra ID) or "development" (dev toolbar with personas)
VITE_AUTH_MODE=development
```

**Step 3: Create `.env.development`**

```
VITE_AUTH_MODE=development
VITE_ENTRA_CLIENT_ID=placeholder-client-id
VITE_ENTRA_AUTHORITY=https://login.microsoftonline.com/placeholder-tenant-id
VITE_ENTRA_REDIRECT_URI=http://localhost:5173
```

**Step 4: Create `web/src/features/auth/msalConfig.ts`**

```typescript
import { type Configuration, PublicClientApplication } from '@azure/msal-browser'

const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority: import.meta.env.VITE_ENTRA_AUTHORITY,
    redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI,
    postLogoutRedirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
}

export const loginRequest = {
  scopes: [`api://${import.meta.env.VITE_ENTRA_CLIENT_ID}/.default`],
}

export const msalInstance = new PublicClientApplication(msalConfig)
```

**Step 5: Verify build succeeds**

Run:
```bash
cd web && npx tsc -b && npx vite build
```
Expected: Build succeeds with no errors.

**Step 6: Commit**

```bash
git add web/src/features/auth/msalConfig.ts web/.env.example web/.env.development web/package.json web/package-lock.json
git commit -m "feat(web): install MSAL v5 and add Entra ID configuration"
```

---

## Task 2: Create Backend Dev Auth Handler (Test-first)

**Testing mode: Test-first** -- Dev auth handler is security-critical; test that it populates ClaimsPrincipal from headers and that it's inactive outside Development.

**Files:**
- Create: `api/src/Web/Configuration/DevelopmentAuthenticationConfiguration.cs`
- Create: `api/tests/Application.FunctionalTests/Authentication/DevAuthenticationTests.cs`
- Modify: `api/src/Web/DependencyInjection.cs` (register dev auth conditionally)
- Modify: `api/src/Web/Program.cs` (add UseAuthentication/UseAuthorization)
- Modify: `api/Directory.Packages.props` (add Microsoft.Identity.Web)
- Modify: `api/src/Web/Web.csproj` (add Microsoft.Identity.Web reference)

**Step 1: Add Microsoft.Identity.Web to Directory.Packages.props**

Add to the `<ItemGroup>` in `api/Directory.Packages.props`:
```xml
<PackageVersion Include="Microsoft.Identity.Web" Version="4.3.0" />
```

**Step 2: Add package reference to Web.csproj**

Add to `<ItemGroup>` in `api/src/Web/Web.csproj`:
```xml
<PackageReference Include="Microsoft.Identity.Web" />
```

**Step 3: Write failing tests for dev auth handler**

Create `api/tests/Application.FunctionalTests/Authentication/DevAuthenticationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace api.Application.FunctionalTests.Authentication;

using static Testing;

[TestFixture]
public class DevAuthenticationTests : BaseTestFixture
{
    [Test]
    public async Task Request_WithDevAuthHeaders_Returns200()
    {
        var client = GetAnonymousClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", "dev-user-a");
        client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Alice Dev");

        var response = await client.GetAsync("/api/health-auth");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Request_WithoutAuthHeaders_Returns401()
    {
        var client = GetAnonymousClient();

        var response = await client.GetAsync("/api/health-auth");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Request_WithDevHeadersButMissingUserId_Returns401()
    {
        var client = GetAnonymousClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Alice Dev");

        var response = await client.GetAsync("/api/health-auth");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Note:** We need a `GetAnonymousClient()` method and a test endpoint `/api/health-auth` that requires authorization. These will be created as part of the implementation. `BaseTestFixture` already exists in the template.

**Step 4: Run tests to verify they fail**

Run:
```bash
cd api && dotnet test tests/Application.FunctionalTests/ --filter "FullyQualifiedName~DevAuthenticationTests" --no-restore
```
Expected: Compilation fails because the test endpoint and `GetAnonymousClient` don't exist yet.

**Step 5: Create DevelopmentAuthenticationConfiguration.cs**

Create `api/src/Web/Configuration/DevelopmentAuthenticationConfiguration.cs`:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace api.Web.Configuration;

public static class DevelopmentAuthenticationConfiguration
{
    public const string SchemeName = "DevAuth";

    public static IServiceCollection AddDevelopmentAuthentication(
        this IServiceCollection services)
    {
        services.AddAuthentication(SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                SchemeName, _ => { });

        return services;
    }
}

public class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Dev-User-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userName = Request.Headers["X-Dev-User-Name"].FirstOrDefault() ?? "Dev User";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

**Step 6: Register auth and add test endpoint in Program.cs and DependencyInjection.cs**

Modify `api/src/Web/DependencyInjection.cs` -- add authentication registration:

```csharp
using api.Web.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
```

Add to `AddWebServices` method, before the OpenApi registration:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentAuthentication();
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

Modify `api/src/Web/Program.cs` -- add auth middleware and test endpoint. After `app.UseHttpsRedirection();` and before `app.MapOpenApi();`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Add a health-auth test endpoint before `app.MapEndpoints();`:

```csharp
app.MapGet("/api/health-auth", () => Results.Ok(new { status = "authenticated" }))
    .RequireAuthorization();
```

**Step 7: Update CustomWebApplicationFactory for dev auth testing**

We need `GetAnonymousClient()` -- a client that does NOT have the automatic test user injected. Update `CustomWebApplicationFactory` or add a helper to `Testing.cs`.

Add to `api/tests/Application.FunctionalTests/Testing.cs`:

```csharp
public static HttpClient GetAnonymousClient()
{
    return s_factory!.CreateClient();
}
```

The existing `CustomWebApplicationFactory` uses `UseEnvironment("Testing")` but we need Development for dev auth. We'll need a separate factory or update the existing one.

Create a minimal helper. Add a new factory setup in the test class that uses Development environment:

Actually, let's handle this properly. The `CustomWebApplicationFactory` currently sets environment to "Testing". For dev auth tests, we need "Development". Rather than modifying the shared factory, we'll create the factory inline in the test.

Update the test to create its own factory:

```csharp
using System.Net;
using api.Web.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:apiDb",
                    "Server=(localdb)\\mssqllocaldb;Database=apiTestDb;Trusted_Connection=True;MultipleActiveResultSets=true");
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
                builder.UseSetting("ConnectionStrings:apiDb",
                    "Server=(localdb)\\mssqllocaldb;Database=apiTestDb;Trusted_Connection=True;MultipleActiveResultSets=true");
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
}
```

**Step 8: Run tests to verify they pass**

Run:
```bash
cd api && dotnet test tests/Application.FunctionalTests/ --filter "FullyQualifiedName~DevAuthenticationTests" --no-restore
```
Expected: All 4 tests pass.

**Step 9: Commit**

```bash
git add api/src/Web/Configuration/DevelopmentAuthenticationConfiguration.cs api/src/Web/DependencyInjection.cs api/src/Web/Program.cs api/src/Web/Web.csproj api/Directory.Packages.props api/tests/Application.FunctionalTests/Authentication/DevAuthenticationTests.cs
git commit -m "feat(api): add dev auth handler with fallback policy and JWT bearer config"
```

---

## Task 3: Create Identity Services -- ICurrentUserService and ITenantContext (Test-first)

**Testing mode: Test-first** -- Unit test that services extract correct values from claims.

**Files:**
- Create: `api/src/Application/Common/Interfaces/ICurrentUserService.cs`
- Create: `api/src/Application/Common/Interfaces/ITenantContext.cs`
- Create: `api/src/Infrastructure/Identity/CurrentUserService.cs`
- Create: `api/src/Infrastructure/Identity/TenantContext.cs`
- Create: `api/tests/Application.UnitTests/Common/Identity/CurrentUserServiceTests.cs`
- Create: `api/tests/Application.UnitTests/Common/Identity/TenantContextTests.cs`
- Modify: `api/src/Infrastructure/DependencyInjection.cs` (register services)

**Step 1: Write failing tests for CurrentUserService**

Create `api/tests/Application.UnitTests/Common/Identity/CurrentUserServiceTests.cs`:

```csharp
using System.Security.Claims;
using api.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace api.Application.UnitTests.Common.Identity;

[TestFixture]
public class CurrentUserServiceTests
{
    [Test]
    public void UserId_WhenClaimPresent_ReturnsClaimValue()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal });

        var service = new CurrentUserService(httpContextAccessor);

        service.UserId.Should().Be("user-123");
    }

    [Test]
    public void UserId_WhenNoHttpContext_ReturnsNull()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var service = new CurrentUserService(httpContextAccessor);

        service.UserId.Should().BeNull();
    }

    [Test]
    public void UserId_WhenNoClaim_ReturnsNull()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal });

        var service = new CurrentUserService(httpContextAccessor);

        service.UserId.Should().BeNull();
    }
}
```

**Step 2: Run tests to verify they fail**

Run:
```bash
cd api && dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~CurrentUserServiceTests" --no-restore
```
Expected: Compilation fails because `CurrentUserService` doesn't exist yet.

**Step 3: Create ICurrentUserService interface**

Create `api/src/Application/Common/Interfaces/ICurrentUserService.cs`:

```csharp
namespace api.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
}
```

**Step 4: Create CurrentUserService implementation**

Create `api/src/Infrastructure/Identity/CurrentUserService.cs`:

```csharp
using System.Security.Claims;
using api.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace api.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
```

**Step 5: Run CurrentUserService tests to verify they pass**

Run:
```bash
cd api && dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~CurrentUserServiceTests" --no-restore
```
Expected: All 3 tests pass.

**Step 6: Write failing tests for TenantContext**

Create `api/tests/Application.UnitTests/Common/Identity/TenantContextTests.cs`:

```csharp
using api.Application.Common.Interfaces;
using api.Infrastructure.Identity;
using FluentAssertions;
using NSubstitute;

namespace api.Application.UnitTests.Common.Identity;

[TestFixture]
public class TenantContextTests
{
    [Test]
    public void UserId_DelegatesToCurrentUserService()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns("user-456");

        var tenantContext = new TenantContext(currentUserService);

        tenantContext.UserId.Should().Be("user-456");
    }

    [Test]
    public void RecruitmentId_DefaultsToNull()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        var tenantContext = new TenantContext(currentUserService);

        tenantContext.RecruitmentId.Should().BeNull();
    }

    [Test]
    public void IsServiceContext_DefaultsToFalse()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        var tenantContext = new TenantContext(currentUserService);

        tenantContext.IsServiceContext.Should().BeFalse();
    }
}
```

**Step 7: Run TenantContext tests to verify they fail**

Run:
```bash
cd api && dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~TenantContextTests" --no-restore
```
Expected: Compilation fails because `ITenantContext` and `TenantContext` don't exist.

**Step 8: Create ITenantContext interface**

Create `api/src/Application/Common/Interfaces/ITenantContext.cs`:

```csharp
namespace api.Application.Common.Interfaces;

public interface ITenantContext
{
    string? UserId { get; }
    Guid? RecruitmentId { get; set; }
    bool IsServiceContext { get; set; }
}
```

**Step 9: Create TenantContext implementation**

Create `api/src/Infrastructure/Identity/TenantContext.cs`:

```csharp
using api.Application.Common.Interfaces;

namespace api.Infrastructure.Identity;

public class TenantContext : ITenantContext
{
    private readonly ICurrentUserService _currentUserService;

    public TenantContext(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public string? UserId => _currentUserService.UserId;
    public Guid? RecruitmentId { get; set; }
    public bool IsServiceContext { get; set; }
}
```

**Step 10: Run TenantContext tests to verify they pass**

Run:
```bash
cd api && dotnet test tests/Application.UnitTests/ --filter "FullyQualifiedName~TenantContextTests" --no-restore
```
Expected: All 3 tests pass.

**Step 11: Register services in DI**

Modify `api/src/Infrastructure/DependencyInjection.cs` -- add to `AddInfrastructureServices`:

```csharp
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
```

Add the using if not present:
```csharp
using api.Infrastructure.Identity;
```

Note: `IHttpContextAccessor` is already registered in `Web/DependencyInjection.cs`.

**Step 12: Run all backend tests to verify nothing broke**

Run:
```bash
cd api && dotnet test --no-restore
```
Expected: All tests pass.

**Step 13: Commit**

```bash
git add api/src/Application/Common/Interfaces/ICurrentUserService.cs api/src/Application/Common/Interfaces/ITenantContext.cs api/src/Infrastructure/Identity/CurrentUserService.cs api/src/Infrastructure/Identity/TenantContext.cs api/src/Infrastructure/DependencyInjection.cs api/tests/Application.UnitTests/Common/Identity/
git commit -m "feat(api): add ICurrentUserService and ITenantContext with claim-based identity extraction"
```

---

## Task 4: Configure Noindex Headers (Test-first)

**Testing mode: Test-first** -- Integration test verifies header is present.

**Files:**
- Create: `api/src/Web/Middleware/NoindexMiddleware.cs`
- Modify: `api/src/Web/Program.cs` (register middleware)
- Modify: `web/index.html` (add meta tag)
- Modify: `api/tests/Application.FunctionalTests/Authentication/DevAuthenticationTests.cs` (add noindex test)

**Step 1: Write failing test for noindex header**

Add to `DevAuthenticationTests.cs` (since we already have a factory there):

```csharp
[Test]
public async Task Response_AlwaysIncludesNoindexHeader()
{
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Dev-User-Id", "dev-user-a");
    client.DefaultRequestHeaders.Add("X-Dev-User-Name", "Alice Dev");

    var response = await client.GetAsync("/api/health-auth");

    response.Headers.GetValues("X-Robots-Tag").Should().Contain("noindex");
}
```

**Step 2: Run test to verify it fails**

Run:
```bash
cd api && dotnet test tests/Application.FunctionalTests/ --filter "FullyQualifiedName~Response_AlwaysIncludesNoindexHeader" --no-restore
```
Expected: Test fails because the header is not present.

**Step 3: Create NoindexMiddleware**

Create `api/src/Web/Middleware/NoindexMiddleware.cs`:

```csharp
namespace api.Web.Middleware;

public class NoindexMiddleware
{
    private readonly RequestDelegate _next;

    public NoindexMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Robots-Tag"] = "noindex";
        await _next(context);
    }
}
```

**Step 4: Register middleware in Program.cs**

Add after `app.UseAuthorization();`:

```csharp
app.UseMiddleware<api.Web.Middleware.NoindexMiddleware>();
```

**Step 5: Run test to verify it passes**

Run:
```bash
cd api && dotnet test tests/Application.FunctionalTests/ --filter "FullyQualifiedName~Response_AlwaysIncludesNoindexHeader" --no-restore
```
Expected: Test passes.

**Step 6: Add noindex meta tag to `web/index.html`**

Add inside `<head>`, after the viewport meta tag:

```html
<meta name="robots" content="noindex" />
```

**Step 7: Commit**

```bash
git add api/src/Web/Middleware/NoindexMiddleware.cs api/src/Web/Program.cs api/tests/Application.FunctionalTests/Authentication/DevAuthenticationTests.cs web/index.html
git commit -m "feat: add noindex headers and meta tag to prevent search engine indexing"
```

---

## Task 5: Create Problem Details Parser (Test-first)

**Testing mode: Test-first** -- Parser is pure logic with clear inputs/outputs.

**Files:**
- Create: `web/src/lib/utils/problemDetails.ts`
- Create: `web/src/lib/utils/problemDetails.test.ts`

**Step 1: Write failing tests for Problem Details parser**

Create `web/src/lib/utils/problemDetails.test.ts`:

```typescript
import { describe, expect, it } from 'vitest'
import { type ProblemDetails, parseProblemDetails } from './problemDetails'

describe('parseProblemDetails', () => {
  it('should parse a valid Problem Details response', () => {
    const json = {
      type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
      title: 'Validation Failed',
      status: 400,
      detail: 'One or more validation errors occurred.',
      errors: { Title: ['Title is required.'] },
    }

    const result = parseProblemDetails(json)

    expect(result.type).toBe('https://tools.ietf.org/html/rfc7231#section-6.5.1')
    expect(result.title).toBe('Validation Failed')
    expect(result.status).toBe(400)
    expect(result.detail).toBe('One or more validation errors occurred.')
    expect(result.errors).toEqual({ Title: ['Title is required.'] })
  })

  it('should handle minimal Problem Details (only status and title)', () => {
    const json = {
      title: 'Not Found',
      status: 404,
    }

    const result = parseProblemDetails(json)

    expect(result.title).toBe('Not Found')
    expect(result.status).toBe(404)
    expect(result.detail).toBeUndefined()
    expect(result.errors).toBeUndefined()
  })

  it('should return fallback for non-Problem Details JSON', () => {
    const json = { message: 'Something went wrong' }

    const result = parseProblemDetails(json)

    expect(result.title).toBe('An unexpected error occurred')
    expect(result.status).toBe(500)
  })
})
```

**Step 2: Run tests to verify they fail**

Run:
```bash
cd web && npx vitest run src/lib/utils/problemDetails.test.ts
```
Expected: Test fails because module doesn't exist.

**Step 3: Implement Problem Details parser**

Create `web/src/lib/utils/problemDetails.ts`:

```typescript
export interface ProblemDetails {
  type?: string
  title: string
  status: number
  detail?: string
  errors?: Record<string, string[]>
}

export function parseProblemDetails(json: unknown): ProblemDetails {
  if (
    typeof json === 'object' &&
    json !== null &&
    'title' in json &&
    'status' in json &&
    typeof (json as Record<string, unknown>).title === 'string' &&
    typeof (json as Record<string, unknown>).status === 'number'
  ) {
    const obj = json as Record<string, unknown>
    return {
      type: typeof obj.type === 'string' ? obj.type : undefined,
      title: obj.title as string,
      status: obj.status as number,
      detail: typeof obj.detail === 'string' ? obj.detail : undefined,
      errors: obj.errors as Record<string, string[]> | undefined,
    }
  }

  return {
    title: 'An unexpected error occurred',
    status: 500,
  }
}
```

**Step 4: Run tests to verify they pass**

Run:
```bash
cd web && npx vitest run src/lib/utils/problemDetails.test.ts
```
Expected: All 3 tests pass.

**Step 5: Commit**

```bash
git add web/src/lib/utils/problemDetails.ts web/src/lib/utils/problemDetails.test.ts
git commit -m "feat(web): add RFC 9457 Problem Details parser"
```

---

## Task 6: Create httpClient with Auth Token Attachment (Mixed)

**Testing mode: Mixed** -- Test-first for dev-mode headers, 401 handling, Problem Details. Spike for MSAL acquireTokenSilent integration, then characterization tests.

**Files:**
- Create: `web/src/lib/api/httpClient.ts`
- Create: `web/src/lib/api/httpClient.test.ts`

**Step 1: Write failing tests for httpClient**

Create `web/src/lib/api/httpClient.test.ts`:

```typescript
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '../../mocks/server'

// We'll test the module by importing it after mocking env vars

describe('httpClient', () => {
  describe('dev mode', () => {
    beforeEach(() => {
      vi.stubEnv('VITE_AUTH_MODE', 'development')
      localStorage.clear()
    })

    afterEach(() => {
      vi.unstubAllEnvs()
    })

    it('should send X-Dev-User-Id and X-Dev-User-Name headers when dev persona is set', async () => {
      localStorage.setItem(
        'dev-auth-user',
        JSON.stringify({ id: 'dev-user-a', name: 'Alice Dev' }),
      )

      let capturedHeaders: Record<string, string> = {}
      server.use(
        http.get('/api/test', ({ request }) => {
          capturedHeaders = {
            'x-dev-user-id': request.headers.get('X-Dev-User-Id') ?? '',
            'x-dev-user-name': request.headers.get('X-Dev-User-Name') ?? '',
          }
          return HttpResponse.json({ ok: true })
        }),
      )

      const { apiGet } = await import('./httpClient')
      const result = await apiGet<{ ok: boolean }>('/test')

      expect(result).toEqual({ ok: true })
      expect(capturedHeaders['x-dev-user-id']).toBe('dev-user-a')
      expect(capturedHeaders['x-dev-user-name']).toBe('Alice Dev')
    })

    it('should send no auth headers when dev persona is unauthenticated', async () => {
      // No dev-auth-user in localStorage = unauthenticated

      let capturedHeaders: Record<string, string | null> = {}
      server.use(
        http.get('/api/test', ({ request }) => {
          capturedHeaders = {
            'x-dev-user-id': request.headers.get('X-Dev-User-Id'),
            authorization: request.headers.get('Authorization'),
          }
          return HttpResponse.json({ ok: true })
        }),
      )

      const { apiGet } = await import('./httpClient')
      const result = await apiGet<{ ok: boolean }>('/test')

      expect(result).toEqual({ ok: true })
      expect(capturedHeaders['x-dev-user-id']).toBeNull()
      expect(capturedHeaders['authorization']).toBeNull()
    })
  })

  describe('error handling', () => {
    beforeEach(() => {
      vi.stubEnv('VITE_AUTH_MODE', 'development')
      localStorage.setItem(
        'dev-auth-user',
        JSON.stringify({ id: 'dev-user-a', name: 'Alice Dev' }),
      )
    })

    afterEach(() => {
      vi.unstubAllEnvs()
      localStorage.clear()
    })

    it('should throw ApiError with Problem Details on 400 response', async () => {
      server.use(
        http.get('/api/test', () => {
          return HttpResponse.json(
            {
              type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
              title: 'Validation Failed',
              status: 400,
              errors: { Name: ['Name is required.'] },
            },
            { status: 400 },
          )
        }),
      )

      const { apiGet, ApiError } = await import('./httpClient')

      await expect(apiGet('/test')).rejects.toThrow(ApiError)
      try {
        await apiGet('/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        const apiError = error as InstanceType<typeof ApiError>
        expect(apiError.status).toBe(400)
        expect(apiError.problemDetails.title).toBe('Validation Failed')
      }
    })

    it('should throw AuthError on 401 response', async () => {
      server.use(
        http.get('/api/test', () => {
          return HttpResponse.json(
            { title: 'Unauthorized', status: 401 },
            { status: 401 },
          )
        }),
      )

      const { apiGet, AuthError } = await import('./httpClient')

      await expect(apiGet('/test')).rejects.toThrow(AuthError)
    })
  })

  describe('HTTP methods', () => {
    beforeEach(() => {
      vi.stubEnv('VITE_AUTH_MODE', 'development')
      localStorage.setItem(
        'dev-auth-user',
        JSON.stringify({ id: 'dev-user-a', name: 'Alice Dev' }),
      )
    })

    afterEach(() => {
      vi.unstubAllEnvs()
      localStorage.clear()
    })

    it('should send POST with JSON body', async () => {
      let capturedBody: unknown = null
      server.use(
        http.post('/api/items', async ({ request }) => {
          capturedBody = await request.json()
          return HttpResponse.json({ id: '1' }, { status: 201 })
        }),
      )

      const { apiPost } = await import('./httpClient')
      const result = await apiPost<{ id: string }>('/items', { name: 'Test' })

      expect(result).toEqual({ id: '1' })
      expect(capturedBody).toEqual({ name: 'Test' })
    })

    it('should send PUT with JSON body', async () => {
      server.use(
        http.put('/api/items/1', () => {
          return HttpResponse.json({ id: '1', name: 'Updated' })
        }),
      )

      const { apiPut } = await import('./httpClient')
      const result = await apiPut<{ id: string; name: string }>('/items/1', {
        name: 'Updated',
      })

      expect(result).toEqual({ id: '1', name: 'Updated' })
    })

    it('should send DELETE request', async () => {
      server.use(
        http.delete('/api/items/1', () => {
          return new HttpResponse(null, { status: 204 })
        }),
      )

      const { apiDelete } = await import('./httpClient')
      await apiDelete('/items/1')
    })
  })
})
```

**Step 2: Run tests to verify they fail**

Run:
```bash
cd web && npx vitest run src/lib/api/httpClient.test.ts
```
Expected: Tests fail because httpClient module doesn't exist.

**Step 3: Implement httpClient**

Create `web/src/lib/api/httpClient.ts`:

```typescript
import { parseProblemDetails, type ProblemDetails } from '../utils/problemDetails'

const API_BASE = '/api'

export class AuthError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'AuthError'
  }
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problemDetails: ProblemDetails,
  ) {
    super(problemDetails.title)
    this.name = 'ApiError'
  }
}

async function getAuthHeaders(): Promise<HeadersInit> {
  const isDev = import.meta.env.VITE_AUTH_MODE === 'development'

  if (isDev) {
    const devUser = JSON.parse(localStorage.getItem('dev-auth-user') || 'null') as {
      id: string
      name: string
    } | null
    if (!devUser) {
      return { 'Content-Type': 'application/json' }
    }
    return {
      'X-Dev-User-Id': devUser.id,
      'X-Dev-User-Name': devUser.name,
      'Content-Type': 'application/json',
    }
  }

  // Production mode: acquire token via MSAL
  const { msalInstance, loginRequest } = await import('../../features/auth/msalConfig')
  const accounts = msalInstance.getAllAccounts()
  if (accounts.length === 0) {
    throw new AuthError('No active session')
  }
  try {
    const { InteractionRequiredAuthError } = await import('@azure/msal-browser')
    const { accessToken } = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account: accounts[0],
    })
    return {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    }
  } catch (error) {
    const { InteractionRequiredAuthError } = await import('@azure/msal-browser')
    if (error instanceof InteractionRequiredAuthError) {
      await msalInstance.loginRedirect(loginRequest)
      throw new AuthError('Session expired — redirecting to login')
    }
    throw error
  }
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (res.status === 401) {
    const isDev = import.meta.env.VITE_AUTH_MODE === 'development'
    if (!isDev) {
      const { msalInstance, loginRequest } = await import('../../features/auth/msalConfig')
      await msalInstance.loginRedirect(loginRequest)
    }
    throw new AuthError('Session expired')
  }

  if (res.status === 204) {
    return undefined as T
  }

  if (!res.ok) {
    const json = await res.json()
    throw new ApiError(res.status, parseProblemDetails(json))
  }

  return res.json() as Promise<T>
}

export async function apiGet<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { headers: await getAuthHeaders() })
  return handleResponse<T>(res)
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: await getAuthHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  })
  return handleResponse<T>(res)
}

export async function apiPut<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers: await getAuthHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  })
  return handleResponse<T>(res)
}

export async function apiDelete(path: string): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'DELETE',
    headers: await getAuthHeaders(),
  })
  return handleResponse<void>(res)
}
```

**Step 4: Run tests to verify they pass**

Run:
```bash
cd web && npx vitest run src/lib/api/httpClient.test.ts
```
Expected: All tests pass.

**Step 5: Commit**

```bash
git add web/src/lib/api/httpClient.ts web/src/lib/api/httpClient.test.ts
git commit -m "feat(web): add httpClient with dev auth headers, 401 handling, and Problem Details"
```

---

## Task 7: Create Frontend Dev Auth Provider (Test-first)

**Testing mode: Test-first** -- DevAuthProvider is UI + state logic for persona switching.

**Files:**
- Create: `web/src/features/auth/DevAuthProvider.tsx`
- Create: `web/src/features/auth/DevAuthProvider.test.tsx`
- Create: `web/src/features/auth/AuthContext.tsx`
- Create: `web/src/features/auth/AuthContext.test.tsx`
- Create: `web/src/features/auth/authProvider.ts`
- Create: `web/src/features/auth/LoginRedirect.tsx`

**Step 1: Write failing tests for DevAuthProvider**

Create `web/src/features/auth/DevAuthProvider.test.tsx`:

```tsx
import { afterEach, describe, expect, it } from 'vitest'
import { render, screen } from '../../test-utils'
import userEvent from '@testing-library/user-event'
import { DevAuthProvider, useDevAuth } from './DevAuthProvider'

function TestConsumer() {
  const { currentUser } = useDevAuth()
  return <div data-testid="current-user">{currentUser?.name ?? 'none'}</div>
}

describe('DevAuthProvider', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('should render dev toolbar with persona options', () => {
    render(
      <DevAuthProvider>
        <div>App</div>
      </DevAuthProvider>,
    )

    expect(screen.getByText('DEV MODE')).toBeInTheDocument()
    expect(screen.getByRole('combobox')).toBeInTheDocument()
  })

  it('should default to User A persona', () => {
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
    )

    expect(screen.getByTestId('current-user')).toHaveTextContent('Alice Dev')
  })

  it('should switch personas when selecting from dropdown', async () => {
    const user = userEvent.setup()
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
    )

    await user.selectOptions(screen.getByRole('combobox'), 'dev-user-b')

    expect(screen.getByTestId('current-user')).toHaveTextContent('Bob Dev')
  })

  it('should persist selected persona to localStorage', async () => {
    const user = userEvent.setup()
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
    )

    await user.selectOptions(screen.getByRole('combobox'), 'dev-admin')

    const stored = JSON.parse(localStorage.getItem('dev-auth-user') ?? 'null')
    expect(stored).toEqual({ id: 'dev-admin', name: 'Admin Dev' })
  })

  it('should clear identity when selecting Unauthenticated', async () => {
    const user = userEvent.setup()
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
    )

    await user.selectOptions(screen.getByRole('combobox'), 'unauthenticated')

    expect(screen.getByTestId('current-user')).toHaveTextContent('none')
    expect(localStorage.getItem('dev-auth-user')).toBeNull()
  })
})
```

**Step 2: Run tests to verify they fail**

Run:
```bash
cd web && npx vitest run src/features/auth/DevAuthProvider.test.tsx
```
Expected: Tests fail because module doesn't exist.

**Step 3: Implement DevAuthProvider**

Create `web/src/features/auth/DevAuthProvider.tsx`:

```tsx
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

export interface DevUser {
  id: string
  name: string
}

interface DevAuthContextValue {
  currentUser: DevUser | null
  setPersona: (id: string) => void
  isAuthenticated: boolean
}

const DevAuthContext = createContext<DevAuthContextValue | null>(null)

const personas: Record<string, DevUser> = {
  'dev-user-a': { id: 'dev-user-a', name: 'Alice Dev' },
  'dev-user-b': { id: 'dev-user-b', name: 'Bob Dev' },
  'dev-admin': { id: 'dev-admin', name: 'Admin Dev' },
}

function getInitialPersona(): DevUser | null {
  const stored = localStorage.getItem('dev-auth-user')
  if (stored) {
    try {
      return JSON.parse(stored) as DevUser
    } catch {
      return personas['dev-user-a']
    }
  }
  // Default to User A
  const defaultUser = personas['dev-user-a']
  localStorage.setItem('dev-auth-user', JSON.stringify(defaultUser))
  return defaultUser
}

export function DevAuthProvider({ children }: { children: ReactNode }) {
  const [currentUser, setCurrentUser] = useState<DevUser | null>(getInitialPersona)

  const setPersona = useCallback((id: string) => {
    if (id === 'unauthenticated') {
      setCurrentUser(null)
      localStorage.removeItem('dev-auth-user')
    } else {
      const user = personas[id]
      if (user) {
        setCurrentUser(user)
        localStorage.setItem('dev-auth-user', JSON.stringify(user))
      }
    }
  }, [])

  const value = useMemo(
    () => ({
      currentUser,
      setPersona,
      isAuthenticated: currentUser !== null,
    }),
    [currentUser, setPersona],
  )

  return (
    <DevAuthContext.Provider value={value}>
      {children}
      <DevToolbar currentPersonaId={currentUser?.id ?? 'unauthenticated'} onSelect={setPersona} />
    </DevAuthContext.Provider>
  )
}

export function useDevAuth(): DevAuthContextValue {
  const context = useContext(DevAuthContext)
  if (!context) {
    throw new Error('useDevAuth must be used within a DevAuthProvider')
  }
  return context
}

function DevToolbar({
  currentPersonaId,
  onSelect,
}: {
  currentPersonaId: string
  onSelect: (id: string) => void
}) {
  return (
    <div
      style={{
        position: 'fixed',
        bottom: 16,
        right: 16,
        zIndex: 9999,
        background: '#ff6b35',
        color: 'white',
        padding: '8px 12px',
        borderRadius: 8,
        fontFamily: 'monospace',
        fontSize: 13,
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        boxShadow: '0 2px 8px rgba(0,0,0,0.3)',
      }}
    >
      <span style={{ fontWeight: 'bold' }}>DEV MODE</span>
      <select
        value={currentPersonaId}
        onChange={(e) => onSelect(e.target.value)}
        style={{
          background: 'white',
          color: '#333',
          border: 'none',
          borderRadius: 4,
          padding: '2px 4px',
          fontSize: 13,
        }}
      >
        <option value="dev-user-a">Alice Dev (User A)</option>
        <option value="dev-user-b">Bob Dev (User B)</option>
        <option value="dev-admin">Admin Dev</option>
        <option value="unauthenticated">Unauthenticated</option>
      </select>
    </div>
  )
}
```

**Step 4: Run DevAuthProvider tests to verify they pass**

Run:
```bash
cd web && npx vitest run src/features/auth/DevAuthProvider.test.tsx
```
Expected: All 5 tests pass.

**Step 5: Write failing tests for AuthContext**

Create `web/src/features/auth/AuthContext.test.tsx`:

```tsx
import { describe, expect, it, vi, afterEach } from 'vitest'
import { render, screen } from '../../test-utils'
import { AuthProvider, useAuth } from './AuthContext'

function TestConsumer() {
  const { isAuthenticated, user, signOut } = useAuth()
  return (
    <div>
      <span data-testid="is-auth">{String(isAuthenticated)}</span>
      <span data-testid="user-name">{user?.name ?? 'none'}</span>
      <button onClick={signOut}>Sign Out</button>
    </div>
  )
}

describe('AuthContext (dev mode)', () => {
  afterEach(() => {
    localStorage.clear()
    vi.unstubAllEnvs()
  })

  it('should provide auth state in dev mode', () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development')

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    )

    expect(screen.getByTestId('is-auth')).toHaveTextContent('true')
    expect(screen.getByTestId('user-name')).toHaveTextContent('Alice Dev')
  })

  it('should show unauthenticated when no dev persona', () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development')
    // Set unauthenticated before rendering
    localStorage.removeItem('dev-auth-user')

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    )

    // DevAuthProvider defaults to User A, so it'll be authenticated
    // This test verifies the initial default
    expect(screen.getByTestId('is-auth')).toHaveTextContent('true')
  })
})
```

**Step 6: Run AuthContext tests to verify they fail**

Run:
```bash
cd web && npx vitest run src/features/auth/AuthContext.test.tsx
```
Expected: Tests fail because module doesn't exist.

**Step 7: Implement AuthContext**

Create `web/src/features/auth/AuthContext.tsx`:

```tsx
import { createContext, useContext, useMemo, type ReactNode } from 'react'
import { DevAuthProvider, useDevAuth } from './DevAuthProvider'

interface AuthUser {
  id: string
  name: string
}

interface AuthContextValue {
  isAuthenticated: boolean
  user: AuthUser | null
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

function DevAuthBridge({ children }: { children: ReactNode }) {
  const { currentUser, isAuthenticated, setPersona } = useDevAuth()

  const value = useMemo(
    () => ({
      isAuthenticated,
      user: currentUser,
      signOut: () => setPersona('unauthenticated'),
    }),
    [isAuthenticated, currentUser, setPersona],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

function MsalAuthProvider({ children }: { children: ReactNode }) {
  // MSAL provider implementation — deferred until production auth is needed
  // For now, this just renders children without auth context
  // Full implementation requires msalInstance.initialize() which is async
  const value = useMemo(
    () => ({
      isAuthenticated: false,
      user: null,
      signOut: () => {
        // Will call msalInstance.logoutRedirect()
      },
    }),
    [],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const isDev = import.meta.env.VITE_AUTH_MODE === 'development'

  if (isDev) {
    return (
      <DevAuthProvider>
        <DevAuthBridge>{children}</DevAuthBridge>
      </DevAuthProvider>
    )
  }

  return <MsalAuthProvider>{children}</MsalAuthProvider>
}
```

**Step 8: Create LoginRedirect component**

Create `web/src/features/auth/LoginRedirect.tsx`:

```tsx
import { useEffect } from 'react'

export function LoginRedirect() {
  useEffect(() => {
    const isDev = import.meta.env.VITE_AUTH_MODE === 'development'
    if (!isDev) {
      // In production, MSAL handles redirect via msalInstance
      import('../auth/msalConfig').then(({ msalInstance, loginRequest }) => {
        msalInstance.loginRedirect(loginRequest)
      })
    }
  }, [])

  return <div>Redirecting to login...</div>
}
```

**Step 9: Create authProvider factory**

Create `web/src/features/auth/authProvider.ts`:

```typescript
// Re-export AuthProvider which internally handles dev vs production mode
export { AuthProvider, useAuth } from './AuthContext'
```

**Step 10: Run AuthContext tests to verify they pass**

Run:
```bash
cd web && npx vitest run src/features/auth/AuthContext.test.tsx
```
Expected: All tests pass.

**Step 11: Run all frontend tests**

Run:
```bash
cd web && npx vitest run
```
Expected: All tests pass.

**Step 12: Commit**

```bash
git add web/src/features/auth/
git commit -m "feat(web): add AuthContext with DevAuthProvider persona switching and login redirect"
```

---

## Task 8: Update Test Infrastructure for Auth (Characterization)

**Testing mode: Characterization** -- Extending test infrastructure to support auth context.

**Files:**
- Create: `web/src/mocks/auth.ts`
- Modify: `web/src/test-utils.tsx` (wrap with AuthProvider)
- Modify: `web/src/App.test.tsx` (verify existing tests still pass)

**Step 1: Create mock auth helpers**

Create `web/src/mocks/auth.ts`:

```typescript
export const mockUsers = {
  userA: { id: 'dev-user-a', name: 'Alice Dev' },
  userB: { id: 'dev-user-b', name: 'Bob Dev' },
  admin: { id: 'dev-admin', name: 'Admin Dev' },
} as const

export function setMockUser(user: { id: string; name: string } | null): void {
  if (user) {
    localStorage.setItem('dev-auth-user', JSON.stringify(user))
  } else {
    localStorage.removeItem('dev-auth-user')
  }
}
```

**Step 2: Update test-utils.tsx to wrap with AuthProvider**

Update `web/src/test-utils.tsx`:

```tsx
import { render } from '@testing-library/react'
import type { RenderOptions } from '@testing-library/react'
import type { ReactElement } from 'react'
import { AuthProvider } from './features/auth/AuthContext'

function AllProviders({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>
}

const customRender = (ui: ReactElement, options?: Omit<RenderOptions, 'wrapper'>) =>
  render(ui, { wrapper: AllProviders, ...options })

export * from '@testing-library/react'
export { customRender as render }
```

**Step 3: Verify existing tests still pass**

Run:
```bash
cd web && npx vitest run
```
Expected: All tests pass including `App.test.tsx`.

**Step 4: Commit**

```bash
git add web/src/mocks/auth.ts web/src/test-utils.tsx
git commit -m "feat(web): update test infrastructure with auth provider and mock helpers"
```

---

## Task 9: Wire Up Auth in main.tsx and Integrate (Spike)

**Testing mode: Spike** -- Integration wiring is configuration.

**Files:**
- Modify: `web/src/main.tsx` (wrap App with AuthProvider)
- Modify: `web/src/App.tsx` (add auth status display for verification)

**Step 1: Update main.tsx to use AuthProvider**

Update `web/src/main.tsx`:

```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from './features/auth/AuthContext'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <App />
    </AuthProvider>
  </StrictMode>,
)
```

**Step 2: Verify build and tests pass**

Run:
```bash
cd web && npx tsc -b && npx vitest run
```
Expected: Build succeeds and all tests pass.

**Step 3: Commit**

```bash
git add web/src/main.tsx
git commit -m "feat(web): wire AuthProvider into application root"
```

---

## Task 10: Add AzureAd Configuration to appsettings (Config)

**Files:**
- Modify: `api/src/Web/appsettings.json`
- Modify: `api/src/Web/appsettings.Development.json`

**Step 1: Add AzureAd section to appsettings.json**

Add to `api/src/Web/appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "placeholder.onmicrosoft.com",
    "TenantId": "placeholder-tenant-id",
    "ClientId": "placeholder-client-id",
    "Audience": "api://placeholder-client-id"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

**Step 2: Keep appsettings.Development.json unchanged**

The dev auth handler bypasses JWT validation entirely in Development, so the AzureAd config is ignored. No changes needed.

**Step 3: Commit**

```bash
git add api/src/Web/appsettings.json
git commit -m "feat(api): add placeholder AzureAd configuration for JWT bearer auth"
```

---

## Task 11: Full Verification (All ACs)

**Step 1: Run all backend tests**

Run:
```bash
cd api && dotnet build && dotnet test --no-restore
```
Expected: All tests pass, build succeeds.

**Step 2: Run all frontend tests**

Run:
```bash
cd web && npx vitest run
```
Expected: All tests pass.

**Step 3: Run frontend lint and format check**

Run:
```bash
cd web && npm run lint && npm run format:check
```
Expected: No errors.

**Step 4: Run frontend build**

Run:
```bash
cd web && npx tsc -b && npx vite build
```
Expected: Build succeeds.

**Step 5: Run backend format check**

Run:
```bash
cd api && dotnet format --verify-no-changes
```
Expected: No formatting issues.

**Step 6: Verify AC checklist**

- [ ] AC1 (Unauthenticated redirect): AuthProvider + LoginRedirect + MsalAuthProvider handle redirect flow. Dev mode uses DevAuthProvider.
- [ ] AC2 (Authenticated access): httpClient attaches tokens. AuthContext provides auth state.
- [ ] AC3 (Sign out): AuthContext exposes signOut(). Dev mode clears localStorage.
- [ ] AC4 (Backend 401 enforcement): FallbackPolicy requires authenticated users on all endpoints. Test: `Request_WithoutAuthHeaders_Returns401`.
- [ ] AC5 (Frontend 401 handling): httpClient handleResponse throws AuthError on 401.
- [ ] AC6 (Noindex enforcement): NoindexMiddleware adds X-Robots-Tag header. Meta tag in index.html. Test: `Response_AlwaysIncludesNoindexHeader`.
- [ ] AC7 (Dev auth mode): DevAuthProvider with persona switching. DevelopmentAuthenticationHandler on backend. httpClient dual path. Test: `Request_WithDevAuthHeaders_Returns200`. Test: `Request_WithDevHeaders_InProductionEnvironment_Returns401`.

---

## Summary

| Task | Testing Mode | Key Files |
|------|-------------|-----------|
| 1. MSAL packages | Spike | `msalConfig.ts`, `.env.*` |
| 2. Backend dev auth | Test-first | `DevelopmentAuthenticationConfiguration.cs`, `DevAuthenticationTests.cs` |
| 3. Identity services | Test-first | `ICurrentUserService.cs`, `ITenantContext.cs`, `CurrentUserService.cs`, `TenantContext.cs` |
| 4. Noindex headers | Test-first | `NoindexMiddleware.cs`, `index.html` |
| 5. Problem Details parser | Test-first | `problemDetails.ts` |
| 6. httpClient | Mixed | `httpClient.ts`, `httpClient.test.ts` |
| 7. Frontend auth | Test-first | `DevAuthProvider.tsx`, `AuthContext.tsx`, `LoginRedirect.tsx` |
| 8. Test infra update | Characterization | `test-utils.tsx`, `mocks/auth.ts` |
| 9. Integration wiring | Spike | `main.tsx` |
| 10. AzureAd config | Config | `appsettings.json` |
| 11. Full verification | N/A | All |
