# App Shell & Empty State Landing — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create the application shell (header, layout, routing, auth guard, viewport guard) and the empty-state landing page for first-time users.

**Architecture:** React Router v7 (declarative SPA mode) with `createBrowserRouter` + `RouterProvider`. TanStack Query v5 for server-state management. CSS Grid layout with 48px header + flex main area. ViewportGuard blocks rendering below 1280px. ProtectedRoute integrates with existing AuthContext.

**Tech Stack:** React 19, TypeScript, React Router v7, TanStack Query v5, Tailwind CSS v4, Vitest, Testing Library

---

### Task 1: Install React Router v7 and TanStack Query v5

**Files:**
- Modify: `web/package.json`

**Step 1: Install packages**

Run:
```bash
cd /home/thomasg/Projects/Web/recruitment-tracker-exp4-opus/web && npm install react-router @tanstack/react-query @tanstack/react-query-devtools
```

**Step 2: Verify imports resolve**

Run:
```bash
cd /home/thomasg/Projects/Web/recruitment-tracker-exp4-opus/web && npx tsc --noEmit 2>&1 | head -20
```
Expected: No errors related to the new packages.

**Step 3: Commit**

```bash
git add web/package.json web/package-lock.json
git commit -m "feat(1.5): install react-router v7 and tanstack query v5"
```

**Testing mode: Spike** — Package installation only.

---

### Task 2: Create QueryClient configuration

**Files:**
- Create: `web/src/lib/queryClient.ts`

**Step 1: Create the QueryClient module**

```typescript
// web/src/lib/queryClient.ts
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 3,
      refetchOnWindowFocus: true,
    },
    mutations: {
      retry: false,
    },
  },
})
```

**Step 2: Verify TypeScript compiles**

Run: `cd web && npx tsc --noEmit`
Expected: No errors.

**Step 3: Commit**

```bash
git add web/src/lib/queryClient.ts
git commit -m "feat(1.5): configure TanStack Query client with defaults"
```

**Testing mode: Spike** — Configuration only; verified through downstream integration.

---

### Task 3: Create ProtectedRoute component (test-first)

**Files:**
- Create: `web/src/features/auth/ProtectedRoute.tsx`
- Create: `web/src/features/auth/ProtectedRoute.test.tsx`

**Step 1: Write failing tests**

```typescript
// web/src/features/auth/ProtectedRoute.test.tsx
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { AuthProvider } from './AuthContext'

// Renders a router with ProtectedRoute wrapping a child route
function renderWithAuth(initialEntry: string) {
  // Import lazily to ensure module is loaded after setup
  // Use dynamic import pattern for the component
}

describe('ProtectedRoute', () => {
  it('renders child route when user is authenticated', () => {
    // DevAuthProvider defaults to dev-user-a (authenticated)
    // Should render the child content
  })

  it('does not render child route when user is unauthenticated', () => {
    // Set localStorage to unauthenticated state
    // Should NOT render child content
    // Should show some redirect/login indication
  })
})
```

The actual implementation will use `createMemoryRouter` with the ProtectedRoute as a layout route wrapping child routes. In dev mode, the authenticated state comes from `DevAuthProvider` which defaults to `dev-user-a`.

**Step 2: Implement ProtectedRoute**

```typescript
// web/src/features/auth/ProtectedRoute.tsx
import { Navigate, Outlet } from 'react-router'
import { useAuth } from './AuthContext'

export function ProtectedRoute() {
  const { isAuthenticated } = useAuth()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
```

Note: In dev mode, unauthenticated redirects to `/login`. In production, MSAL's `loginRedirect()` would handle this. For now we use `<Navigate to="/login">` as a simple redirect pattern.

**Step 3: Run tests, verify pass**

Run: `cd web && npx vitest run src/features/auth/ProtectedRoute.test.tsx`

**Step 4: Commit**

```bash
git add web/src/features/auth/ProtectedRoute.tsx web/src/features/auth/ProtectedRoute.test.tsx
git commit -m "feat(1.5): add ProtectedRoute auth guard with tests"
```

**Testing mode: Test-first** — Auth guard is security-critical.

---

### Task 4: Create ViewportGuard component (test-first)

**Files:**
- Create: `web/src/components/ViewportGuard.tsx`
- Create: `web/src/components/ViewportGuard.test.tsx`

**Step 1: Write failing tests**

Tests for:
- Renders children when viewport >= 1280px
- Shows alert message when viewport < 1280px
- Message has `role="alert"` and `aria-live="assertive"`
- Children are NOT rendered when viewport < 1280px

Use `window.matchMedia` mock to control viewport state in tests.

**Step 2: Implement ViewportGuard**

