# Story 7.1: Health Check Endpoints

Status: ready-for-dev

## Story

As a **platform operator**,
I want the API to expose health check endpoints,
So that Azure App Service can detect failures and automatically restart unhealthy instances.

## Acceptance Criteria

**AC-1: Liveness Check**
**Given** the API is running
**When** a GET request is sent to `/health`
**Then** a 200 OK response is returned confirming the process is alive (liveness check)

**AC-2: Readiness Check (Healthy)**
**Given** the API is running and the database is connected
**When** a GET request is sent to `/ready`
**Then** a 200 OK response is returned confirming all dependencies are available (readiness check)

**AC-3: Readiness Check (Unhealthy)**
**Given** the API is running but the database is unreachable
**When** a GET request is sent to `/ready`
**Then** a 503 Service Unavailable response is returned indicating the service is not ready

**AC-4: Auth Exclusion**
**Given** the health check endpoints are registered
**When** the authentication middleware processes requests to `/health` or `/ready`
**Then** both endpoints are excluded from authentication and respond without requiring a valid JWT token

**AC-5: Environment Independence**
**Given** the health check endpoints exist
**When** the application starts in any environment (local, staging, production)
**Then** both endpoints respond correctly regardless of environment configuration

## Tasks / Subtasks

### Task 1: Replace `UseHealthChecks` Middleware with `MapHealthChecks` Endpoint Routing (AC-1, AC-4)

**What:** The current `Program.cs` uses `app.UseHealthChecks("/health")` which is middleware-based and does not support tag filtering or per-endpoint auth configuration. Replace it with `app.MapHealthChecks(...)` endpoint routing to enable separate liveness/readiness endpoints with different health check sets.

**File:** `api/src/Web/Program.cs`

**Current code (line 25):**
```csharp
app.UseHealthChecks("/health");
```

**Replace with (after `app.MapEndpoints()`):**
```csharp
// Health check endpoints — anonymous, no auth required
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false  // No checks — liveness only (process alive)
}).AllowAnonymous();

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();
```

**Why after `MapEndpoints()`:** Health check endpoints are infrastructure concerns, not feature endpoints. Placing them after `MapEndpoints()` keeps the separation clear. The `.AllowAnonymous()` call overrides the fallback authorization policy (which requires authenticated users — see `DependencyInjection.cs` line 45-48).

**Required using:**
```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
```

**Important:** Remove the `app.UseHealthChecks("/health");` line entirely. Do NOT keep both middleware and endpoint routing for health checks — they would conflict.

### Task 2: Tag the Existing DbContext Health Check as "ready" (AC-2, AC-3)

**What:** The existing `AddDbContextCheck<ApplicationDbContext>()` in `DependencyInjection.cs` does not have tags. Add the `"ready"` tag so it is included in the `/ready` endpoint but excluded from `/health` (liveness).

**File:** `api/src/Web/DependencyInjection.cs`

**Current code (lines 29-30):**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();
```

**Replace with:**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: ["ready"]);
```

**Why this is sufficient:** `AddDbContextCheck<T>()` already does everything `DbHealthCheck` would do — it calls `context.Database.CanConnectAsync()` under the hood. There is no need to write a custom `IHealthCheck` class. The built-in check from `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (already in `Web.csproj` and `Directory.Packages.props`) handles:
- Successful connection -> `HealthCheckResult.Healthy()`
- Failed connection -> `HealthCheckResult.Unhealthy()` (triggers 503 via default status code mapping)

### Task 3: Write Functional Tests (AC-1 through AC-5)

**Testing mode: Test-first.** These are infrastructure endpoints with clear, binary behavior (200 or 503). Write the tests before modifying Program.cs.

**File to create:** `api/tests/Application.FunctionalTests/Endpoints/HealthCheckEndpointTests.cs`

**Test class pattern:** Follow the same self-contained `WebApplicationFactory<Program>` pattern used in `DevAuthenticationTests.cs` — create a local factory per test fixture. Do NOT use `BaseTestFixture` or `Testing.cs` helpers since health checks do not need MediatR or database reset infrastructure.

**Namespace:** `api.Application.FunctionalTests.Endpoints`

**Tests to write:**

| Test Name | Covers | Asserts |
|-----------|--------|---------|
| `HealthEndpoint_Returns200_WhenApiIsRunning` | AC-1 | `GET /health` -> 200 OK |
| `HealthEndpoint_Returns200_WithoutAuthHeaders` | AC-4 | `GET /health` without any auth headers -> 200 OK (not 401) |
| `ReadyEndpoint_Returns200_WhenDatabaseIsConnected` | AC-2 | `GET /ready` -> 200 OK (using Testcontainers database) |
| `ReadyEndpoint_Returns200_WithoutAuthHeaders` | AC-4 | `GET /ready` without any auth headers -> 200 OK (not 401) |
| `ReadyEndpoint_Returns503_WhenDatabaseIsUnreachable` | AC-3 | `GET /ready` with invalid connection string -> 503 |
| `HealthEndpoint_Returns200_InProductionEnvironment` | AC-5 | `GET /health` with `UseEnvironment("Production")` -> 200 OK |
| `ReadyEndpoint_Returns200_InProductionEnvironment` | AC-5 | `GET /ready` with `UseEnvironment("Production")` and valid DB -> 200 OK |

**Test for AC-3 (503 when DB unreachable):** Use a separate `WebApplicationFactory` with an intentionally broken connection string:

```csharp
// Example approach for the unreachable-DB test
using var factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:apiDb",
            "Server=localhost,19999;Database=nonexistent;User Id=sa;Password=fake;TrustServerCertificate=True;Connect Timeout=2");
    });

