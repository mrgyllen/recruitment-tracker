# Story 1.2: SSO Authentication

Status: ready-for-dev

## Story

As a **user**,
I want to sign in using my organizational Microsoft Entra ID account and be automatically redirected if not authenticated,
so that I can securely access the application without creating separate credentials.

## Acceptance Criteria

1. **Unauthenticated redirect:** Given a user navigates to the application URL without being authenticated, when the page loads, then the user is redirected to the Microsoft Entra ID login flow (Authorization Code + PKCE), and after successful SSO authentication, the user is redirected back to the application.

2. **Authenticated access:** Given a user is authenticated via Entra ID SSO, when the application loads, then the user sees the application content (not a login page) and the JWT access token is attached to all API requests via `httpClient.ts`.

3. **Sign out:** Given a user is authenticated, when they click the sign out action, then the MSAL session is cleared and the user is redirected to the login flow.

4. **Backend 401 enforcement:** Given an API request is made without a valid JWT token, when the backend receives the request, then it returns 401 Unauthorized.

5. **Frontend 401 handling:** Given the frontend receives a 401 response, when `httpClient.ts` handles the response, then the user is redirected to the Entra ID login flow.

6. **Noindex enforcement:** Given the application is deployed, when any page is served, then the response includes `X-Robots-Tag: noindex` header and the HTML includes a `<meta name="robots" content="noindex">` tag.

7. **Dev auth mode:** Given `VITE_AUTH_MODE=development` (frontend) and `ASPNETCORE_ENVIRONMENT=Development` (backend), when the app loads, then a dev toolbar is visible allowing the user to switch between preconfigured user personas (User A, User B, Admin/Service, Unauthenticated) without Entra ID, and the selected identity flows through `httpClient.ts` and `ITenantContext` identically to real SSO auth.

## Tasks / Subtasks

- [ ] **Task 1: Install and configure MSAL packages** (AC: 1, 2)
  - [ ] Install `@azure/msal-browser@5.1.0` and `@azure/msal-react@5.0.2` in `web/`
  - [ ] Create MSAL configuration in `web/src/features/auth/msalConfig.ts` with Entra ID settings (clientId, authority, redirectUri from environment variables). **This file also creates and exports the `msalInstance`** — it is the single source of the MSAL PublicClientApplication, imported by both `AuthContext.tsx` and `httpClient.ts` to avoid circular dependencies.
  - [ ] Create `.env.example` documenting `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_AUTHORITY`, `VITE_ENTRA_REDIRECT_URI`, `VITE_AUTH_MODE`
  - [ ] Create `.env.development` with `VITE_AUTH_MODE=development` and placeholder Entra values
  - [ ] **Testing mode: Spike** — MSAL configuration is declarative; verify it initializes without errors.

- [ ] **Task 2: Create dev auth bypass** (AC: 7) — *depends on Task 1*
  - [ ] **Backend:** Create `api/src/Web/Configuration/DevelopmentAuthenticationConfiguration.cs` — registers a custom `AuthenticationHandler<T>` that reads `X-Dev-User-Id` and `X-Dev-User-Name` headers, builds a `ClaimsPrincipal`. Registered ONLY inside `if (app.Environment.IsDevelopment())` using runtime `IHostEnvironment.IsDevelopment()` check.
  - [ ] **Frontend:** Create `web/src/features/auth/DevAuthProvider.tsx` — replaces `MsalProvider` when `VITE_AUTH_MODE=development`. Renders a floating dev toolbar (position: fixed, bottom-right, z-9999) with user persona dropdown. Preconfigured personas: User A (id: `dev-user-a`, name: "Alice Dev"), User B (id: `dev-user-b`, name: "Bob Dev"), Admin/Service (id: `dev-admin`, name: "Admin Dev"), Unauthenticated (no identity). Stores selection in `localStorage`.
  - [ ] **Frontend:** Create `web/src/features/auth/authProvider.ts` — factory that returns `MsalProvider` or `DevAuthProvider` based on `import.meta.env.VITE_AUTH_MODE`.
  - [ ] **httpClient integration:** In `getAuthHeaders()`, when `VITE_AUTH_MODE === 'development'`, skip `acquireTokenSilent()` and attach `X-Dev-User-Id` / `X-Dev-User-Name` headers from dev context instead.
  - [ ] Dev toolbar must be **visually obvious** — bright colored banner/badge that clearly indicates "DEV MODE" to prevent confusion with real auth.
  - [ ] **Testing mode: Test-first** — Test that DevelopmentAuthenticationHandler populates ClaimsPrincipal from headers. Test that DevAuthProvider switches personas and updates context. Test that httpClient sends correct headers in dev mode.

