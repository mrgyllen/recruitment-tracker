# Story 1.5: App Shell & Empty State Landing

Status: ready-for-dev

<!-- Validated via party-mode review 2026-02-07. Findings fixed: AC4 wording, provider placement, ViewportGuard intent, NFR verification note. -->

## Story

As a **user (Erik)**,
I want to see a functional application shell with clear guidance when no recruitments exist,
so that I understand what the app does and how to get started on my first visit.

## Acceptance Criteria

1. **App shell header:** A fixed 48px header is visible with the app name/breadcrumb on the left and the user's name + sign out action on the right. The header uses CSS Grid and remains fixed at the top of the viewport.

2. **React Router configured:** Client-side routing via React Router v7 (declarative mode). Navigation completes in under 300ms (NFR1 — inherent to client-side routing; verified manually, not in unit tests). The browser URL reflects the current view. Route definitions live in `web/src/routes/index.tsx`.

3. **ProtectedRoute component:** Routes are wrapped in a `ProtectedRoute` component that redirects unauthenticated users to the login flow. Integrates with the `AuthContext` from story 1-2.

4. **httpClient.ts enhanced:** The existing `httpClient.ts` (created in story 1-2 with `apiGet<T>()` and `apiPost<T>()`) is extended with `apiPut<T>()` and `apiDelete<T>()`. Bearer token from MSAL on all requests, Problem Details parsing on error, and 401 redirect remain unchanged. All API modules import from httpClient, never call `fetch` directly.

5. **TanStack Query configured:** `QueryClientProvider` wraps the app. Default configuration: 3 retries for GET, no retry for mutations, staleTime appropriate for the app's refresh pattern. Silent background refetch on window focus.

6. **Empty state landing:** When the user has no recruitments, the home screen displays: a heading ("Create your first recruitment"), a value proposition description ("Track candidates from screening to offer. Your team sees the same status without meetings."), and a prominent "Create Recruitment" CTA button using the `EmptyState` component from story 1-4. This is onboarding-quality guidance, not a generic "No data" screen (FR10).

7. **Viewport width guard:** When the browser viewport is narrower than 1280px, a message is displayed asking the user to use a wider browser window. The main application content is not rendered. The message uses `role="alert"` and `aria-live="assertive"` for screen reader announcement.

8. **Page-level CSS Grid layout:** The layout uses CSS Grid: `[header 48px] [main content area fills remaining viewport]`. The main content area uses `height: calc(100vh - 48px)` or equivalent grid row sizing.

9. **Skip-to-content link:** A "Skip to main content" link is present, targeting the main content area. Only visible on keyboard focus.

## Tasks / Subtasks

- [ ] **Task 1: Install React Router v7 and TanStack Query** (AC: 2, 5)
  - [ ] Install `react-router` (v7.x — single package, no `react-router-dom`)
  - [ ] Install `@tanstack/react-query` (v5.x) and `@tanstack/react-query-devtools`
  - [ ] **Testing mode: Spike** — Package installation, verify imports work.

- [ ] **Task 2: Configure React Router with declarative routing** (AC: 2)
  - [ ] Create `web/src/routes/index.tsx` with route definitions using `createBrowserRouter`
  - [ ] Define routes: `/` (home/recruitment list), `/recruitment/:id` (future), `/recruitment/:id/candidate/:id` (future — placeholder only)
  - [ ] Create `web/src/routes/RootLayout.tsx` — the root layout component wrapping `<Outlet />` with the app shell (header + main content area)
  - [ ] Update `web/src/App.tsx` to render `<RouterProvider router={router} />`
  - [ ] **Testing mode: Test-first** — Write tests for: routes render correct components, unknown routes show 404.

- [ ] **Task 3: Configure TanStack Query** (AC: 5)
  - [ ] Create `web/src/lib/queryClient.ts` — export a configured `QueryClient` instance
  - [ ] Configure defaults: `retry: 3` for queries, `retry: false` for mutations, `staleTime: 30_000` (30s), `refetchOnWindowFocus: true`
  - [ ] Wrap the app with `QueryClientProvider` in `App.tsx` (above `RouterProvider`, so all route components have access)
  - [ ] Add `ReactQueryDevtools` in development mode only
  - [ ] **Testing mode: Spike** — Configuration only. Verified through downstream integration.

