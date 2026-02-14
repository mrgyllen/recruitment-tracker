# Development Authentication Patterns

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains the production authentication & security model._

## Purpose

A dev auth bypass enables full-stack development and testing without an Entra ID tenant dependency. All downstream stories can be built and tested locally using preconfigured user personas.

## Frontend — Dev Auth Mode

**Activation:** `VITE_AUTH_MODE=development`

- `DevAuthProvider` replaces `MsalProvider` when `VITE_AUTH_MODE=development`
- A floating dev toolbar allows switching between preconfigured personas: User A, User B, Admin/Service, Unauthenticated
- `httpClient.ts` has a dual path in `getAuthHeaders()` — production mode uses `acquireTokenSilent()`, dev mode reads from the dev auth context and sends `X-Dev-User-Id` / `X-Dev-User-Name` headers
- Selection persisted in `localStorage`

## Backend — Dev Auth Handler

**Activation:** `ASPNETCORE_ENVIRONMENT=Development`

- `DevelopmentAuthenticationConfiguration.cs` registers a custom `AuthenticationHandler<T>` that reads `X-Dev-User-Id` and `X-Dev-User-Name` headers and builds a `ClaimsPrincipal`
- Downstream services (`CurrentUserService`, `TenantContext`) read claims identically regardless of which auth handler ran

**Safety invariant:** The dev auth handler is registered ONLY inside an `IHostEnvironment.IsDevelopment()` runtime check. **Never use `#if DEBUG` preprocessor directives** — they can leak into release builds if build configurations are misconfigured.

## BackgroundService Authorization Pattern

Background services (`IHostedService`, `BackgroundService`) run outside the HTTP pipeline and have no `HttpContext` or JWT claims. They must set up their own `ITenantContext` explicitly.

### Pattern

1. Create a new DI scope per work item (`IServiceScopeFactory.CreateScope()`)
2. Resolve `ITenantContext` from the scope
3. Set `RecruitmentId` for recruitment-scoped operations
4. All subsequent EF Core queries within that scope will be filtered by the tenant context

```csharp
// Canonical BackgroundService authorization pattern
private async Task ProcessWorkItemAsync(WorkItem item, CancellationToken ct)
{
    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

    // Set tenant context for data isolation — this is the auth equivalent for background services
    tenantContext.RecruitmentId = item.RecruitmentId;

    // All queries in this scope are now filtered to this recruitment
    var candidates = await db.Candidates.ToListAsync(ct);
}
```

### Key Rules

- **Never share scopes** across work items — each item gets its own scope and tenant context
- **Never set `IsServiceContext = true`** unless the service genuinely needs cross-recruitment access (e.g., GDPR anonymization job)
- **No JWT validation** — background services authenticate via their deployment context (App Service managed identity, not user tokens)
- **Audit events** from background services should use the `ImportSessionId` or `ServiceName` as the performer identity, not a user ID

### Current Implementations

| Service | Auth Pattern | Scope |
|---------|-------------|-------|
| `ImportPipelineHostedService` | `tenantContext.RecruitmentId = request.RecruitmentId` | Per-import recruitment |
| GDPR Retention Job (future) | `tenantContext.IsServiceContext = true` | Cross-recruitment |

## Preconfigured Personas

Personas map to security test scenarios:

- **User A / User B** — cross-recruitment isolation testing
- **Admin/Service** — service context bypass testing
- **Unauthenticated** — 401 enforcement testing