```typescript
// web/src/components/ViewportGuard.tsx
import { useEffect, useState, type ReactNode } from 'react'

const MIN_WIDTH = 1280
const MEDIA_QUERY = `(min-width: ${MIN_WIDTH}px)`

export function ViewportGuard({ children }: { children: ReactNode }) {
  const [isWideEnough, setIsWideEnough] = useState(() =>
    window.matchMedia(MEDIA_QUERY).matches
  )

  useEffect(() => {
    const mql = window.matchMedia(MEDIA_QUERY)
    const handler = (e: MediaQueryListEvent) => setIsWideEnough(e.matches)
    mql.addEventListener('change', handler)
    return () => mql.removeEventListener('change', handler)
  }, [])

  if (!isWideEnough) {
    return (
      <div className="flex h-screen items-center justify-center p-8 text-center">
        <p role="alert" aria-live="assertive" className="max-w-md text-brand-brown">
          This application is designed for desktop browsers (1280px or wider).
          Please use a wider browser window.
        </p>
      </div>
    )
  }

  return <>{children}</>
}
```

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git add web/src/components/ViewportGuard.tsx web/src/components/ViewportGuard.test.tsx
git commit -m "feat(1.5): add ViewportGuard with 1280px minimum and a11y"
```

**Testing mode: Test-first** — Accessibility-critical.

---

### Task 5: Create AppHeader component (test-first)

**Files:**
- Create: `web/src/components/AppHeader.tsx`
- Create: `web/src/components/AppHeader.test.tsx`

**Step 1: Write failing tests**

Tests for:
- Renders "Recruitment Tracker" app name
- Renders user display name from AuthContext
- Renders "Sign out" button
- Uses `<header>` semantic element
- Skip-to-content link is in the DOM (rendered by RootLayout, but tested here for header coexistence)
- Axe accessibility check passes

**Step 2: Implement AppHeader**

```typescript
// web/src/components/AppHeader.tsx
import { useAuth } from '@/features/auth/AuthContext'
import { ActionButton } from './ActionButton'

export function AppHeader() {
  const { user, signOut } = useAuth()

  return (
    <header className="flex h-12 items-center justify-between border-b border-border-default bg-bg-surface px-4">
      <span className="text-sm font-semibold text-brand-brown">
        Recruitment Tracker
      </span>
      <div className="flex items-center gap-3">
        {user && (
          <span className="text-sm text-text-secondary">{user.name}</span>
        )}
        <ActionButton variant="secondary" onClick={signOut}>
          Sign out
        </ActionButton>
      </div>
    </header>
  )
}
```

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git add web/src/components/AppHeader.tsx web/src/components/AppHeader.test.tsx
git commit -m "feat(1.5): add AppHeader with user info and sign out"
```

**Testing mode: Test-first** — Layout is user-facing.

---

### Task 6: Create RootLayout with CSS Grid and skip-to-content

**Files:**
- Create: `web/src/routes/RootLayout.tsx`

**Step 1: Implement RootLayout**

```typescript
// web/src/routes/RootLayout.tsx
import { Outlet } from 'react-router'
import { AppHeader } from '@/components/AppHeader'
import { ViewportGuard } from '@/components/ViewportGuard'

export function RootLayout() {
  return (
    <ViewportGuard>
      <div className="grid h-screen grid-rows-[48px_1fr]">
        <a
          href="#main-content"
          className="sr-only focus:not-sr-only focus:fixed focus:left-2 focus:top-2 focus:z-50 focus:rounded focus:bg-interactive focus:px-4 focus:py-2 focus:text-white"
        >
          Skip to main content
        </a>
        <AppHeader />
        <main id="main-content" className="overflow-auto">
          <Outlet />
        </main>
      </div>
    </ViewportGuard>
  )
}
```

**Step 2: Verify TypeScript compiles**

**Step 3: Commit**

```bash
git add web/src/routes/RootLayout.tsx
git commit -m "feat(1.5): add RootLayout with CSS Grid, skip link, ViewportGuard"
```

**Testing mode: Tested through route integration tests (Task 8).**

---

### Task 7: Create HomePage with empty state (test-first)

**Files:**
- Create: `web/src/features/recruitments/pages/HomePage.tsx`
- Create: `web/src/features/recruitments/pages/HomePage.test.tsx`

**Step 1: Write failing tests**

Tests for:
- Renders heading "Create your first recruitment"
- Renders description text
- Renders "Create Recruitment" CTA button
- CTA button is clickable (shows toast "Coming in Epic 2")
- Axe accessibility check

**Step 2: Implement HomePage**

```typescript
// web/src/features/recruitments/pages/HomePage.tsx
import { EmptyState } from '@/components/EmptyState'
import { useAppToast } from '@/hooks/useAppToast'

export function HomePage() {
  const toast = useAppToast()

  return (
    <div className="flex h-full items-center justify-center">
      <EmptyState
        heading="Create your first recruitment"
        description="Track candidates from screening to offer. Your team sees the same status without meetings."
        actionLabel="Create Recruitment"
        onAction={() => toast.info('Coming in Epic 2')}
      />
    </div>
  )
}
```

**Step 3: Run tests, verify pass**

**Step 4: Commit**