- [ ] **Task 4: Create ProtectedRoute component** (AC: 3)
  - [ ] Create `web/src/features/auth/ProtectedRoute.tsx`
  - [ ] Reads auth state from `AuthContext` (story 1-2)
  - [ ] If not authenticated: redirect to login flow (via MSAL `loginRedirect()` in production, or show dev toolbar state in dev mode)
  - [ ] If authenticated: render `<Outlet />` (child routes)
  - [ ] Wrap all app routes in `ProtectedRoute` in the route configuration
  - [ ] **Testing mode: Test-first** — Write tests for: authenticated user sees content, unauthenticated user triggers redirect.

- [ ] **Task 5: Enhance httpClient.ts** (AC: 4)
  - [ ] Add `apiPut<T>()` and `apiDelete<T>()` methods to the existing `httpClient.ts`
  - [ ] Ensure Problem Details parsing is robust (handle non-JSON error responses gracefully)
  - [ ] Add `ApiError` and `AuthError` custom error classes if not already present (check story 1-2 output)
  - [ ] Export error types for consumer use
  - [ ] **Testing mode: Test-first** — Write tests for: `apiPut` sends PUT with auth headers, `apiDelete` sends DELETE with auth headers, non-JSON error responses are handled gracefully.

- [ ] **Task 6: Create App Shell layout** (AC: 1, 8, 9)
  - [ ] Create `web/src/components/AppHeader.tsx` — 48px fixed header
  - [ ] Header left: app name text "Recruitment Tracker" (plain text for now — `RecruitmentSelector` breadcrumb added in epic 2)
  - [ ] Header right: user display name from `AuthContext` + "Sign out" button (calls MSAL `logout()` / dev auth clear)
  - [ ] Apply page-level CSS Grid: `grid-template-rows: 48px 1fr` on the app container
  - [ ] Main content area: `<main>` element with `id="main-content"` for skip link target
  - [ ] Create skip-to-content link: `<a href="#main-content" class="sr-only focus:not-sr-only ...">Skip to main content</a>` positioned at top of DOM
  - [ ] Header uses `<header>` semantic element. Main content uses `<main>`.
  - [ ] **Testing mode: Test-first** — Write tests for: header renders with app name, user name displayed, sign out button present, skip link is visible on focus, CSS grid structure applied.

- [ ] **Task 7: Create viewport width guard** (AC: 7)
  - [ ] Create `web/src/components/ViewportGuard.tsx`
  - [ ] Uses `window.matchMedia('(min-width: 1280px)')` or CSS media query
  - [ ] Below 1280px: renders an accessible message with `role="alert"` and `aria-live="assertive"`: "This application is designed for desktop browsers (1280px or wider). Please use a wider browser window."
  - [ ] Below 1280px: does NOT render `children` (the app content)
  - [ ] At 1280px+: renders `children` normally
  - [ ] Wrap the app shell in `ViewportGuard` in the root layout
  - [ ] **Testing mode: Test-first** — Write tests for: content renders at 1280px, message shown below 1280px, message has role="alert".

- [ ] **Task 8: Create Home page with empty state** (AC: 6)
  - [ ] Create `web/src/features/recruitments/pages/HomePage.tsx`
  - [ ] For now (no backend API yet): render the `EmptyState` component from story 1-4 with:
    - `heading`: "Create your first recruitment"
    - `description`: "Track candidates from screening to offer. Your team sees the same status without meetings."
    - `actionLabel`: "Create Recruitment"
    - `onAction`: placeholder handler (log to console or show toast "Coming in Epic 2")
  - [ ] Register as the `/` route in the router configuration
  - [ ] **Testing mode: Test-first** — Write tests for: empty state renders with correct heading, correct description, CTA button present and clickable.