- [ ] **Task 3: Create AuthContext and auth components** (AC: 1, 2, 3) — *depends on Tasks 1, 2*
  - [ ] Create `web/src/features/auth/AuthContext.tsx` — wraps MSAL's `useMsal` hook, provides auth state to the app. Imports `msalInstance` from `msalConfig.ts` (NOT the other way around — avoid circular deps). Exports `useAuth()` hook for components to access auth state (user info, isAuthenticated, signOut).
  - [ ] Create `web/src/features/auth/LoginRedirect.tsx` — handles the OIDC redirect callback flow
  - [ ] Create `web/src/features/auth/AuthContext.test.tsx` — test that AuthContext provides expected auth state
  - [ ] **Note:** `ProtectedRoute.tsx` is deferred to Story 1.5 (App Shell) when React Router is installed. This story provides auth state and token management; route-level guards require routing infrastructure.
  - [ ] **Testing mode: Test-first** — AuthContext is business logic (provides auth state to the app). Write tests first for: renders children when authenticated, redirects when unauthenticated, provides user info, handles sign-out.

- [ ] **Task 4: Create httpClient with auth token attachment** (AC: 2, 5, 7) — *depends on Tasks 1, 2*
  - [ ] Create `web/src/lib/api/httpClient.ts` with `apiGet<T>()`, `apiPost<T>()`, `apiPut<T>()`, `apiDelete<T>()` functions
  - [ ] Implement `getAuthHeaders()` with dual path: production mode acquires token via `msalInstance.acquireTokenSilent()` with scope `api://<client-id>/.default`; dev mode reads from dev auth context and sends `X-Dev-User-*` headers
  - [ ] Implement `handleResponse<T>()` with 401 detection → redirect to login (via MSAL or dev toolbar depending on mode), and Problem Details parsing for other errors
  - [ ] **Test silent token renewal failure:** When `acquireTokenSilent()` throws `InteractionRequiredAuthError`, httpClient must fall back to `loginRedirect()`. This is a distinct test case from a 401 response.
  - [ ] Create `web/src/lib/utils/problemDetails.ts` — RFC 9457 Problem Details parser
  - [ ] Create `web/src/lib/api/httpClient.test.ts` — test token attachment, 401 redirect, Problem Details parsing, **silent token failure fallback**, dev-mode header attachment
  - [ ] **Testing mode: Mixed** — Test-first for auth header logic (dev-mode headers, error parsing, 401 redirect, Problem Details parsing) since these are clear, testable behaviors. Spike for MSAL `acquireTokenSilent()` integration (external API with complex mock requirements). Add characterization tests after implementation to verify the MSAL integration path works with the mock MSAL instance.

