# Epic 1: Project Foundation & User Access

Users can access the application through SSO and see the initial landing experience with guidance to get started.

## Story 1.1: Project Scaffolding & CI Pipeline

As a **developer**,
I want a working monorepo with backend and frontend projects, test infrastructure, and CI pipeline,
So that the team has a verified foundation to build features on.

**Acceptance Criteria:**

**Given** the repository is empty (no api/ or web/ directories)
**When** the scaffolding story is complete
**Then** the `api/` directory contains a .NET solution initialized from the Jason Taylor Clean Architecture template (`dotnet new ca-sln -o api --database SqlServer`)
**And** `dotnet build api/api.sln` succeeds with zero errors
**And** `dotnet test` passes all template default tests
**And** the `web/` directory contains a Vite + React + TypeScript app (`npm create vite@latest web -- --template react-ts`)
**And** Tailwind CSS v4 is installed and configured via `@tailwindcss/vite` plugin
**And** `npm run dev` starts the Vite dev server and `npm run build` produces `dist/`
**And** Vite proxy routes `/api/*` to the .NET backend in `vite.config.ts`
**And** Vitest + Testing Library + MSW are installed and configured
**And** `web/src/test-utils.tsx` exists with a custom render wrapping providers (QueryClient, Router)
**And** `web/src/mocks/server.ts` and `web/src/mocks/handlers.ts` exist with MSW setup
**And** ESLint + Prettier are configured with import ordering rules
**And** `.editorconfig` is configured for backend code style
**And** `.github/workflows/ci.yml` builds and tests both `api/` and `web/` on every PR
**And** the monorepo structure matches the architecture document's project tree

## Story 1.2: SSO Authentication

As a **user**,
I want to sign in using my organizational Microsoft Entra ID account and be automatically redirected if not authenticated,
So that I can securely access the application without creating separate credentials.

**Acceptance Criteria:**

**Given** a user navigates to the application URL without being authenticated
**When** the page loads
**Then** the user is redirected to the Microsoft Entra ID login flow (Authorization Code + PKCE)
**And** after successful SSO authentication, the user is redirected back to the application

**Given** a user is authenticated via Entra ID SSO
**When** the application loads
**Then** the user sees the application content (not a login page)
**And** the JWT access token is attached to all API requests via `httpClient.ts`

**Given** a user is authenticated
**When** they click the sign out action
**Then** the MSAL session is cleared and the user is redirected to the login flow

**Given** an API request is made without a valid JWT token
**When** the backend receives the request
**Then** it returns 401 Unauthorized

**Given** the frontend receives a 401 response
**When** `httpClient.ts` handles the response
**Then** the user is redirected to the Entra ID login flow

**Given** the application is deployed
**When** any page is served
**Then** the response includes `X-Robots-Tag: noindex` header
**And** the HTML includes a `<meta name="robots" content="noindex">` tag

**Technical notes:**
- Backend: ASP.NET Core JWT bearer authentication middleware, Entra ID configuration in `AuthenticationConfiguration.cs`
- Frontend: `@azure/msal-browser` + `@azure/msal-react`, `AuthContext.tsx` wrapping MSAL's `useMsal` hook, `ProtectedRoute.tsx` component, `LoginRedirect.tsx` component
- FR1, FR2, FR3 fulfilled

## Story 1.3: Core Data Model & Tenant Isolation

As a **developer**,
I want the domain entities, database configuration, and per-recruitment data isolation in place with verified security tests,
So that all subsequent feature stories can persist and query data safely within recruitment boundaries.

**Acceptance Criteria:**

**Given** the data model story is complete
**When** I inspect the Domain project
**Then** the following entities exist: `Recruitment`, `WorkflowStep`, `RecruitmentMember`, `Candidate`, `CandidateOutcome`, `CandidateDocument`, `ImportSession`, `AuditEntry`
**And** entities enforce aggregate boundaries (child entities modified only through aggregate root methods)
**And** cross-aggregate references use IDs only (no navigation properties across aggregates)

**Given** the EF Core configuration is complete
**When** I inspect the Infrastructure project
**Then** each entity has a Fluent API configuration file in `Data/Configurations/`
**And** no data annotations exist on domain entities
**And** `ApplicationDbContext` applies global query filters via `ITenantContext` on all candidate-related entities

**Given** `ITenantContext` is configured
**When** a web request is processed
**Then** `TenantContextMiddleware` populates `ITenantContext.UserId` from the authenticated JWT
**And** queries automatically filter to recruitments where the user is a member

**Given** User A is a member of Recruitment 1 but not Recruitment 2
**When** User A queries candidates
**Then** only candidates from Recruitment 1 are returned
**And** candidates from Recruitment 2 are never visible

**Given** the import service sets `ITenantContext.RecruitmentId`
**When** the import service queries or writes candidates
**Then** operations are scoped to that specific recruitment only

**Given** the GDPR service sets `ITenantContext.IsServiceContext = true`
**When** the GDPR service queries recruitments
**Then** the global query filter is bypassed and all expired recruitments are queryable

**Given** `ITenantContext` has no user, no recruitment ID, and no service flag
**When** a query is executed
**Then** zero results are returned (not an error)