- [ ] **Task 9: Update test-utils.tsx for routing and query providers** (AC: all) — *depends on Tasks 2-5*
  - [ ] Update `web/src/test-utils.tsx` custom render to wrap with:
    - `QueryClientProvider` (with a test-specific `QueryClient` that has `retry: false`)
    - `MemoryRouter` (from `react-router`) for route-dependent tests
    - Existing providers from stories 1-2 and 1-4 (MSAL mock, Toaster)
  - [ ] Export a `renderWithRouter` helper for tests that need specific route paths
  - [ ] Verify all existing tests still pass
  - [ ] **Testing mode: Spike** — Test infrastructure update. Verified by running all existing tests.

- [ ] **Task 10: Verify build and all tests pass** (AC: all) — *depends on Tasks 1-9*
  - [ ] Run `npm run build` — zero errors
  - [ ] Run `npm run test` — all tests pass (new + existing)
  - [ ] Run `npm run lint` — zero violations
  - [ ] Visually verify in dev server: header renders, empty state shows, viewport guard works
  - [ ] **Testing mode: N/A** — Final verification.

## Dev Notes

- **Affected aggregate(s):** None — this is frontend-only. No backend API calls are made in this story (the empty state is static; the recruitments API endpoint doesn't exist yet).
- **Source tree:** All work in `web/` directory. No `api/` changes.
- **ViewportGuard placement:** `ViewportGuard` wraps the app shell in `RootLayout`, which means it runs *before* `ProtectedRoute` (auth). This is intentional — there's no point redirecting to login on a device that can't use the app. The narrow-viewport message is shown without requiring authentication.

### Critical Architecture Constraints

**IMPORTANT: Stories 1-1, 1-2, and 1-4 must be completed first.** Story 1-3 (backend data model) has no technical dependency from this story — it can run in parallel with 1-4 and 1-5 if needed. The epic ordering (1-3 before 1-5) is organizational for completeness, not a technical blocker. This story assumes:
- `web/` directory exists with Vite 7 + React 19 + TypeScript + Tailwind CSS v4 (story 1-1)
- Vitest + Testing Library + MSW configured (story 1-1)
- `web/src/test-utils.tsx` exists with custom render wrapping providers (stories 1-1, 1-2, 1-4)
- `web/src/features/auth/` directory exists with AuthContext, DevAuthProvider, msalConfig (story 1-2)
- `web/src/lib/api/httpClient.ts` exists with `apiGet<T>()`, `apiPost<T>()` (story 1-2)
- `web/src/lib/utils/problemDetails.ts` exists (story 1-2)
- `web/src/components/EmptyState.tsx` exists (story 1-4)
- `web/src/components/ErrorBoundary.tsx` exists (story 1-4)
- shadcn/ui components installed in `web/src/components/ui/` (story 1-4)
- Design tokens in `web/src/index.css` `@theme` block (story 1-4)
- Toaster provider configured (story 1-4)

### React Router v7 — Declarative Mode (SPA)

**This project uses React Router v7 in declarative/SPA mode — NOT framework mode.**

- Install: `npm install react-router` (single package — `react-router-dom` is NOT needed in v7)
- Use `createBrowserRouter()` + `<RouterProvider />` pattern
- Route definitions in `web/src/routes/index.tsx`
- Root layout component wraps `<Outlet />` with app shell
- No `react-router.config.ts`, no `root.tsx`, no `entry.client.tsx` — those are framework mode patterns
- Future flags from v6 are now defaults in v7 — no migration needed if starting fresh

**Key v7 changes from v6:**
- Single `react-router` package (replaces `react-router-dom`)
- `RouterProvider` is the primary API (not `<BrowserRouter>`)
- Data APIs (`loader`, `action`) available but optional in SPA mode — we use TanStack Query for data fetching instead
- React 19 compatible. `unstable_useTransitions` is available but NOT needed for this story.

**Route structure for this story:**

```typescript
// web/src/routes/index.tsx
import { createBrowserRouter } from 'react-router';

export const router = createBrowserRouter([
  {
    element: <RootLayout />,  // App shell: ViewportGuard + header + main
    children: [
      {
        element: <ProtectedRoute />,  // Auth guard
        children: [
          { path: '/', element: <HomePage /> },
          // Future routes added here by subsequent stories
        ],
      },
    ],
  },
]);
```

### TanStack Query v5 Setup

- Install: `npm install @tanstack/react-query @tanstack/react-query-devtools`
- v5 naming: `isPending` replaces `isLoading`, `status: 'pending'` replaces `status: 'loading'`
- React 19 gotcha: Suspense siblings now waterfall instead of fetching in parallel — not an issue for this story (no data fetching yet) but relevant for later stories
- Create `QueryClient` in a separate file (`web/src/lib/queryClient.ts`) — NOT inline in a component
- DevTools only in development: `import.meta.env.DEV`

```typescript
// web/src/lib/queryClient.ts
import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000, // 30 seconds
      retry: 3,
      refetchOnWindowFocus: true,
    },
    mutations: {
      retry: false,
    },
  },
});
```

### httpClient.ts Enhancement Notes

Story 1-2 created `httpClient.ts` with `apiGet<T>()` and `apiPost<T>()`. This story adds:
- `apiPut<T>()` — for updating resources
- `apiDelete<T>()` — for deleting resources (returns `void` or parsed response)
- Robust error handling for non-JSON error responses (e.g., 502 gateway errors that return HTML)

**Pattern to follow** (matches existing `apiGet`/`apiPost`):

```typescript
export async function apiPut<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers: await getAuthHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  return handleResponse<T>(res);
}

export async function apiDelete<T = void>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'DELETE',
    headers: await getAuthHeaders(),
  });
  if (res.status === 204) return undefined as T;
  return handleResponse<T>(res);
}
```

### App Header Design Spec

| Element | Value | Notes |
|---------|-------|-------|
| Height | 48px fixed | Compact, permanent chrome |
| Background | `--color-bg-surface` (#ffffff) | White surface, elevated |
| Border | 1px bottom border, `--color-border-default` (#ede6e1) | Subtle warm separator |
| Left content | App name "Recruitment Tracker" | Plain text, `--color-brand-brown`, semibold. RecruitmentSelector breadcrumb deferred to Epic 2. |
| Right content | User display name + "Sign out" button | Name from AuthContext. Sign out uses Ghost button variant. |
| Font | Body size (14px), semibold for app name | Consistent with type scale |
| Padding | 0 16px (horizontal) | Standard section padding |
| Semantic HTML | `<header>` element | Landmark region |

### Viewport Guard Implementation

- Use `window.matchMedia('(min-width: 1280px)')` with event listener for responsive updates
- Alternatively, CSS-only approach: render both message and content, use `@media (max-width: 1279px)` to hide content and show message, `@media (min-width: 1280px)` to show content and hide message
- The message should be centered on screen with adequate padding
- Message styling: centered text, `--color-brand-brown`, body font size
- No app chrome (header) shown when viewport is too narrow — just the message

### Empty State — First-Time User Experience (FR10)

The empty state is NOT a generic "No data" screen. It's the onboarding entry point for the application. From the UX spec:

- **Heading:** "Create your first recruitment"
- **Description:** "Track candidates from screening to offer. Your team sees the same status without meetings."
- **CTA:** "Create Recruitment" button (primary variant)
- **No illustrations or graphics** — text + CTA only (tool, not consumer app)
- The empty state uses the `EmptyState` component from story 1-4
- The CTA is a placeholder for now — actual recruitment creation is Epic 2

### Frontend Conventions (Enforce)

- Components: PascalCase (file and export) — `AppHeader.tsx`, `ProtectedRoute.tsx`
- Tests: co-located `.test.tsx` files — `AppHeader.test.tsx`
- Hooks: `use` prefix, camelCase — `useAuth.ts`
- Feature folders: kebab-case — `features/recruitments/`
- Routes: `web/src/routes/` directory (thin layer — imports page components from features)
- API modules: `web/src/lib/api/` directory
- Utilities: `web/src/lib/` directory
- Page components: `web/src/features/{feature}/pages/` directory

### Libraries Installed by This Story

| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| react-router | ^7.13.0 | Client-side routing | Single package in v7 (no react-router-dom) |
| @tanstack/react-query | ^5.90.0 | Server state management | isPending replaces isLoading in v5 |
| @tanstack/react-query-devtools | ^5.90.0 | Dev tools for React Query | Dev-only, tree-shaken in production |

### What This Story Does NOT Include

- No backend API endpoints — the empty state is rendered statically
- No recruitment creation flow (Epic 2)
- No RecruitmentSelector breadcrumb in header (Epic 2, when multiple recruitments exist)
- No overview section or collapsible overview (Epic 5)
- No three-panel screening layout (Epic 4)
- No candidate list or data fetching
- No React Router data APIs (loaders/actions) — we use TanStack Query instead
- No responsive breakpoints between 1024px-1279px (Growth scope)

### Anti-Patterns to Avoid

- **Do NOT use `react-router-dom`** — v7 consolidates into single `react-router` package
- **Do NOT use `<BrowserRouter>`** — use `createBrowserRouter` + `<RouterProvider />` instead
- **Do NOT use React Router loaders/actions for data fetching** — use TanStack Query hooks instead. Loaders/actions are a framework-mode pattern.
- **Do NOT create the `QueryClient` inline in a component** — create in a separate file and import
- **Do NOT use `isLoading` with TanStack Query v5** — use `isPending` instead
- **Do NOT call `fetch` directly from components or features** — always go through `httpClient.ts` → API module → TanStack Query hook
- **Do NOT add responsive breakpoints** — desktop-only app, 1280px minimum, show message below that
- **Do NOT use full-page spinners** — use skeleton placeholders (though this story has no async data loading)
- **Do NOT create a separate navigation component** — there are no navigation tabs. Navigation is via candidate list and overview click-through (later epics).
- **Do NOT install `react-router-dom`** — it's been merged into `react-router` in v7

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (install packages) | Spike | Package installation, verify imports |
| Task 2 (React Router) | Test-first | Routing is structural — verify correct components render for routes |
| Task 3 (TanStack Query) | Spike | Configuration only, verified through downstream use |
| Task 4 (ProtectedRoute) | Test-first | Auth guard is security-critical — must verify redirect behavior |
| Task 5 (httpClient enhance) | Test-first | HTTP methods must correctly send auth and handle errors |
| Task 6 (App Shell) | Test-first | Layout is user-facing — verify header content, semantic HTML, skip link |
| Task 7 (Viewport Guard) | Test-first | Accessibility-critical — must verify role="alert" and content hiding |
| Task 8 (Home page) | Test-first | Onboarding experience — must verify exact copy and CTA |
| Task 9 (test-utils update) | Spike | Test infrastructure, verified by running all tests |
| Task 10 (final verify) | N/A | Manual verification + CI |

**Tests added by this story:**
- `ProtectedRoute.test.tsx` — authenticated renders children, unauthenticated redirects
- `AppHeader.test.tsx` — renders app name, user name, sign out button, semantic header element
- `ViewportGuard.test.tsx` — renders children at 1280px+, shows message below, role="alert" present
- `HomePage.test.tsx` — empty state renders correct heading, description, CTA
- `httpClient.test.ts` — apiPut sends PUT, apiDelete sends DELETE, handles 204, handles non-JSON errors
- Route tests — correct components render for `/` path

**Risk covered:** The app shell is the persistent frame for every feature. If routing breaks, auth guard fails, or the header layout is wrong, every subsequent feature inherits the defect. Test-first ensures the foundation is correct.

### Previous Story Intelligence

**Story 1-1 (Project Scaffolding):**
- `web/` initialized with Vite 7 + React 19 + TypeScript
- Tailwind CSS v4 installed via `@tailwindcss/vite` — `@import "tailwindcss"` in `index.css`
- Vitest + Testing Library + MSW configured
- `web/src/test-utils.tsx` exists with custom render (wrapping providers from later stories)
- ESLint + Prettier configured with import ordering
- Vite proxy: `/api/*` routes to .NET backend in `vite.config.ts`

**Story 1-2 (SSO Authentication):**
- `web/src/features/auth/AuthContext.tsx` — wraps MSAL, provides auth state
- `web/src/features/auth/msalConfig.ts` — MSAL configuration + `msalInstance`
- `web/src/features/auth/DevAuthProvider.tsx` — dev-mode persona switching
- `web/src/features/auth/authProvider.ts` — factory returning MSAL or Dev provider
- `web/src/lib/api/httpClient.ts` — `apiGet<T>()`, `apiPost<T>()` with auth + Problem Details
- `web/src/lib/utils/problemDetails.ts` — RFC 9457 parser
- **`ProtectedRoute.tsx` was explicitly deferred to this story** — "deferred to Story 1.5 (requires React Router)"
- MSAL v5 packages installed: `@azure/msal-browser@5.1.0`, `@azure/msal-react@5.0.2`

**Story 1-3 (Core Data Model):**
- Backend-only — no frontend impact
- No frontend files created or modified

**Story 1-4 (Shared UI Components):**
- `web/src/components/EmptyState.tsx` — the component we'll use for the landing page
- `web/src/components/ErrorBoundary.tsx` — wrap routes/features in this
- `web/src/components/ActionButton.tsx` — primary/secondary/destructive button variants
- shadcn/ui components in `web/src/components/ui/`
- Design tokens configured in `web/src/index.css` `@theme` block
- `web/src/lib/utils.ts` — `cn()` utility
- Toaster provider added to app root
- `vitest-axe` installed for accessibility testing

### File Structure (What This Story Creates)

```
web/src/
  routes/
    index.tsx                    # Route definitions (createBrowserRouter)
    RootLayout.tsx               # App shell: ViewportGuard + header + main + providers
  features/
    auth/
      ProtectedRoute.tsx         # Auth guard wrapper
      ProtectedRoute.test.tsx
    recruitments/
      pages/
        HomePage.tsx             # Empty state landing page
        HomePage.test.tsx
  components/
    AppHeader.tsx                # 48px fixed header
    AppHeader.test.tsx
    ViewportGuard.tsx            # <1280px message guard
    ViewportGuard.test.tsx
  lib/
    queryClient.ts               # TanStack Query client configuration
    api/
      httpClient.ts              # Updated with apiPut, apiDelete (existing file)
      httpClient.test.ts         # Updated tests (existing file)
```

### Project Structure Notes

- Route definitions in `web/src/routes/` — thin layer importing page components from features
- Page components in `web/src/features/{feature}/pages/` — co-located with their feature
- Shared layout components (`AppHeader`, `ViewportGuard`) in `web/src/components/`
- `ProtectedRoute` in `web/src/features/auth/` — it's an auth concern
- `queryClient.ts` in `web/src/lib/` — app-wide configuration utility
- No feature-specific API modules created in this story (no data fetching)

### References

- [Source: `_bmad-output/planning-artifacts/architecture.md` (core) — Enforcement Guidelines]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` — Frontend Architecture, State Management, Folder Structure]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` — UI Consistency Rules, Empty State Pattern]
- [Source: `_bmad-output/planning-artifacts/epics/epic-1-project-foundation-user-access.md` — Story 1.5 definition and acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md` — Layout grid structure, header spec, spacing]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md` — Viewport guard spec, skip link, WCAG requirements]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md` — Navigation patterns, URL strategy, empty states]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md` — EmptyState spec, RecruitmentSelector (deferred)]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/core-user-experience.md` — J0 first-time experience, onboarding flow]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md` — J0 onboarding journey, empty state flow]
- [Source: `_bmad-output/implementation-artifacts/1-2-sso-authentication.md` — httpClient.ts pattern, AuthContext, ProtectedRoute deferral]
- [Source: `_bmad-output/implementation-artifacts/1-4-shared-ui-components-design-tokens.md` — EmptyState component, design tokens, Toaster setup]
- [Source: `docs/testing-pragmatic-tdd.md` — Testing policy]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
