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

## Preconfigured Personas

Personas map to security test scenarios:

- **User A / User B** — cross-recruitment isolation testing
- **Admin/Service** — service context bypass testing
- **Unauthenticated** — 401 enforcement testing