- [ ] **Task 5: Configure backend JWT bearer authentication** (AC: 4) — *depends on Task 2 (backend dev auth)*
  - [ ] **Check template first:** Review `api/src/Web/DependencyInjection.cs` and `Program.cs` from the Clean Architecture template for existing authentication registration patterns. If the template provides auth extension methods, extend them rather than creating new ones.
  - [ ] Install `Microsoft.Identity.Web` v4.3.0 in `api/src/Web/` project
  - [ ] Create `api/src/Web/Configuration/AuthenticationConfiguration.cs` — extension method to configure JWT bearer auth with Entra ID (or extend template's existing pattern)
  - [ ] Add Entra ID configuration section to `api/src/Web/appsettings.json` and `appsettings.Development.json` (use placeholder values in Development — the dev auth handler ignores AzureAd config)
  - [ ] Register authentication in `Program.cs` via `services.AddAuthentication().AddMicrosoftIdentityWebApi()`
  - [ ] **Add global authorization policy** — configure `FallbackPolicy` to require authenticated users on all endpoints by default. No `[Authorize]` attributes needed on individual endpoints. This is how AC #4 (401 enforcement) works.
  - [ ] **Middleware registration order in `Program.cs`** (order matters):
    1. Exception handling middleware (template default)
    2. `app.UseAuthentication()`
    3. `app.UseAuthorization()`
    4. Noindex middleware (Task 7)
    5. Endpoint mapping
  - [ ] **Testing mode: Test-first** — Create `api/tests/Application.FunctionalTests/Authentication/JwtAuthenticationTests.cs`. Test 1: request without auth header → 401. Test 2: request with dev auth headers (reuse `DevelopmentAuthenticationHandler` pattern) → 200. **Test 3: dev auth handler inactive in production** — configure `CustomWebApplicationFactory` with `ASPNETCORE_ENVIRONMENT=Production`, send request with `X-Dev-User-Id`/`X-Dev-User-Name` headers → must return 401 (not 200). This verifies the `IsDevelopment()` guard prevents dev auth bypass from leaking into production. Use `CustomWebApplicationFactory` which can register the dev auth handler for test scenarios, avoiding the need to generate real JWTs in tests.

- [ ] **Task 6: Create identity and tenant context services** (AC: 4, 7) — *depends on Task 5*
  - [ ] Create `api/src/Application/Common/Interfaces/ICurrentUserService.cs` — application-layer interface with `UserId` property (the Clean Architecture boundary for user identity)
  - [ ] Create `api/src/Application/Common/Interfaces/ITenantContext.cs` — interface with `UserId`, `RecruitmentId`, `IsServiceContext`
  - [ ] Create `api/src/Infrastructure/Identity/CurrentUserService.cs` — implementation that extracts user ID from `HttpContext.User.Claims` via `IHttpContextAccessor`. This is the low-level claim reader.
  - [ ] Create `api/src/Infrastructure/Identity/TenantContext.cs` — implements `ITenantContext`. Internally wraps `ICurrentUserService` to get `UserId`. Provides settable `RecruitmentId` (for import service context, story 3.x) and `IsServiceContext` (for GDPR service context, story 6.x). These remain default/false in this story.
  - [ ] Register both as **scoped** services in DI container (`ICurrentUserService` → `CurrentUserService`, `ITenantContext` → `TenantContext`)
  - [ ] **Relationship:** `CurrentUserService` reads claims from `HttpContext`. `TenantContext` wraps `CurrentUserService` for `UserId` and adds the other context fields. No middleware needed — extraction happens lazily via DI when services are resolved during a request.
  - [ ] **Testing mode: Test-first** — Unit test that `CurrentUserService` extracts user ID from `HttpContext.User.Claims`. Unit test that `TenantContext.UserId` delegates to `ICurrentUserService`. Unit test that `TenantContext` returns default values for `RecruitmentId` (null) and `IsServiceContext` (false).

- [ ] **Task 7: Configure noindex headers** (AC: 6)
  - [ ] Add middleware in backend to set `X-Robots-Tag: noindex` response header on all responses
  - [ ] Add `<meta name="robots" content="noindex">` to `web/index.html`
  - [ ] **Testing mode: Test-first** — Integration test that verifies response header is present on API responses.

- [ ] **Task 8: Update test infrastructure for auth** (AC: all) — *depends on Tasks 2, 3*
  - [ ] Update `web/src/test-utils.tsx` to wrap renders with `MsalProvider` using a mock MSAL instance
  - [ ] Create `web/src/mocks/auth.ts` with mock MSAL instance and helper functions for test scenarios (authenticated, unauthenticated)
  - [ ] Add MSW handlers in `web/src/mocks/handlers.ts` for auth-related API responses
  - [ ] Verify all existing tests still pass with auth wrapping
  - [ ] **Testing mode: Characterization** — Extending existing test infrastructure to support auth context. Tests verify the mock setup works correctly.

- [ ] **Task 9: End-to-end verification** (AC: 1–7) — *depends on Tasks 1–8*
  - [ ] **Dev mode E2E (no Entra tenant required):** Start both backend and frontend in development mode. Verify: dev toolbar appears, persona switching works, selected persona identity flows through API requests via `X-Dev-User-*` headers, switching to "Unauthenticated" triggers 401 from backend, httpClient redirects appropriately.
  - [ ] **Production mode verification (requires Entra tenant):** If a configured Entra ID tenant is available, verify: unauthenticated → redirect → authenticate → access app, sign-out clears session, 401 handling works end-to-end. If no tenant available, document this as a deferred verification step.
  - [ ] Verify noindex header and meta tag are present in both modes
  - [ ] Run full CI pipeline (`dotnet build`, `dotnet test`, `npm run lint`, `npm run build`, `npm run test`)
  - [ ] **Testing mode: N/A** — Manual verification + CI pipeline confirmation. Dev mode enables full E2E testing without external dependencies.

## Dev Notes

- **Affected aggregate(s):** None directly — this is authentication infrastructure. However, this story creates `ITenantContext` and `ICurrentUserService` which are consumed by all aggregates (Recruitment, Candidate, ImportSession) in later stories.
- **Cross-aggregate impact:** `ITenantContext` becomes the single data isolation mechanism. Its design here determines how all future queries are filtered.

### Critical Architecture Constraints

**IMPORTANT: Story 1-1 (scaffolding) must be completed first.** This story assumes `api/` and `web/` directory structures exist with the Clean Architecture template and Vite+React setup.

**Backend (.NET 10):**
- **Auth library:** `Microsoft.Identity.Web` v4.3.0 — handles JWT bearer validation, token extraction, Entra ID integration. Do NOT use raw `JwtBearerHandler` configuration manually.
- **Template context:** Jason Taylor Clean Architecture template provides `DependencyInjection.cs` extension patterns, `Program.cs` structure, and test projects (`CustomWebApplicationFactory`). Follow these existing patterns.
- **Mocking:** NSubstitute is MANDATORY — do NOT use Moq.
- **Middleware pipeline order:** `UseAuthentication()` → `UseAuthorization()` → endpoint mapping. Order matters. (No custom TenantContext middleware — tenant resolution happens lazily via DI when `ITenantContext` is resolved.)
- **ITenantContext pattern:** Three usage contexts — web requests (UserId from JWT), import service (RecruitmentId), GDPR service (IsServiceContext = true). Only the web request context is implemented in this story; the others are just interface properties. **This story creates the contracts and middleware skeleton only** — no EF Core global query filters or database integration. Nothing reads from `ITenantContext` yet; that wiring happens in story 1.3.
- **Dev auth bypass:** `DevelopmentAuthenticationConfiguration.cs` registers a custom handler ONLY inside `IHostEnvironment.IsDevelopment()` runtime check. This handler reads `X-Dev-User-Id`/`X-Dev-User-Name` headers and populates `ClaimsPrincipal` identically to how real JWT auth would. Downstream services (`CurrentUserService`, `TenantContext`) don't know or care which auth handler ran — they just read claims.
- **Architecture doc alignment note:** The dev auth mode is a pragmatic development enabler introduced at story level to prevent external Entra ID dependency from blocking development and testing. It is not yet documented in the architecture doc. After this story is implemented, the architecture doc should be updated with a "Development Patterns" section covering the dev auth bypass. This is a follow-up task, not a blocker for implementation.

**Frontend (React 19):**
- **MSAL packages:** `@azure/msal-browser@5.1.0` + `@azure/msal-react@5.0.2` — these are the latest stable v5 releases (Jan 2026). v5 uses Auth Code + PKCE by default.
- **MSAL v5 breaking changes from v4:** Removed `encodeExtraQueryParams` config, removed all requested claims references, refactored event types and `InteractionStatus`. If referencing v4 examples online, be aware of these changes.
- **httpClient.ts is the SINGLE HTTP entry point** — auth token, error handling, 401 redirect all in ONE place. API modules (`recruitments.ts`, `candidates.ts`, etc.) import from httpClient, NEVER call `fetch` directly.
- **Token acquisition:** Always try `acquireTokenSilent()` first. Only fall back to `loginRedirect()` on failure (`InteractionRequiredAuthError`). This silent-token-failure → redirect path must be explicitly tested.
- **Scope:** `api://<client-id>/.default` — single scope for the API.
- **`msalInstance` lives in `msalConfig.ts`**, NOT in `AuthContext.tsx`. Both `AuthContext.tsx` and `httpClient.ts` import from `msalConfig.ts`. This avoids a circular dependency: `httpClient → AuthContext → (components that use httpClient)`.
- **Dev auth mode:** `httpClient.ts` has a dual path in `getAuthHeaders()` — production path uses `acquireTokenSilent()`, dev path reads from dev auth context and sends `X-Dev-User-*` headers. Same function signature, different implementation branch based on `VITE_AUTH_MODE`.

**Configuration:**
- Backend: `appsettings.json` → `AzureAd` section with `Instance`, `Domain`, `TenantId`, `ClientId`, `Audience`
- Frontend: Vite env vars with `VITE_` prefix → `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_AUTHORITY`, `VITE_ENTRA_REDIRECT_URI`, `VITE_AUTH_MODE` (values: `production` | `development`)
- Secrets: Never commit real client IDs or tenant IDs. Use `.env.development` with placeholders, `.env.example` for documentation.

### File Structure (What This Story Creates)

```
web/src/
  features/
    auth/
      msalConfig.ts              # MSAL configuration + msalInstance (single source)
      AuthContext.tsx             # Wraps useMsal, provides auth state
      AuthContext.test.tsx        # Auth state tests
      LoginRedirect.tsx           # OIDC redirect handler
      DevAuthProvider.tsx         # Dev-mode auth with persona switching toolbar
      authProvider.ts            # Factory: returns MsalProvider or DevAuthProvider
  lib/
    api/
      httpClient.ts              # fetch wrapper + auth + Problem Details + 401
      httpClient.test.ts         # httpClient tests
    utils/
      problemDetails.ts          # RFC 9457 parser
  mocks/
    auth.ts                      # Mock MSAL instance for tests

api/src/
  Web/
    Configuration/
      AuthenticationConfiguration.cs          # JWT bearer + Entra ID setup (production)
      DevelopmentAuthenticationConfiguration.cs  # Dev-mode auth handler (X-Dev-User-* headers)
  Application/
    Common/
      Interfaces/
        ICurrentUserService.cs         # User identity contract
        ITenantContext.cs              # Tenant isolation contract
  Infrastructure/
    Identity/
      CurrentUserService.cs            # Extracts user ID from JWT claims
      TenantContext.cs                 # ITenantContext implementation
```

### Code Pattern References

**httpClient.ts pattern (architecture-based, with dev auth and silent-token fallback):**
```typescript
import { msalInstance } from '../../features/auth/msalConfig';
import { InteractionRequiredAuthError } from '@azure/msal-browser';

const API_BASE = '/api';
const isDev = import.meta.env.VITE_AUTH_MODE === 'development';

async function getAuthHeaders(): Promise<HeadersInit> {
  if (isDev) {
    // Dev mode: read from localStorage dev persona, send custom headers
    const devUser = JSON.parse(localStorage.getItem('dev-auth-user') || 'null');
    if (!devUser) return { 'Content-Type': 'application/json' }; // unauthenticated
    return {
      'X-Dev-User-Id': devUser.id,
      'X-Dev-User-Name': devUser.name,
      'Content-Type': 'application/json',
    };
  }

  // Production mode: acquire token via MSAL
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) throw new AuthError('No active session');
  try {
    const { accessToken } = await msalInstance.acquireTokenSilent({
      scopes: ['api://<client-id>/.default'],
      account: accounts[0],
    });
    return { Authorization: `Bearer ${accessToken}`, 'Content-Type': 'application/json' };
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      msalInstance.loginRedirect();
      throw new AuthError('Session expired — redirecting to login');
    }
    throw error;
  }
}

export async function apiGet<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { headers: await getAuthHeaders() });
  return handleResponse<T>(res);
}
```

**AuthenticationConfiguration.cs pattern:**
```csharp
// api/src/Web/Configuration/AuthenticationConfiguration.cs
using Microsoft.Identity.Web;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddEntraIdAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
        return services;
    }
}
```

**appsettings.json AzureAd section:**
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<your-domain>.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "Audience": "api://<client-id>"
  }
}
```

### What This Story Does NOT Include

- **No domain entities** — those are story 1.3
- **No database/EF Core setup** — story 1.3
- **No UI components or design tokens** — story 1.4
- **No routing, app shell, or ProtectedRoute** — story 1.5 (route guards require React Router which isn't installed until then)
- **No EntraIdDirectoryService** — that's for team management (story 2.4), not basic auth
- **No API endpoints** — this story only sets up the auth middleware pipeline
- **No React Router** — story 1.5 (httpClient and auth work independently of routing)
- **No TanStack Query** — story 1.5
- **No shadcn/ui** — story 1.4

### Anti-Patterns to Avoid

- **Do NOT install libraries that belong to later stories** (React Router, TanStack Query, shadcn/ui, Microsoft.Graph)
- **Do NOT configure `EntraIdDirectoryService`** — that's team management, not auth
- **Do NOT use Moq** — NSubstitute is mandatory for all backend mocking
- **Do NOT call `fetch` directly** from any file other than `httpClient.ts`
- **Do NOT store tokens manually** — MSAL handles token caching and refresh
- **Do NOT create custom error response shapes** — use Problem Details (RFC 9457)
- **Do NOT bypass MSAL** for token acquisition — always use `acquireTokenSilent()` with redirect fallback
- **Do NOT hardcode Entra ID configuration** — use environment variables (frontend) and appsettings (backend)
- **Do NOT commit real client IDs, tenant IDs, or secrets** to source control
- **Do NOT use `#if DEBUG` preprocessor directives** for auth switching — use runtime `IHostEnvironment.IsDevelopment()` checks. Preprocessor directives can leak into release builds if build configurations are misconfigured.
- **Do NOT reference `DevelopmentAuthenticationHandler`** outside the Web project's DI registration, and the registration MUST be inside an `IsDevelopment()` guard — this is the safety invariant.