var client = factory.CreateClient();
var response = await client.GetAsync("/ready");
response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
```

Use a short `Connect Timeout=2` so the test does not hang waiting for a connection that will never succeed.

**Test for AC-1/AC-2/AC-4/AC-5 (healthy path):** For tests that need a real database, the test can use the existing `SqlTestcontainersTestDatabase` infrastructure OR create a lightweight factory with a Development environment pointing at the Testcontainers database. The simplest approach: use the same `Testing.s_factory` exposed via a helper, or instantiate a new `CustomWebApplicationFactory` with the Testcontainers connection from the shared `Testing` class.

Recommended approach: Create a separate test fixture that stands up its own `WebApplicationFactory<Program>` with `UseEnvironment("Development")`. For tests that need a real DB (the `/ready` happy path), point at the Testcontainers SQL container from the test suite. For the `/health` endpoint, no DB is needed (liveness returns 200 regardless).

### Task 4: Verify Existing Bicep Alignment (No Code Change)

**What:** Verify that `api/infra/services/web.bicep` already sets `healthCheckPath: '/health'` (line 32). This is the path Azure App Service uses for its built-in health probe.

**Current value:** `healthCheckPath: '/health'` — already correct, aligns with the `/health` liveness endpoint.

**No change needed.** Document this alignment in a code comment or in this story's completion notes. Story 7.2 will update the Bicep templates for other concerns (runtime version, environment settings, blob storage).

### Task 5: Remove or Update the `/api/health-auth` Endpoint (Cleanup)

**What:** `Program.cs` line 37-38 has a manually-registered `/api/health-auth` endpoint used to verify that authentication is working. This endpoint was added during Epic 1 scaffolding as a diagnostic tool. With proper health checks now in place, evaluate whether to keep it.

**Decision:** Keep `/api/health-auth` for now. It serves a different purpose — it verifies the auth pipeline is working (requires a valid JWT). The `DevAuthenticationTests.cs` functional tests depend on it. This is not a health check; it is a diagnostic endpoint. No change needed unless the team lead decides to remove it.

## Dev Notes

### Architecture Patterns

**Testing strategy:** Test-first. Health check behavior is well-defined and binary (200 or 503). Write functional tests first, then make them pass.

**Pragmatic TDD mode: Test-first** — These are infrastructure endpoints with clear contracts defined by the acceptance criteria.

**Middleware pipeline order (current in `Program.cs`):**
```
1. UseHealthChecks("/health")  <-- REMOVE this
2. UseHttpsRedirection
3. UseExceptionHandler
4. UseAuthentication
5. UseAuthorization
6. UseRateLimiter
7. SecurityHeadersMiddleware
8. NoindexMiddleware
9. MapOpenApi
10. MapGet("/api/health-auth")  <-- Keep (diagnostic, different purpose)
11. MapEndpoints               <-- Feature endpoints
12. MapHealthChecks("/health")  <-- ADD here (liveness)
13. MapHealthChecks("/ready")   <-- ADD here (readiness)
```

`MapHealthChecks` is endpoint routing, so it goes with the other `Map*` calls. The `.AllowAnonymous()` call on each endpoint overrides the global fallback authorization policy set in `DependencyInjection.cs` (`SetFallbackPolicy` requires authenticated users for all endpoints by default).

**Why `.AllowAnonymous()` is required:** The project configures a fallback authorization policy via `AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()` in `DependencyInjection.cs` (line 45-48). Without `.AllowAnonymous()`, health check endpoints would require a valid JWT token, defeating their purpose for Azure App Service probes which do not authenticate.

**No custom `IHealthCheck` needed:** The built-in `AddDbContextCheck<ApplicationDbContext>()` from `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (already in the project dependencies) is sufficient. It calls `context.Database.CanConnectAsync()` — this validates TCP connectivity, authentication, and database availability. A custom `DbHealthCheck` class would be redundant.