```bash
git add web/src/features/recruitments/pages/HomePage.tsx web/src/features/recruitments/pages/HomePage.test.tsx
git commit -m "feat(1.5): add HomePage with onboarding empty state"
```

**Testing mode: Test-first** — Onboarding experience, exact copy matters.

---

### Task 8: Create route definitions and wire up App.tsx (test-first)

**Files:**
- Create: `web/src/routes/index.tsx`
- Modify: `web/src/App.tsx`
- Create: `web/src/routes/routes.test.tsx`

**Step 1: Write failing route tests**

Tests for:
- `/` route renders HomePage content
- Unknown route shows 404 or redirects
- Authenticated user sees content through ProtectedRoute
- Unauthenticated user does not see HomePage content

**Step 2: Create route definitions**

```typescript
// web/src/routes/index.tsx
import { createBrowserRouter } from 'react-router'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { HomePage } from '@/features/recruitments/pages/HomePage'
import { RootLayout } from './RootLayout'

export const router = createBrowserRouter([
  {
    element: <RootLayout />,
    children: [
      {
        element: <ProtectedRoute />,
        children: [
          { path: '/', element: <HomePage /> },
        ],
      },
    ],
  },
])
```

**Step 3: Rewrite App.tsx**

```typescript
// web/src/App.tsx
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { RouterProvider } from 'react-router'
import { Toaster } from '@/components/ui/sonner'
import { AuthProvider } from './features/auth/AuthContext'
import { queryClient } from './lib/queryClient'
import { router } from './routes'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
        <Toaster
          position="bottom-right"
          toastOptions={{ style: { fontFamily: 'var(--font-primary)' } }}
          visibleToasts={1}
        />
      </AuthProvider>
      {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </QueryClientProvider>
  )
}

export default App
```

**Step 4: Run tests, verify pass**

**Step 5: Commit**

```bash
git add web/src/routes/index.tsx web/src/App.tsx web/src/routes/routes.test.tsx
git commit -m "feat(1.5): wire up React Router, QueryClient, and App shell"
```

**Testing mode: Test-first** — Routing is structural.

---

### Task 9: Update test-utils.tsx with router and query providers

**Files:**
- Modify: `web/src/test-utils.tsx`

**Step 1: Update AllProviders**

Add `QueryClientProvider` (with test-specific `QueryClient` that has `retry: false`) and `MemoryRouter` from `react-router`.

```typescript
// web/src/test-utils.tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { AuthProvider } from './features/auth/AuthContext'
import type { RenderOptions } from '@testing-library/react'
import type { ReactElement } from 'react'
import { Toaster } from '@/components/ui/sonner'

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

function AllProviders({ children }: { children: React.ReactNode }) {
  const queryClient = createTestQueryClient()
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter>
          {children}
        </MemoryRouter>
        <Toaster position="bottom-right" visibleToasts={1} />
      </AuthProvider>
    </QueryClientProvider>
  )
}

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllProviders, ...options })

export * from '@testing-library/react'
export { customRender as render }
```

**Step 2: Run ALL existing tests**

Run: `cd web && npx vitest run`
Expected: All tests pass (existing + new).

**Step 3: Commit**

```bash
git add web/src/test-utils.tsx
git commit -m "feat(1.5): update test-utils with QueryClient and MemoryRouter"
```

**Testing mode: Spike** — Test infrastructure; verified by running all tests.

---

### Task 10: Final verification + lint fix + Dev Agent Record

**Step 1: Run build**

Run: `cd web && npm run build`
Expected: Zero errors.

**Step 2: Run all tests**

Run: `cd web && npx vitest run`
Expected: All tests pass.

**Step 3: Run lint**

Run: `cd web && npm run lint`
Expected: Zero violations.

**Step 4: Fix any issues found**

**Step 5: Update Dev Agent Record in story file**

**Step 6: Final commit**

```bash
git add -A
git commit -m "feat(1.5): final verification pass and dev record"
```

---

## Acceptance Criteria Traceability

| AC | Task(s) | Verification |
|----|---------|-------------|
| AC1: App shell header 48px | Task 5, 6 | Test: header renders, CSS Grid 48px row |
| AC2: React Router configured | Task 1, 8 | Test: routes render correct components |
| AC3: ProtectedRoute | Task 3 | Test: auth/unauth behavior |
| AC4: httpClient enhanced | N/A — already done | Existing tests for apiPut/apiDelete pass |
| AC5: TanStack Query configured | Task 2, 8 | QueryClientProvider in App.tsx |
| AC6: Empty state landing | Task 7 | Test: exact heading, description, CTA |
| AC7: Viewport width guard | Task 4 | Test: role="alert", content hidden below 1280px |
| AC8: CSS Grid layout | Task 6 | Test: grid-rows-[48px_1fr] class |
| AC9: Skip-to-content link | Task 6 | Test: skip link in DOM with correct href |

**Note on AC4:** The existing `httpClient.ts` already has `apiPut<T>()` and `apiDelete()` with full test coverage (3 tests in `httpClient.test.ts`). No additional work needed.