### Previous Story Intelligence (Story 1-1)

Story 1-1 establishes the project foundation. Key learnings for this story:
- **Project structure:** `api/` (Clean Architecture template) and `web/` (Vite + React + TypeScript)
- **Test infrastructure:** Vitest + Testing Library + MSW in frontend, xUnit + NSubstitute in backend
- **Custom test render:** `web/src/test-utils.tsx` exists — needs to be updated to include `MsalProvider`
- **MSW setup:** `web/src/mocks/server.ts` and `handlers.ts` exist — add auth-related mocks
- **CI pipeline:** `.github/workflows/ci.yml` runs both stacks — all tests must pass
- **Code quality:** ESLint + Prettier (frontend), `dotnet format` (backend)
- **Vite proxy:** `/api` → `http://localhost:5001` already configured

### Library Versions (Verified Feb 2026)

| Library | Version | Notes |
|---------|---------|-------|
| @azure/msal-browser | 5.1.0 | Latest stable, Auth Code + PKCE by default |
| @azure/msal-react | 5.0.2 | Latest stable, peer dep on msal-browser ^5.0.2 |
| Microsoft.Identity.Web | 4.3.0 | .NET 10 compatible, handles JWT bearer + Entra ID |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.2 | Bundled with .NET 10 |

### Testing: Pragmatic TDD