### Guardrails

- **DO NOT** keep both `UseHealthChecks` (middleware) and `MapHealthChecks` (endpoint routing) — they would conflict. Remove the middleware call entirely.
- **DO NOT** create a custom `IHealthCheck` for the database — `AddDbContextCheck<T>()` already does this.
- **DO NOT** add health check endpoints under `/api/` prefix — they are infrastructure endpoints, not feature API endpoints. Use root paths `/health` and `/ready`.
- **DO NOT** forget `.AllowAnonymous()` — without it, the fallback auth policy blocks unauthenticated access.
- **DO** use `Predicate = _ => false` for liveness (no checks) and `Predicate = check => check.Tags.Contains("ready")` for readiness (DB check only).
- **DO** verify that `GET /health` and `GET /ready` return 200 without ANY auth headers in tests — this is the critical AC-4 validation.
- **DO** ensure the 503 test uses a short connection timeout to avoid slow tests.

### What NOT to Change

- `api/infra/services/web.bicep` — already has `healthCheckPath: '/health'`
- `api/src/Web/Web.csproj` — already has `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- `api/Directory.Packages.props` — already has the package version
- `/api/health-auth` endpoint — keep as-is (diagnostic, tested separately)
- `DependencyInjection.cs` health check registration — only change is adding `tags: ["ready"]`

### Project Structure Notes

**Files to modify:**
- `api/src/Web/Program.cs` — Remove `UseHealthChecks`, add `MapHealthChecks` with `AllowAnonymous`
- `api/src/Web/DependencyInjection.cs` — Add `tags: ["ready"]` to `AddDbContextCheck`

**Files to create:**
- `api/tests/Application.FunctionalTests/Endpoints/HealthCheckEndpointTests.cs` — Functional tests

**Files verified (no changes needed):**
- `api/src/Web/Web.csproj` — Already has health check EF Core package
- `api/Directory.Packages.props` — Already has version `10.0.0` for health checks package
- `api/infra/services/web.bicep` — Already has `healthCheckPath: '/health'` (line 32)
- `api/infra/core/host/appservice.bicep` — Passes `healthCheckPath` to App Service config (line 59)

### References

- **Architecture core:** `_bmad-output/planning-artifacts/architecture.md` — Middleware pipeline order (patterns-backend.md), auth fallback policy
- **Backend patterns:** `_bmad-output/planning-artifacts/architecture/patterns-backend.md` — Middleware Pipeline Order section
- **Testing standards:** `_bmad-output/planning-artifacts/architecture/testing-standards.md` — NUnit, FluentAssertions, WebApplicationFactory, test naming
- **Infrastructure:** `_bmad-output/planning-artifacts/architecture/infrastructure.md` — Hosting, App Service
- **Epic 7:** `_bmad-output/planning-artifacts/epics/epic-7-deployment-infrastructure-automation.md` — Story 7.1 definition
- **NFR40:** Health monitoring requirement
- **Existing test pattern:** `api/tests/Application.FunctionalTests/Authentication/DevAuthenticationTests.cs` — Self-contained WebApplicationFactory pattern
- **ASP.NET Core health checks:** `Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions` — `Predicate`, `AllowAnonymous()`, tag filtering

### Cross-Story Dependencies

- **Story 7.2 (Azure IaC):** Will update `web.bicep` for runtime version and environment settings. The `healthCheckPath: '/health'` is already correct and will not change.
- **Story 7.4 (Staging):** Health check endpoints are used to verify staging deployment is functional. Story 7.4 AC explicitly tests `/health` and `/ready` in staging.

## Dev Agent Record

### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