**Given** the `AuditBehaviour` is registered in the MediatR pipeline
**When** any command is dispatched through MediatR
**Then** an `AuditEntry` is created capturing who performed the action, when, and what changed
**And** no PII is stored in the audit event context (IDs and metadata only)

**Technical notes:**
- Cross-recruitment isolation tests are mandatory and must pass before any feature tests
- Enums: `OutcomeStatus`, `ImportMatchConfidence`, `RecruitmentStatus`, `ImportSessionStatus`
- Value objects: `CandidateMatch`, `AnonymizationResult`
- Domain events: `CandidateImportedEvent`, `OutcomeRecordedEvent`, `DocumentUploadedEvent`, `RecruitmentCreatedEvent`, `RecruitmentClosedEvent`, `MembershipChangedEvent`
- Domain exceptions: `RecruitmentClosedException`, `DuplicateCandidateException`, `InvalidWorkflowTransitionException`, `StepHasOutcomesException`

## Story 1.4: Shared UI Components & Design Tokens

As a **developer**,
I want the design system foundation with shared UI components and If Insurance brand tokens,
So that all feature stories use consistent, accessible components from the start.

**Acceptance Criteria:**

**Given** the Tailwind CSS v4 `@theme` block is configured in `index.css`
**When** I inspect the design tokens
**Then** the If Insurance brand palette is defined: `--color-brand-brown` (#331e11), `--color-bg-base` (#faf9f7), `--color-bg-surface` (#ffffff), `--color-border-default` (#ede6e1), `--color-interactive` (#005fcc)
**And** semantic status colors are defined: `--status-pass` (#1a7d37), `--status-fail` (#c4320a), `--status-hold` (#b54708)
**And** the font stack uses Segoe UI as primary: `'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif`

**Given** shadcn/ui is initialized in the project
**When** I inspect `web/src/components/ui/`
**Then** core shadcn/ui components are installed: Button, Card, Table, Dialog, Select, Form, Input, Textarea, Toast, Badge, Tooltip, Skeleton, Separator, Collapsible, Progress, Alert, Sheet, DropdownMenu

**Given** the `StatusBadge` component exists
**When** rendered with each variant (Pass, Fail, Hold, Stale, Not Started)
**Then** each variant displays a distinctive icon + color + shape combination
**And** status is distinguishable by icon alone without color perception (WCAG compliance)
**And** each badge has an appropriate `aria-label`

**Given** the `ActionButton` component exists
**When** rendered with primary, secondary, and destructive variants
**Then** each variant follows the architecture's style rules (filled primary, outlined secondary, red destructive)

**Given** the `EmptyState` component exists
**When** rendered with heading, description, and action props
**Then** it displays the icon area, heading, description text, and a primary CTA button
**And** the heading uses the appropriate heading level

**Given** the `Toast` system exists
**When** a toast is triggered via `useToast()`
**Then** it appears in the bottom-right corner with slide-in animation (~150ms)
**And** success toasts auto-dismiss after 3 seconds
**And** error toasts persist until dismissed
**And** animations respect `prefers-reduced-motion`

**Given** the `SkeletonLoader` component exists
**When** rendered
**Then** it displays a placeholder matching the final layout shape

**Given** the `ErrorBoundary` component exists
**When** a child component throws a render error
**Then** a fallback UI is displayed instead of a crash

**Given** all shared components are tested
**When** I run `npm run test`
**Then** all component tests pass with assertions on rendering, accessibility, and interaction states

## Story 1.5: App Shell & Empty State Landing

As a **user (Erik)**,
I want to see a functional application shell with clear guidance when no recruitments exist,
So that I understand what the app does and how to get started on my first visit.

**Acceptance Criteria:**

**Given** an authenticated user opens the application
**When** the app shell loads
**Then** a fixed 48px header is visible with the app name/breadcrumb on the left and the user's name + sign out action on the right

**Given** the application has React Router configured
**When** the user navigates between routes
**Then** navigation completes in under 300ms (client-side routing, NFR1)
**And** the browser URL reflects the current view

**Given** `httpClient.ts` is configured
**When** any API module makes a request
**Then** the request includes the Bearer token from MSAL
**And** responses are parsed for Problem Details on error
**And** 401 responses trigger a redirect to the login flow

**Given** the user has no recruitments (first-time user)
**When** the home screen loads
**Then** the empty state displays: a heading ("Create your first recruitment"), a value proposition description ("Track candidates from screening to offer. Your team sees the same status without meetings."), and a prominent "Create Recruitment" CTA button
**And** the empty state serves as onboarding-quality guidance (not a generic "No data" screen)

**Given** the user's browser viewport is narrower than 1280px
**When** the page loads
**Then** a message is displayed asking the user to use a wider browser window
**And** the main application content is not rendered

**Given** the app shell is complete
**When** I inspect the page
**Then** the layout uses CSS Grid with the page-level structure: [header 48px] [main content area fills remaining viewport]

**Technical notes:**
- FR10 fulfilled (empty state guidance)
- `httpClient.ts` implements `apiGet<T>()`, `apiPost<T>()` with auth headers, Problem Details parsing, and 401 redirect
- Route definitions in `web/src/routes/index.tsx`
- NFR8: initial load target under 3 seconds

---