Per-task testing modes are declared in the task list above.

**Tests added by this story:**
- `AuthContext.test.tsx` — auth state management (test-first)
- `httpClient.test.ts` — dev-mode headers, error parsing, 401 redirect, Problem Details (test-first); MSAL `acquireTokenSilent()` path including **silent token renewal failure** (`InteractionRequiredAuthError` → `loginRedirect()`) (characterization after spike)
- `api/tests/Application.FunctionalTests/Authentication/JwtAuthenticationTests.cs` — request without auth → 401, request with dev auth headers → 200, **dev auth headers in Production environment → 401** (test-first via CustomWebApplicationFactory)
- Backend integration test for `DevelopmentAuthenticationHandler` — verifies `X-Dev-User-*` headers populate `ClaimsPrincipal` (test-first)
- `CurrentUserService` unit tests — extracts user ID from claims (test-first)
- `TenantContext` unit tests — delegates `UserId` to `ICurrentUserService`, returns defaults for other properties (test-first)
- Noindex header integration test (test-first)
- `DevAuthProvider` component test — persona switching, localStorage persistence (test-first)
- Mock auth infrastructure (`mocks/auth.ts`) with verification tests (characterization)

**Risk covered:** Authentication is the gateway to the entire application. If auth breaks, nothing works. Test-first approach ensures the auth pipeline is verified before any feature code depends on it. Dev auth mode enables full E2E testing of all subsequent stories without Entra ID tenant dependency.

### Project Structure Notes

- Auth feature files go in `web/src/features/auth/` following the feature-based organization from the architecture doc
- httpClient goes in `web/src/lib/api/` as a shared utility, not a feature
- Backend auth files follow Clean Architecture layers: interfaces in Application, implementations in Infrastructure, configuration in Web
- Middleware registration order in `Program.cs` is critical: Exception handling → Authentication → Authorization → Noindex → Endpoints

### References

- [Source: `_bmad-output/planning-artifacts/architecture.md` — Authentication Patterns, Security Patterns, Frontend Architecture, API Patterns, Project Structure]
- [Source: `_bmad-output/planning-artifacts/epics/epic-1-project-foundation-user-access.md` — Story 1.2 definition and acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` — FR1-FR3 (Authentication), NFR11 (SSO), NFR16 (noindex)]
- [Source: `_bmad-output/implementation-artifacts/1-1-project-scaffolding-ci-pipeline.md` — Previous story context]
- [Source: `docs/testing-pragmatic-tdd.md` — Testing policy]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
