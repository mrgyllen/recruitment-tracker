---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/product-brief-recruitment-tracker-2026-02-01.md
  - docs/chatgpt-research-intro.md
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-02-03'
project_name: 'recruitment-tracker'
user_name: 'MrGyllen'
date: '2026-02-02'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**

62 FRs across 10 capability areas define the MVP contract:

| Capability Area | FR Count | Architectural Weight |
|----------------|----------|---------------------|
| Authentication & Access | 3 | Low — delegated to Entra ID |
| Recruitment Lifecycle | 10 | Medium — CRUD + workflow config + close/retention |
| Team Management | 6 | Medium — membership-based access model |
| Candidate Import | 12 | **High** — XLSX parsing, matching engine, idempotent upsert, async processing |
| CV Document Management | 8 | **High** — PDF splitting, blob storage, SAS tokens, in-app viewing |
| Workflow & Outcome Tracking | 5 | Medium — configurable state machine with immutability constraints |
| Batch Screening | 6 | Medium (backend) / **High** (frontend) — split-panel UX, keyboard-first |
| Recruitment Overview & Monitoring | 5 | Medium — pre-aggregated endpoint, stale step detection |
| Audit & Compliance | 4 | Medium — append-only event log, cross-cutting |
| Workflow Defaults & Configuration | 2 | Low — deployment config |

The import pipeline (Candidate Import + CV Document Management = 20 FRs, 32% of total) is the densest area of domain logic. However, user journeys reveal that import is a **setup activity** (J0, J2), while the **daily-use paths** (J1, J3) center on the overview dashboard and batch screening.

**Non-Functional Requirements:**

34 NFRs organized by architectural impact:

- **Performance (10):** Tight response budgets — 300ms SPA nav, 500ms overview, 2s PDF load, 3s initial load. Import/split operations are async with progress. Max 15 concurrent users.
- **Security (11):** Entra ID SSO, TLS 1.2+, encryption at rest, SAS tokens for documents, GDPR retention/anonymization, immutable audit trail, file validation. No PII in logs.
- **Accessibility (8):** WCAG 2.1 AA across all core flows. Keyboard-first batch screening. ARIA live regions for dynamic content. Focus management in sequential workflows.
- **Integration (5):** Configurable XLSX column mapping, PDF bundle parsing with graceful failure, document storage separated from DB, ACID relational storage, RESTful API conventions.

**Scale & Complexity:**

- Primary domain: Full-stack web application (SPA + REST API + Azure SQL + Azure Blob Storage)
- Complexity level: **Medium** — significant domain logic in import/workflow, modest operational scale
- Concurrency: 15 users, up to 150 candidates per recruitment, PDF bundles up to 100MB
- These numbers directly justify simpler architectural patterns (monolith, not microservices)

### Architectural Principle: Read-Path vs Write-Path Optimization

The most architecturally complex subsystem (import) is the one users interact with least frequently. The simpler subsystems (overview, screening) are where users spend most of their time.

**Write path (cold — import):** Event-driven, async, tolerant of seconds-to-minutes latency. XLSX processing (NFR6: 10s), PDF splitting (NFR7: 60s), both with async fallback. Import does the heavy lifting so reads stay simple.

**Read path (hot — daily use):** Overview dashboard (NFR2: 500ms), candidate list (NFR3: 1s), PDF viewing (NFR4: 2s), outcome recording (NFR5: 500ms). These budgets are tight and intentional — the architecture must make the read path trivially fast, even if the write path does extra work to pre-compute aggregations.

### Backend Component Boundaries

| # | Component | Responsibility |
|---|-----------|---------------|
| 1 | Authentication/Authorization middleware | Entra ID integration, per-recruitment membership enforcement |
| 2 | Recruitment aggregate | Lifecycle, workflow configuration, close/retention |
| 3 | Team/Membership management | Invite, remove, membership queries |
| 4 | Import orchestration | Coordination layer for XLSX + PDF subsystems |
| 5 | XLSX parsing + candidate matching engine | Column mapping, identity matching, idempotent upsert |
| 6 | PDF splitting + document matching engine | TOC parsing, page boundaries, name-based matching |
| 7 | Document storage & access | Blob operations, SAS token generation (batch-capable) |
| 8 | Workflow state machine | Step progression, outcome recording, immutability rules |
| 9 | Audit event pipeline | Append-only event capture, cross-cutting |
| 10 | Recruitment overview/aggregation | Pre-computed dashboard data, stale step detection |

### Frontend Architectural Constraints: Batch Screening

The batch screening flow (J3) is architecturally significant enough to warrant explicit frontend constraints:

- **PDF pre-fetching:** While reviewing candidate N, candidates N+1 and N+2 load in the background. The document API should support batch SAS URL retrieval.
- **Client-side state isolation:** Candidate list, PDF viewer, and outcome form are three independent state domains. They coordinate but must not cascade re-renders to each other.
- **Focus management contract:** After outcome submission, focus MUST return to the candidate list for keyboard navigation. This is a component architecture contract, not a styling detail.
- **Optimistic outcome recording:** Outcome save should feel instant (NFR5: 500ms). Record locally, sync to API, show confirmation. Non-blocking retry on failure.

### Scope Boundaries (PRD vs Brief Alignment)

The PRD is the binding contract. The Product Brief includes features the PRD explicitly defers:

| Feature | Brief Says | PRD Says | Architecture Assumes |
|---------|-----------|----------|---------------------|
| Role-based access (Lead/Member/Read-Only) | MVP | Growth | **Accommodate** — `role` column on membership, ignored in MVP |
| Reviewer assignment / "My Assignments" | MVP | Growth | **Accommodate** — `assignedTo` on candidate-step, no UI/API |
| Candidate notes/comments | MVP | Growth | **Accommodate** — schema placeholder, no UI/API |
| Recruitment templates | MVP | Growth | Maybe — "copy recruitment" could suffice |
| SignalR live updates | Growth | Growth | **Defer** — fundamentally different plumbing |
| Word-to-PDF conversion | MVP | Growth | **Defer** — separate processing pipeline |
| Advanced reporting/export | Growth | Growth | **Defer** — different read models |

"Accommodate" = zero-cost schema/model accommodations that prevent future migrations. "Defer" = genuinely new subsystems not designed for now.

### Technical Constraints & Dependencies

| Constraint | Source | Impact |
|-----------|--------|--------|
| ASP.NET Core Web API backend | PRD / Brief | Backend technology locked |
| SPA frontend (framework TBD) | PRD | React or Vue recommended, Blazor WASM rejected |
| Azure SQL for structured data | PRD / Brief | Relational, ACID, point-in-time restore |
| Azure Blob Storage for documents | PRD / Brief | Separate from DB, SAS token access |
| Microsoft Entra ID (OIDC) | PRD / Brief | Auth provider locked, SSO required |
| Solo developer + AI-assisted | PRD | Architecture must be simple enough for one person to maintain |
| Desktop-first, Edge/Chrome only | PRD | No Safari/Firefox, no mobile optimization |
| PDF-only upload in MVP | PRD | Word-to-PDF deferred to Growth |
| No real-time updates in MVP | PRD | Manual refresh, SignalR deferred |
| Pragmatic TDD for domain/application logic | Brief | Testing strategy shapes code structure (see `docs/testing-pragmatic-tdd.md`) |

### Cross-Cutting Concerns Identified

1. **Audit Trail** — Every state-changing operation must produce an immutable event. Shapes middleware/interceptor design. No PII in event payloads (IDs + action context only).

2. **GDPR/PII Lifecycle** — Data model must support selective anonymization from day one. Aggregate metrics preserved, direct identifiers stripped. Retention timer on recruitment close triggers scheduled cleanup.

3. **Per-Recruitment Access Control** — Not global roles. Every data query filtered by recruitment membership. **Open question:** implementation strategy (EF Core global query filters vs repository-level filtering vs middleware).

4. **Idempotency** — PRD emphasizes idempotent re-import, but idempotency as a design principle should influence the entire API layer, not just imports.

5. **Accessibility (WCAG 2.1 AA)** — Keyboard navigation, focus management, ARIA attributes, color-independent indicators. Must be designed into components, not bolted on.

6. **Error Handling & Validation** — Import pipeline has multiple failure modes. All must surface clear, actionable feedback without exposing internals.

7. **Structured Logging** — Correlation IDs, zero PII, Application Insights integration. **Open question:** import session correlation model — single ID spanning the entire import session, or parent-child hierarchy across upload/parse/split/match phases.

## Starter Template Evaluation

### Primary Technology Domain

Full-stack web application based on project requirements analysis. Backend API + SPA frontend, both deployed on Azure.

### Repository Structure

**Monorepo** — single repository with two top-level application folders:

```
recruitment-tracker/
  api/           # .NET Clean Architecture solution
  web/           # Vite React app
  docs/          # Existing project documentation
  .github/       # CI/CD workflows
```

Rationale: Solo developer project. One repo means one PR per feature (both API and UI changes together), one CI/CD pipeline configuration, zero context-switching between repos. A feature like "import candidates" touches both `api/` and `web/` — one PR, one review, one merge.

### Starter Options Considered

**Backend:**

| Template | Version | Framework | Key Feature | Why Not |
|----------|---------|-----------|-------------|---------|
| Jason Taylor CleanArchitecture | v10.0.0 | .NET 10 | Minimal API, azd, use case scaffolding | **Selected** |
| Ardalis CleanArchitecture | v11.0.0 | .NET 9 | FastEndpoints, DDD-focused | FastEndpoints adds dependency + learning curve |

**Frontend:**

| Approach | Key Feature | Why Not |
|----------|-------------|---------|
| Official Vite + react-ts template | Zero opinions, add what we need | **Selected** |
| Third-party feature-rich templates | Pre-bundled libraries | Opinionated choices to remove, not add |

### Selected Backend Starter: Jason Taylor Clean Architecture

**Rationale:**
- .NET 10 (current), most widely adopted Clean Architecture template (18.7K stars)
- Minimal API is built into ASP.NET Core — no additional library dependency
- Use case scaffolding (`dotnet new ca-usecase`) eliminates boilerplate for commands/queries (~40% of a typical story's backend code; domain entities and infrastructure services remain manual)
- Azure Developer CLI (azd) integration aligns with Azure deployment target
- Aspire integration provides Azure SQL + Blob Storage + Application Insights wiring

**Aspire trade-off:** Aspire adds cognitive overhead (another abstraction to debug) but provides genuine value for Azure service orchestration and telemetry. For this project's scale, the integration value outweighs the debugging complexity. Application code does not couple to Aspire — it can be removed later if painful.

**Initialization Command:**

```bash
dotnet new install Clean.Architecture.Solution.Template::10.0.0
dotnet new ca-sln -o api --database SqlServer
```

**Architectural Decisions Provided by Starter:**

- **Language & Runtime:** C# / .NET 10
- **API Style:** Minimal API endpoints
- **Project Structure:** Domain / Application / Infrastructure / Web (Clean Architecture)
- **ORM:** Entity Framework Core with SQL Server
- **Patterns:** CQRS via MediatR, FluentValidation for input validation
- **Testing:** xUnit with test project structure (Domain.UnitTests, Application.UnitTests, Application.FunctionalTests, Infrastructure.IntegrationTests)
- **Azure:** azd integration, Aspire orchestration

**Database Strategy:**
- **Development:** Template defaults to delete-and-recreate with seed data on startup. Acceptable for initial development only.
- **Production:** EF Core migrations from the first deployment onward. Switch to migrations in development once schema stabilizes. The anonymization schema (GDPR) requires careful migration planning — cannot casually recreate a database with retention timers and audit trails.

### Selected Frontend Starter: Official Vite + React + TypeScript

**Rationale:**
- Clean starting point — add exactly what the project needs, remove nothing
- Tailwind CSS v4 with first-party Vite plugin requires zero configuration
- No opinionated library choices to fight or remove

**Initialization Commands:**

```bash
npm create vite@latest web -- --template react-ts
cd web && npm install tailwindcss @tailwindcss/vite
```

**Current Versions:** Vite 7.x (stable), React 19, Tailwind CSS 4.1.x

**Architectural Decisions Provided by Starter:**

- **Language:** TypeScript (strict)
- **Build Tool:** Vite 7 (dev server + production build)
- **Styling:** Tailwind CSS v4 via `@tailwindcss/vite` plugin (zero-config, automatic content detection)
- **Development:** Hot module replacement, fast refresh

**Tailwind v4 Browser Support:** Requires Chrome 111+, Firefox 128+, Safari 16.4+. Non-issue — PRD constrains to Edge (Chromium) and Chrome only.

**Libraries to add incrementally (not upfront):**

| Library | When to Add | Purpose |
|---------|------------|---------|
| @azure/msal-browser + @azure/msal-react | Day one | Entra ID OIDC auth (Authorization Code + PKCE) |
| Vitest + Testing Library | Day one | Unit/component testing |
| MSW (Mock Service Worker) | Day one | API mocking for component tests (consistent with `mocks/` directory) |
| ESLint + Prettier (or Biome) | Day one | Code quality |
| React Router v7 | First route setup | Client-side routing |
| TanStack Query | First API call | Server state management |
| Playwright | First E2E test | End-to-end smoke tests |
| react-pdf | Screening feature (Epic 4) | PDF rendering with text layer accessibility and per-page lazy loading |
| Form library (TBD) | Outcome recording feature | Form state management |
| Typed API client (TBD) | First API integration | Typed HTTP calls matching backend DTOs |

### Architectural Decision: PDF Viewing

**MVP uses react-pdf (PDF.js wrapper) for inline PDF rendering with short-lived SAS URLs.**

SAS tokens (15-minute validity) make the URL self-authenticating. react-pdf provides a text layer for screen reader accessibility (WCAG 2.1 AA compliance) and per-page lazy loading for efficient screening of 130+ candidate sessions. Page 1 renders immediately; subsequent pages lazy-load on scroll intersection. SAS URL pre-fetching for the next 2-3 candidates ensures seamless screening flow.

### Development Workflow

- .NET API runs on `localhost:5001` (or similar)
- Vite dev server runs on `localhost:5173` with API proxy configuration
- Aspire can orchestrate the API + Azure SQL in development (does not manage the Vite dev server)
- Both apps start independently; proxy configuration in `vite.config.ts` routes `/api/*` to the .NET backend

### CI/CD Pipeline Strategy

**Start with a single pipeline** that builds and tests both `api/` and `web/` on every PR. Simple and correct.

Optimize to **path-filtered pipelines** (`api/**` triggers .NET, `web/**` triggers frontend, both trigger both) only if CI becomes a bottleneck. Premature pipeline optimization is real.

**Note:** Project initialization using these commands should be the first implementation story.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| 1 | Per-recruitment authorization | EF Core global query filters + endpoint-level authorization | Defense in depth — global filters catch data layer leaks, endpoint checks provide clear access denial |
| 2 | Async import processing | In-process `IHostedService` + `Channel<T>` | Simple, no external dependencies. Database import session as durable state. Queue-based approach accommodated for later |
| 3 | Frontend state management | Component-local state + React Context | Almost all state is server state (TanStack Query) or URL state (router params). No external library justified |
| 4 | Azure hosting model | App Service (API) + Static Web Apps (frontend) | Path of least resistance for .NET + SPA on Azure. azd template support. Solo dev operational simplicity |
| 5 | Error handling | RFC 9457 Problem Details (ASP.NET Core built-in) | HTTP standard, native framework support, predictable frontend contract |

**Important Decisions (Shape Architecture):**

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| 6 | SAS token strategy | Batch URLs embedded in candidate list response | Supports screening pre-fetch pattern. No separate document URL endpoint needed |
| 7 | GDPR retention job | `IHostedService` with daily timer | Same pattern as import processor. App Service "Always On" keeps it running. Anonymization logic in Application layer |
| 8 | API documentation | Built-in OpenAPI + Scalar | Ships with template. Dev-only, disabled in production |
| 9 | Frontend folder structure | Feature-based | Self-contained feature units. Clean mapping to domain areas |
| 10 | Environment configuration | `appsettings.json` layering + Azure Key Vault for secrets | Standard .NET pattern. App Service native Key Vault references for secrets |

**Deferred Decisions (Post-MVP):**

| Decision | Rationale for Deferral |
|----------|----------------------|
| API versioning | Single consumer (own frontend), no external API clients in MVP |
| Rate limiting | Internal tool behind SSO, 15 users max. Not a risk vector for MVP |
| CDN / edge caching | Static Web Apps includes built-in CDN. No additional configuration needed for MVP scale |
| Advanced monitoring/alerting | Application Insights via Aspire provides baseline. Custom dashboards deferred |
| Typed API client generation | Evaluate OpenAPI-based codegen (e.g., orval, openapi-typescript) vs hand-typed clients during first API integration story |

### Data Architecture

**Database:** Azure SQL with Entity Framework Core (Code First)

**Modeling approach:** Rich domain entities in the Domain project following pragmatic DDD. EF Core configurations in Infrastructure (Fluent API, no data annotations on domain entities). Entities own their business rules and invariants. Aggregates define consistency and testing boundaries.

#### Aggregate Boundaries

The domain uses three aggregates. Each aggregate root enforces its own invariants. Child entities are never modified directly — always through the aggregate root's methods.

| Aggregate Root | Owned Entities | Key Invariants |
|----------------|---------------|----------------|
| **Recruitment** | `WorkflowStep`, `RecruitmentMember` | Cannot close with active candidates in progress. Step names unique within recruitment. Step order is contiguous. Members require valid role. At least one Recruiting Leader must exist. |
| **Candidate** | `CandidateOutcome`, `CandidateDocument` | Outcomes reference valid workflow steps. Cannot record outcome on a step that doesn't exist in the recruitment. Cannot delete candidate with recorded outcomes (soft-delete or archive). One primary document per type. |
| **ImportSession** | _(no children — tracks row-level results as value objects)_ | Status transitions: Processing → Completed or Failed. Cannot transition backwards. Row results are immutable once written. |

**Standalone entities (not aggregates):**
- `AuditEntry` — append-only, no business rules, no aggregate root needed

**Aggregate rules for AI agents:**
1. All state changes to owned entities go through the aggregate root's methods (e.g., `recruitment.AddStep()`, not `dbContext.WorkflowSteps.Add()`)
2. An aggregate root is the unit of persistence — load and save the whole aggregate, not individual children
3. Cross-aggregate references use IDs only (e.g., `Candidate` holds `RecruitmentId`, not a navigation property to `Recruitment`)
4. Domain events are raised by aggregate roots when significant state changes occur
5. Command handlers operate on one aggregate per transaction — if a command affects two aggregates, use domain events for eventual consistency

**Testing implication:** Test aggregate behavior through the root's public methods. Unit tests verify invariants (e.g., "adding a duplicate step name throws `DuplicateStepException`"). No need to test child entity properties in isolation.

#### Ubiquitous Language

These terms have precise meanings in code, documentation, and conversation. AI agents and developers must use them consistently.

| Term | Meaning | NOT |
|------|---------|-----|
| **Recruitment** | A hiring process with steps, candidates, and team members | "Job", "position", "opening" |
| **Workflow Step** | A named phase in a recruitment (e.g., Screening, Technical Interview) | "Stage", "phase", "milestone" |
| **Candidate** | A person being evaluated in a recruitment | "Applicant", "participant" |
| **Outcome** | A recorded decision on a candidate for a specific step (Pass/Fail/Hold) | "Result", "score", "verdict" |
| **Screening** | The act of reviewing candidates and recording outcomes across steps | "Review", "evaluation" (too vague) |
| **Import Session** | A single XLSX/PDF upload-and-process operation with tracked results | "Upload", "sync", "batch" |
| **Recruitment Member** | A user with an assigned role in a specific recruitment | "Participant", "user" (too generic) |
| **Recruiting Leader** | The member who created/owns the recruitment (admin role) | "Manager", "owner" |
| **SME/Collaborator** | A member who reviews candidates and records outcomes | "Reviewer" (acceptable shorthand in UI) |

**Per-recruitment data isolation via `ITenantContext`:**

EF Core global query filters on all candidate-related entities, driven by an `ITenantContext` abstraction:

```
ITenantContext
  ├── UserId         (set by web middleware from JWT)
  ├── RecruitmentId  (set by import service for scoped access)
  └── IsServiceContext  (set by GDPR job to bypass filter)
```

The `DbContext` reads from `ITenantContext` — it doesn't know or care whether the caller is a web request, import job, or GDPR service. Web middleware populates `UserId` from the authenticated JWT. Background services set `RecruitmentId` (import) or `IsServiceContext = true` (GDPR anonymization) explicitly.

This pattern solves three problems at once:
- Web requests: automatically scoped to user's recruitment memberships
- Import service: scoped to the specific recruitment being imported
- GDPR job: bypasses filter to query all expired recruitments

**Overview data strategy:** Computed on read via `GROUP BY` query for MVP. At 150 candidates and 7 workflow steps, this is sub-100ms on Azure SQL — well within the 500ms NFR2 budget. No pre-aggregation needed at this scale. Pre-aggregate only if measured performance degrades.

This simplifies both the import pipeline (no counter updates) and the outcome recording path (just saves the outcome). Fewer moving parts, fewer bugs, same performance.

**Migration strategy:**
- Development: Template's delete-and-recreate for initial schema exploration
- Production: EF Core migrations from first deployment onward
- Schema designed for anonymization from day one (direct identifiers separable from aggregate metrics)

**Configurable deployment values:**
- GDPR retention period (default: 12 months)
- Stale step threshold (default: 5 calendar days)
- SAS token validity (max: 15 minutes)
- XLSX column name mapping

### Authentication & Security

**Authentication:** Microsoft Entra ID via OIDC. Authorization Code flow with PKCE for the SPA frontend. Backend validates JWT access tokens.

**Authorization (defense in depth):**
1. **Endpoint level:** ASP.NET Core authorization policies check recruitment membership before any data access. Returns 403 for non-members.
2. **Data level:** EF Core global query filters via `ITenantContext` ensure queries only return data for authorized recruitments. Catches any bugs in endpoint authorization.

**Mandatory security test scenarios (write first, before feature tests):**
- User in Recruitment A cannot see candidates from Recruitment B (positive filter test)
- Import service *can* write candidates to the recruitment it's processing (service context bypass)
- GDPR job *can* query across all expired recruitments (admin context bypass)
- Misconfigured context (no user, no service flag) returns zero results, not an error
- These are security-critical integration tests — non-optional

**Document security:** PDF documents served via short-lived SAS tokens (15-minute validity) generated by the API. SAS URLs embedded in candidate list responses for batch screening efficiency. No direct Blob Storage access from the frontend.

**Input validation:** FluentValidation on all command/query inputs. File type and size validation on uploads (XLSX: 10MB max, PDF: 100MB max). Parameterized queries via EF Core (SQL injection prevention).

**Headers:** `noindex` meta tag + `X-Robots-Tag: noindex` response header. CORS restricted to the Static Web Apps domain.

### Development Authentication Patterns

**Purpose:** A dev auth bypass enables full-stack development and testing without an Entra ID tenant dependency. All downstream stories can be built and tested locally using preconfigured user personas.

**Frontend — Dev Auth Mode (`VITE_AUTH_MODE=development`):**
- `DevAuthProvider` replaces `MsalProvider` when `VITE_AUTH_MODE=development`
- A floating dev toolbar allows switching between preconfigured personas: User A, User B, Admin/Service, Unauthenticated
- `httpClient.ts` has a dual path in `getAuthHeaders()` — production mode uses `acquireTokenSilent()`, dev mode reads from the dev auth context and sends `X-Dev-User-Id` / `X-Dev-User-Name` headers
- Selection persisted in `localStorage`

**Backend — Dev Auth Handler (`ASPNETCORE_ENVIRONMENT=Development`):**
- `DevelopmentAuthenticationConfiguration.cs` registers a custom `AuthenticationHandler<T>` that reads `X-Dev-User-Id` and `X-Dev-User-Name` headers and builds a `ClaimsPrincipal`
- Downstream services (`CurrentUserService`, `TenantContext`) read claims identically regardless of which auth handler ran

**Safety invariant:** The dev auth handler is registered ONLY inside an `IHostEnvironment.IsDevelopment()` runtime check. **Never use `#if DEBUG` preprocessor directives** — they can leak into release builds if build configurations are misconfigured.

**Preconfigured personas map to security test scenarios:**
- User A / User B → cross-recruitment isolation testing
- Admin/Service → service context bypass testing
- Unauthenticated → 401 enforcement testing

### API & Communication Patterns

**API style:** RESTful via Minimal API endpoints. Organized by feature in the Web project.

**Key endpoint patterns:**
- `GET /api/recruitments/{id}/overview` — Computed dashboard data via GROUP BY (NFR2: 500ms)
- `GET /api/recruitments/{id}/candidates` — Paginated list with search/filter, includes batch SAS URLs (NFR3: 1s)
- `POST /api/recruitments/{id}/import` — Accepts file upload, returns 202 Accepted with import session ID
- `GET /api/import-sessions/{id}` — Poll for import progress and results

**Async operations:** Import upload returns 202 Accepted immediately. Client polls the import session endpoint for progress. Import processing runs in-process via `IHostedService` + `Channel<T>`.

**Error responses:** RFC 9457 Problem Details. Validation errors include field-level detail. Import errors include row-level detail. No PII in error responses.

**Test convention for error responses:** Every endpoint's integration tests must verify the Problem Details shape — assert status code *and* `ProblemDetails.Title` *and* `ProblemDetails.Errors` keys for validation failures. Status code-only assertions are insufficient.

**Documentation:** OpenAPI document auto-generated. Scalar UI for interactive exploration in development only.

### Frontend Architecture

**Framework:** React 19 + TypeScript (strict) + Vite 7 + Tailwind CSS v4

**Viewport constraint:** Desktop-first, minimum viewport width of 1280px. The screening split-panel layout requires sufficient horizontal space for candidate list + PDF viewer + outcome form. Below 1280px, display a "please use a wider browser window" message. No responsive breakpoints — the PRD specifies desktop-only (Edge/Chrome).

**Authentication library:** `@azure/msal-browser` + `@azure/msal-react` for Entra ID OIDC (Authorization Code flow with PKCE). MSAL handles token acquisition, caching, and silent refresh. The `AuthContext` wraps MSAL's `useMsal` hook to provide a consistent interface for the rest of the app. The `msalInstance` (PublicClientApplication) is created and exported from `msalConfig.ts` — both `AuthContext.tsx` and `httpClient.ts` import from there to avoid circular dependencies.

**State management:**
- **Server state:** TanStack Query (caching, refetching, optimistic updates)
- **URL state:** React Router v7 params (active recruitment, candidate selection)
- **Client state:** Component-local `useState`/`useReducer` + React Context for shared concerns (auth, screening session)

**Folder structure:** Feature-based

```
web/src/
  features/
    auth/              # Auth context, login redirect
    recruitments/      # List, create, edit, close
    candidates/        # List, detail, import, manual create
    screening/         # Split-panel batch screening
    overview/          # Dashboard, health indicators
    team/              # Membership management
  components/          # Shared UI components
  hooks/               # Shared hooks
  lib/
    api/               # API client modules (per feature)
  routes/              # Route definitions (thin layer)
```

**API client contract pattern:**

Each feature has a corresponding API module in `lib/api/`. TanStack Query hooks consume these modules:

```typescript
// lib/api/recruitments.ts — API client module
export const recruitmentApi = {
  getById: (id: string) => fetch(`/api/recruitments/${id}`).then(handleResponse),
  getOverview: (id: string) => fetch(`/api/recruitments/${id}/overview`).then(handleResponse),
};

// features/overview/hooks/useRecruitmentOverview.ts — TanStack Query hook
export const useRecruitmentOverview = (id: string) =>
  useQuery({
    queryKey: ['recruitment', id, 'overview'],
    queryFn: () => recruitmentApi.getOverview(id),
  });
```

Whether the API modules are hand-written or generated from OpenAPI (deferred decision), the consumption pattern is the same. Features never call `fetch` directly.

**HTTP client foundation pattern:**

`httpClient.ts` is the single entry point for all API communication. Every API module imports from here — never from `fetch` directly:

```typescript
// lib/api/httpClient.ts
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

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: await getAuthHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  return handleResponse<T>(res);
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (res.status === 401) {
    // Token expired or invalid — redirect to login
    msalInstance.loginRedirect();
    throw new AuthError('Session expired');
  }
  if (!res.ok) {
    const problem = await res.json(); // ProblemDetails shape
    throw new ApiError(res.status, problem);
  }
  return res.json() as Promise<T>;
}
```

API modules then use these helpers instead of raw `fetch`:

```typescript
// lib/api/recruitments.ts
import { apiGet, apiPost } from './httpClient';
import type { Recruitment, RecruitmentOverview } from './recruitments.types';

export const recruitmentApi = {
  getById: (id: string) => apiGet<Recruitment>(`/recruitments/${id}`),
  getOverview: (id: string) => apiGet<RecruitmentOverview>(`/recruitments/${id}/overview`),
  create: (data: CreateRecruitmentRequest) => apiPost<Recruitment>('/recruitments', data),
};
```

**Batch screening architecture:**
- PDF pre-fetching via SAS URLs from candidate list response
- Three isolated state domains (candidate list, PDF viewer, outcome form)
- Focus management contract: focus returns to candidate list after outcome submission
- Optimistic outcome recording with non-blocking retry
- Keyboard-first navigation

**PDF viewing:** react-pdf (PDF.js wrapper) with self-authenticating SAS URLs. Text layer enabled for screen reader accessibility. Per-page lazy loading for screening performance.

### Infrastructure & Deployment

**Hosting:**
- API: Azure App Service (Always On, for background services)
- Frontend: Azure Static Web Apps (built-in CDN, CI/CD from GitHub)
- Database: Azure SQL
- Storage: Azure Blob Storage
- Secrets: Azure Key Vault (App Service native references)
- Telemetry: Application Insights (via Aspire)

**CI/CD:** Single GitHub Actions pipeline. Builds and tests both `api/` and `web/` on every PR. Path-filtered optimization deferred until CI becomes a bottleneck.

**Environment configuration:** `appsettings.json` layering for non-secret config. Azure Key Vault for secrets (connection strings, client secrets). App Service settings for deployment-specific overrides.

**Background processing:**
- Import pipeline: `IHostedService` + `Channel<T>` (in-process)
- GDPR anonymization: `IHostedService` with daily timer
- Both run within App Service process (Always On)
- Both use `ITenantContext` with appropriate service context (not HTTP user context)

### Decision Impact Analysis

**Implementation Sequence:**

1. Project scaffolding (template init, monorepo structure, CI pipeline, linting, test infrastructure)
2. Authentication (Entra ID + OIDC via MSAL) — gates everything
3. Data model + EF Core setup (`ITenantContext`, global query filters, migration strategy)
4. **Cross-recruitment isolation integration tests** (mandatory, before feature tests)
5. **Shared frontend components** (StatusBadge, ActionButton, EmptyState, Toast, SkeletonLoader, ErrorBoundary) — must exist before any feature UI
6. Recruitment CRUD (first end-to-end vertical slice)
7. Team membership management
8. Candidate import pipeline (async processing via `IHostedService` + `Channel<T>`)
9. Document storage + SAS token generation (batch-capable)
10. Batch screening UX (frontend-heavy)
11. Overview dashboard (computed `GROUP BY` query)
12. Audit trail
13. GDPR retention job (daily timer, `ITenantContext.IsServiceContext`)

**Cross-Component Dependencies:**
- `ITenantContext` + global query filters depend on auth (need current user identity)
- Import pipeline depends on data model + document storage + `ITenantContext.RecruitmentId`
- Screening UX depends on candidate list API + batch SAS URLs
- Overview depends on computed query (no write-path dependencies — simplest path)
- GDPR job depends on anonymization schema design + `ITenantContext.IsServiceContext`

## Implementation Patterns & Consistency Rules

_These patterns prevent AI agent implementation conflicts. All agents MUST follow these conventions._

### Naming Patterns

#### Backend (C# / .NET)

| Element | Convention | Example |
|---------|-----------|---------|
| Classes, methods, properties | PascalCase | `ImportCandidatesCommand`, `GetById()` |
| Private fields | `_camelCase` with underscore prefix | `_recruitmentRepository` |
| Local variables, parameters | camelCase | `candidateCount`, `importSession` |
| Interfaces | `I` prefix + PascalCase | `ITenantContext`, `ICandidateRepository` |
| Constants | PascalCase | `MaxImportFileSize` |
| Enums | PascalCase (singular) | `OutcomeStatus.Approved` |
| Async methods | `Async` suffix | `GetCandidatesAsync()` |

#### Database (EF Core → Azure SQL)

| Element | Convention | Example |
|---------|-----------|---------|
| Tables | PascalCase plural | `Recruitments`, `Candidates`, `WorkflowSteps` |
| Columns | PascalCase (match C# property) | `FullName`, `DateApplied`, `RecruitmentId` |
| Foreign keys | `{Entity}Id` | `RecruitmentId`, `CandidateId` |
| Indexes | `IX_{Table}_{Columns}` | `IX_Candidates_RecruitmentId_Email` |
| Unique constraints | `UQ_{Table}_{Columns}` | `UQ_Candidates_RecruitmentId_Email` |

EF Core maps C# PascalCase properties directly — no snake_case translation layer.

#### API (Minimal API Endpoints)

| Element | Convention | Example |
|---------|-----------|---------|
| URL paths | kebab-case, plural nouns | `/api/recruitments/{id}/candidates` |
| Route parameters | camelCase in `{braces}` | `{recruitmentId}`, `{candidateId}` |
| Query parameters | camelCase | `?stepId=abc&outcome=approved` |
| JSON response fields | camelCase | `{ "fullName": "...", "dateApplied": "..." }` |

ASP.NET Core's `System.Text.Json` uses camelCase by default.

#### Frontend (React / TypeScript)

| Element | Convention | Example |
|---------|-----------|---------|
| Components | PascalCase (file and export) | `CandidateList.tsx`, `OutcomeForm.tsx` |
| Hooks | `use` prefix, camelCase file | `useRecruitmentOverview.ts` |
| Utilities / helpers | camelCase file | `formatDate.ts`, `handleResponse.ts` |
| API modules | camelCase file | `recruitmentApi.ts`, `candidateApi.ts` |
| API types | camelCase `.types.ts` suffix | `recruitmentApi.types.ts`, `candidateApi.types.ts` |
| Types / interfaces | PascalCase, no `I` prefix | `Candidate`, `RecruitmentOverview` |
| Feature folders | kebab-case | `features/batch-screening/` |
| Constants | UPPER_SNAKE_CASE | `MAX_FILE_SIZE`, `STALE_THRESHOLD_DAYS` |

**Import statement ordering** (enforced via ESLint):

```
1. React/framework imports
2. Third-party libraries
3. Absolute imports (@/ alias)
4. Relative imports
5. Type-only imports
```

### Structure Patterns

#### Backend Project Organization (Clean Architecture)

```
api/src/
  Domain/
    Entities/              # Recruitment, Candidate, WorkflowStep, etc.
    ValueObjects/          # OutcomeResult, CandidateMatch, etc.
    Enums/                 # OutcomeStatus, ImportMatchConfidence, etc.
    Events/                # Domain events
    Exceptions/            # Domain-specific exceptions
  Application/
    Common/                # Shared interfaces, behaviors, mappings
      Interfaces/          # IRecruitmentRepository, ITenantContext, etc.
      Behaviours/          # Validation, logging pipeline behaviors
    Features/              # Organized by feature (CQRS)
      Recruitments/
        Commands/
          CreateRecruitment/
            CreateRecruitmentCommand.cs
            CreateRecruitmentCommandValidator.cs
            CreateRecruitmentCommandHandler.cs
        Queries/
          GetRecruitmentOverview/
            GetRecruitmentOverviewQuery.cs
            GetRecruitmentOverviewQueryHandler.cs
            RecruitmentOverviewDto.cs
      Candidates/
      Import/
      Screening/
  Infrastructure/
    Data/                  # DbContext, configurations, migrations
    Services/              # Blob storage, XLSX parser, PDF splitter
    Identity/              # Entra ID integration, ITenantContext impl
  Web/
    Endpoints/             # Minimal API endpoint definitions (by feature)
    Middleware/             # Auth, error handling, tenant context
    Configuration/         # DI registration, app config
```

**Rule: One command/query per folder.** Each command or query gets its own folder containing the request, validator, handler, and any DTOs. The `ca-usecase` scaffolding follows this pattern.

**Rule: Manual mapping, no AutoMapper.** Use `ToDto()` extension methods or static `From()` factory methods on DTOs. Every field is visibly mapped — explicit beats magic for debuggability and solo dev maintenance.

```csharp
// Example: DTO with explicit mapping
public record RecruitmentOverviewDto
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public int TotalCandidates { get; init; }

    public static RecruitmentOverviewDto From(Recruitment entity, int candidateCount) =>
        new()
        {
            Id = entity.Id,
            Title = entity.Title,
            TotalCandidates = candidateCount
        };
}
```

#### Backend Tests

```
api/tests/
  Domain.UnitTests/        # Entity logic, value objects, domain rules
  Application.UnitTests/   # Command/query handlers (mocked infra)
  Application.FunctionalTests/  # API endpoints via WebApplicationFactory
  Infrastructure.IntegrationTests/  # EF Core, Blob Storage, real database
```

**Rule: Test files mirror source structure.** `Application.UnitTests/Features/Recruitments/Commands/CreateRecruitmentCommandTests.cs` matches the source path.

**Rule: NSubstitute for backend mocking.** The Jason Taylor template uses NSubstitute. All Application.UnitTests use NSubstitute for mocking interfaces (`ITenantContext`, `IApplicationDbContext`, `IBlobStorageService`, etc.). No Moq, no other mocking library. Example: `var tenantContext = Substitute.For<ITenantContext>();`

#### Frontend Structure

```
web/src/
  features/
    auth/
    recruitments/
    candidates/
    screening/
    overview/
    team/
  components/              # Shared UI components (StatusBadge, ActionButton, Toast, etc.)
  hooks/                   # Shared hooks
  lib/
    api/                   # API client modules + co-located types
      recruitments.ts
      recruitments.types.ts
      candidates.ts
      candidates.types.ts
  routes/                  # Route definitions (thin layer)
```

**Rule: Frontend tests co-locate with source.** Test files sit next to the file they test (`Component.test.tsx`). Backend tests use separate projects (template convention).

**Rule: API types co-located with API modules.** Each API module has a corresponding `.types.ts` file defining request/response TypeScript types. When codegen replaces hand-typed clients, these files are the ones that get replaced.

### Format Patterns

#### API Response Formats

| Scenario | Format | Example |
|----------|--------|---------|
| Single entity | Direct object | `{ "id": "...", "title": "..." }` |
| Collection | Pagination wrapper | `{ "items": [...], "totalCount": 42, "page": 1, "pageSize": 50 }` |
| Creation | 201 + Location header + entity | `Location: /api/recruitments/abc-123` |
| Async operation | 202 + status endpoint | `{ "importSessionId": "...", "statusUrl": "/api/import-sessions/..." }` |
| Validation error | 400 + Problem Details | `{ "type": "...", "title": "Validation Failed", "errors": {...} }` |
| Not found | 404 + Problem Details | `{ "type": "...", "title": "Not Found", "detail": "..." }` |
| Auth failure | 401/403 + Problem Details | Standard ASP.NET Core response |
| Server error | 500 + Problem Details (no internals) | `{ "type": "...", "title": "Internal Server Error" }` |

**Rule: No wrapper envelope.** Success responses return data directly. Errors use Problem Details. Frontend distinguishes by HTTP status code.

#### Data Formats

- **Date/time:** ISO 8601 everywhere. `DateTimeOffset` in C#, `datetimeoffset` in SQL, `string` in TypeScript. Display via `Intl.DateTimeFormat`.
- **IDs:** GUIDs (`Guid` / `uniqueidentifier` / `string`). Generated server-side. No sequential integers (prevents enumeration).
- **Nulls:** API never returns `null` for collections — return `[]`. Nullable fields use `null` in JSON (not omitted). TypeScript uses `| null` explicitly, not `| undefined`.
- **JSON casing:** camelCase (System.Text.Json default). No configuration needed.

### Communication Patterns

#### MediatR Domain Events

| Element | Convention | Example |
|---------|-----------|---------|
| Event name | PascalCase past tense | `CandidateImportedEvent`, `OutcomeRecordedEvent` |
| Event class | Implements `INotification` | Lives in `Domain/Events/` |
| Handler | `{EventName}Handler` | In `Application/Features/` near related feature |

#### Audit Events

```csharp
public record AuditEvent(
    Guid RecruitmentId,
    Guid? EntityId,
    string EntityType,       // "Candidate", "Recruitment", "Document"
    string ActionType,       // "Created", "Updated", "Deleted", "Accessed"
    Guid PerformedBy,
    DateTimeOffset PerformedAt,
    JsonDocument? Context    // No PII — IDs and metadata only
);
```

**Rule: No PII in audit event `Context`.** Use entity IDs, not names or emails. The audit trail references entities; it doesn't duplicate their data.

### Process Patterns

#### Backend Error Handling

| Layer | Pattern |
|-------|---------|
| Domain | Throw domain-specific exceptions (`RecruitmentClosedException`, `DuplicateCandidateException`) |
| Application | FluentValidation catches input errors before handler executes. Handler catches domain exceptions and translates to results. |
| Web | Global exception middleware converts unhandled exceptions to Problem Details. No stack traces in production. |

**Rule: Domain never catches silently.** If a business rule is violated, throw. Let the application layer decide what to do.

#### Frontend Error Handling

| Scenario | Pattern |
|----------|---------|
| API errors | TanStack Query `onError`. Parse Problem Details. Toast for transient, inline for validation. |
| Render errors | React Error Boundary at feature level. Fallback UI + console log. |
| Network errors | TanStack Query retry (3 attempts for GET, no retry for mutations). Offline state if all fail. |

#### Frontend Loading States

| State | Pattern |
|-------|---------|
| Initial load | Skeleton placeholder matching final layout shape |
| Navigation | Instant (client-side routing, TanStack Query cache) |
| Mutation in progress | Disable submit button, show inline spinner |
| Optimistic update | Update TanStack Query cache immediately, rollback on error |
| Background refresh | No visible indicator (silent refetch on focus/interval) |

**Rule: Never show a full-page spinner.** Skeletons for initial loads, inline indicators for mutations, silent for background refreshes.

#### Empty State Pattern

**Rule: Every list component has an empty state variant.** Empty states show:
- An illustration or icon (contextual)
- Explanatory text ("No candidates imported yet")
- A primary action ("Import from Workday" or "Add candidate")

Never a blank void, never "No data found," never a spinner for empty data.

**Special case — Onboarding (FR10):** The `RecruitmentList` empty state doubles as the first-time user experience. When no recruitments exist, this is the user's entry point to the application. The empty state here should include onboarding-quality guidance: what the app does, how to get started, and a prominent "Create your first recruitment" CTA. This is not a generic "No data" screen — it's the onboarding flow.

#### Validation Timing

| Side | When | How |
|------|------|-----|
| Frontend | On blur + on submit | Field-level validation for immediate feedback |
| Backend | Before handler execution | FluentValidation pipeline behavior in MediatR |
| **Rule** | Backend is authoritative | Frontend validation is UX convenience, not security |

### UI Consistency Rules

#### Status Indicators (Shared `StatusBadge` Component)

| Status | Color | Icon | Used For |
|--------|-------|------|----------|
| Not Started | Gray | Circle outline | Step/outcome not yet touched |
| In Progress | Blue | Half-filled circle | Step currently active |
| Approved/Pass | Green | Checkmark | Positive outcome |
| Declined/Fail | Red | X mark | Negative outcome |
| Hold | Amber | Pause icon | Deferred decision |
| Stale | Orange | Clock/warning | Time threshold exceeded (NFR27: shape+icon, not color only) |

**Rule: All status indicators use the shared `StatusBadge` component.** No feature creates its own status styling.

#### Action Button Patterns (Shared `ActionButton` Component)

| Action Type | Style | Position |
|-------------|-------|----------|
| Primary (Create, Save, Import) | Filled, accent color | Bottom-right or top-right |
| Secondary (Cancel, Back) | Outlined | Left of primary |
| Destructive (Remove, Close Recruitment) | Red text or outlined red | Separated from primary actions |
| Navigation (View, Open) | Text link or ghost button | Inline |

#### Toast Notifications (Shared `Toast` System)

| Type | Color | Duration | Use |
|------|-------|----------|-----|
| Success | Green | 3 seconds, auto-dismiss | Outcome saved, import started |
| Error | Red | Persistent until dismissed | API error, validation failure |
| Info | Blue | 5 seconds, auto-dismiss | Import complete, status change |

#### Shared Components (Build First)

These shared components must exist before feature development begins:

```
components/
  StatusBadge.tsx           # Status indicator with color + icon
  StatusBadge.types.ts      # Status enum matching backend OutcomeStatus
  ActionButton.tsx          # Primary/Secondary/Destructive variants
  EmptyState.tsx            # Icon + text + action pattern
  Toast/
    ToastProvider.tsx
    useToast.ts
```

### Enforcement Guidelines

**All AI Agents MUST:**

1. Follow naming conventions exactly — no exceptions, no "creative" alternatives
2. Place files in the specified locations — structure is non-negotiable
3. Use shared components (`StatusBadge`, `ActionButton`, `Toast`, `EmptyState`) — never create feature-local equivalents
4. Return Problem Details for all API errors — no custom error shapes
5. Use manual DTO mapping — no AutoMapper or implicit mapping libraries
6. Include empty state handling for every list component
7. Write tests that assert Problem Details shape, not just status codes
8. Never put PII in audit events or logs
9. Use `ITenantContext` for data scoping — never query without it
10. Modify child entities only through aggregate root methods — never bypass the root (see Aggregate Boundaries)
11. Use ubiquitous language terms consistently — no synonyms (see Ubiquitous Language table)

**Pattern Enforcement:**
- ESLint + Prettier enforce frontend code style and import ordering
- `dotnet format` enforces backend code style
- CI pipeline runs both formatters — PRs with violations fail
- Code review (human or AI) validates architectural patterns above code style

## Project Structure & Boundaries

### Complete Project Directory Structure

```
recruitment-tracker/
├── .github/
│   └── workflows/
│       └── ci.yml                          # Single pipeline: build + test api/ and web/
├── .gitignore
├── docs/                                    # Existing project documentation
│   └── chatgpt-research-intro.md
├── api/                                     # .NET Clean Architecture solution
│   ├── api.sln
│   ├── Directory.Build.props                # Shared MSBuild properties
│   ├── Directory.Packages.props             # Central package management
│   ├── .editorconfig                        # C# code style enforcement
│   ├── azure.yaml                           # azd deployment manifest
│   ├── src/
│   │   ├── Domain/
│   │   │   ├── Domain.csproj
│   │   │   ├── Common/
│   │   │   │   └── BaseEntity.cs
│   │   │   ├── Entities/
│   │   │   │   ├── Recruitment.cs
│   │   │   │   ├── WorkflowStep.cs
│   │   │   │   ├── Candidate.cs
│   │   │   │   ├── CandidateOutcome.cs
│   │   │   │   ├── CandidateDocument.cs
│   │   │   │   ├── RecruitmentMember.cs
│   │   │   │   ├── ImportSession.cs
│   │   │   │   └── AuditEntry.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── CandidateMatch.cs        # Import matching result (confidence, method)
│   │   │   │   └── AnonymizationResult.cs   # GDPR anonymization outcome
│   │   │   ├── Enums/
│   │   │   │   ├── OutcomeStatus.cs          # NotStarted, Pass, Fail, Hold
│   │   │   │   ├── ImportMatchConfidence.cs  # High, Low, None
│   │   │   │   ├── RecruitmentStatus.cs      # Active, Closed
│   │   │   │   └── ImportSessionStatus.cs    # Processing, Completed, Failed
│   │   │   ├── Events/
│   │   │   │   ├── CandidateImportedEvent.cs
│   │   │   │   ├── OutcomeRecordedEvent.cs
│   │   │   │   ├── DocumentUploadedEvent.cs
│   │   │   │   ├── RecruitmentCreatedEvent.cs
│   │   │   │   ├── RecruitmentClosedEvent.cs
│   │   │   │   └── MembershipChangedEvent.cs
│   │   │   └── Exceptions/
│   │   │       ├── RecruitmentClosedException.cs
│   │   │       ├── DuplicateCandidateException.cs
│   │   │       ├── InvalidWorkflowTransitionException.cs
│   │   │       └── StepHasOutcomesException.cs
│   │   ├── Application/
│   │   │   ├── Application.csproj
│   │   │   ├── Common/
│   │   │   │   ├── Interfaces/
│   │   │   │   │   ├── IApplicationDbContext.cs
│   │   │   │   │   ├── ITenantContext.cs
│   │   │   │   │   ├── ICurrentUserService.cs   # User identity contract (Clean Architecture)
│   │   │   │   │   ├── IBlobStorageService.cs
│   │   │   │   │   ├── IXlsxParser.cs
│   │   │   │   │   ├── IPdfSplitter.cs
│   │   │   │   │   └── ICandidateMatchingEngine.cs
│   │   │   │   ├── Behaviours/
│   │   │   │   │   ├── ValidationBehaviour.cs
│   │   │   │   │   ├── LoggingBehaviour.cs
│   │   │   │   │   └── AuditBehaviour.cs
│   │   │   │   ├── Models/
│   │   │   │   │   └── PaginatedList.cs
│   │   │   │   └── Mappings/
│   │   │   │       └── MappingExtensions.cs  # Shared ToDto() extension methods
│   │   │   └── Features/
│   │   │       ├── Recruitments/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── CreateRecruitment/
│   │   │       │   │   │   ├── CreateRecruitmentCommand.cs
│   │   │       │   │   │   ├── CreateRecruitmentCommandValidator.cs
│   │   │       │   │   │   └── CreateRecruitmentCommandHandler.cs
│   │   │       │   │   ├── UpdateRecruitment/
│   │   │       │   │   ├── CloseRecruitment/
│   │   │       │   │   ├── AddWorkflowStep/
│   │   │       │   │   └── RemoveWorkflowStep/
│   │   │       │   └── Queries/
│   │   │       │       ├── GetRecruitments/
│   │   │       │       ├── GetRecruitmentById/
│   │   │       │       └── GetRecruitmentOverview/
│   │   │       │           ├── GetRecruitmentOverviewQuery.cs
│   │   │       │           ├── GetRecruitmentOverviewQueryHandler.cs
│   │   │       │           └── RecruitmentOverviewDto.cs
│   │   │       ├── Candidates/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── CreateCandidate/
│   │   │       │   │   ├── RemoveCandidate/
│   │   │       │   │   ├── AssignDocument/
│   │   │       │   │   └── UploadDocument/         # Individual PDF upload (per candidate)
│   │   │       │   └── Queries/
│   │   │       │       ├── GetCandidates/          # Paginated list with batch SAS URLs
│   │   │       │       ├── GetCandidateById/       # Single candidate detail + SAS URL
│   │   │       │       └── SearchCandidates/
│   │   │       ├── Import/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── StartImport/            # Accepts XLSX + optional PDF bundle
│   │   │       │   │   └── ResolveMatchConflict/   # Manual review of low-confidence matches
│   │   │       │   └── Queries/
│   │   │       │       └── GetImportSession/        # Poll for progress + results
│   │   │       ├── Screening/
│   │   │       │   ├── Commands/
│   │   │       │   │   └── RecordOutcome/
│   │   │       │   └── Queries/
│   │   │       │       └── GetCandidateOutcomeHistory/
│   │   │       ├── Team/
│   │   │       │   ├── Commands/
│   │   │       │   │   ├── AddMember/
│   │   │       │   │   └── RemoveMember/
│   │   │       │   └── Queries/
│   │   │       │       ├── GetMembers/
│   │   │       │       └── SearchDirectory/        # Entra ID directory search
│   │   │       └── Audit/
│   │   │           └── Queries/
│   │   │               └── GetAuditTrail/
│   │   ├── Infrastructure/
│   │   │   ├── Infrastructure.csproj
│   │   │   ├── Data/
│   │   │   │   ├── ApplicationDbContext.cs
│   │   │   │   ├── Configurations/
│   │   │   │   │   ├── RecruitmentConfiguration.cs
│   │   │   │   │   ├── CandidateConfiguration.cs
│   │   │   │   │   ├── WorkflowStepConfiguration.cs
│   │   │   │   │   ├── CandidateOutcomeConfiguration.cs
│   │   │   │   │   ├── CandidateDocumentConfiguration.cs
│   │   │   │   │   ├── RecruitmentMemberConfiguration.cs
│   │   │   │   │   ├── ImportSessionConfiguration.cs
│   │   │   │   │   └── AuditEntryConfiguration.cs
│   │   │   │   ├── Migrations/
│   │   │   │   ├── Interceptors/
│   │   │   │   │   └── AuditableEntityInterceptor.cs
│   │   │   │   └── Seeds/
│   │   │   │       └── SeedData.cs              # Development seed data
│   │   │   ├── Services/
│   │   │   │   ├── BlobStorageService.cs         # Azure Blob + SAS token generation
│   │   │   │   ├── XlsxParserService.cs          # Workday XLSX parsing + column mapping
│   │   │   │   ├── PdfSplitterService.cs         # Bundle splitting + TOC extraction
│   │   │   │   ├── CandidateMatchingEngine.cs    # Email primary, name+phone fallback
│   │   │   │   ├── ImportPipelineHostedService.cs # IHostedService + Channel<T> consumer
│   │   │   │   └── GdprRetentionService.cs       # IHostedService with daily timer
│   │   │   └── Identity/
│   │   │       ├── TenantContext.cs               # ITenantContext implementation
│   │   │       ├── CurrentUserService.cs          # ICurrentUserService impl, extracts from JWT
│   │   │       └── EntraIdDirectoryService.cs     # Graph API for directory search
│   │   └── Web/
│   │       ├── Web.csproj
│   │       ├── Program.cs
│   │       ├── appsettings.json
│   │       ├── appsettings.Development.json
│   │       ├── Endpoints/
│   │       │   ├── RecruitmentEndpoints.cs
│   │       │   ├── CandidateEndpoints.cs         # Includes document upload endpoint
│   │       │   ├── ImportEndpoints.cs
│   │       │   ├── ScreeningEndpoints.cs
│   │       │   ├── TeamEndpoints.cs
│   │       │   └── AuditEndpoints.cs
│   │       ├── Middleware/
│   │       │   ├── ExceptionHandlingMiddleware.cs  # Global → Problem Details
│   │       │   └── TenantContextMiddleware.cs      # Populates ITenantContext from JWT
│   │       └── Configuration/
│   │           ├── DependencyInjection.cs
│   │           └── AuthenticationConfiguration.cs
│   ├── tests/
│   │   ├── Domain.UnitTests/
│   │   │   ├── Domain.UnitTests.csproj
│   │   │   └── Entities/
│   │   │       ├── RecruitmentTests.cs
│   │   │       ├── CandidateTests.cs
│   │   │       ├── WorkflowStepTests.cs
│   │   │       └── CandidateOutcomeTests.cs
│   │   ├── Application.UnitTests/
│   │   │   ├── Application.UnitTests.csproj
│   │   │   └── Features/
│   │   │       ├── Recruitments/
│   │   │       │   └── Commands/
│   │   │       │       └── CreateRecruitmentCommandTests.cs
│   │   │       ├── Candidates/
│   │   │       ├── Import/
│   │   │       ├── Screening/
│   │   │       └── Team/
│   │   ├── Application.FunctionalTests/
│   │   │   ├── Application.FunctionalTests.csproj
│   │   │   ├── CustomWebApplicationFactory.cs
│   │   │   └── Endpoints/
│   │   │       ├── RecruitmentEndpointTests.cs
│   │   │       ├── CandidateEndpointTests.cs
│   │   │       ├── ImportEndpointTests.cs
│   │   │       ├── ScreeningEndpointTests.cs
│   │   │       ├── TeamEndpointTests.cs
│   │   │       └── SecurityIsolationTests.cs   # Cross-recruitment isolation (mandatory)
│   │   └── Infrastructure.IntegrationTests/
│   │       ├── Infrastructure.IntegrationTests.csproj
│   │       ├── Data/
│   │       │   ├── TenantContextFilterTests.cs  # Global query filter verification
│   │       │   └── MigrationTests.cs
│   │       └── Services/
│   │           ├── BlobStorageServiceTests.cs
│   │           ├── XlsxParserServiceTests.cs
│   │           ├── PdfSplitterServiceTests.cs
│   │           ├── CandidateMatchingEngineTests.cs
│   │           └── GdprRetentionServiceTests.cs # High-risk operation coverage
│   └── aspire/
│       ├── AppHost/                             # Aspire orchestration project
│       │   ├── AppHost.csproj
│       │   └── Program.cs
│       └── ServiceDefaults/
│           ├── ServiceDefaults.csproj
│           └── Extensions.cs
├── web/                                         # Vite React app
│   ├── package.json
│   ├── package-lock.json
│   ├── tsconfig.json
│   ├── tsconfig.app.json
│   ├── tsconfig.node.json
│   ├── vite.config.ts                           # API proxy to localhost:5001
│   ├── index.html
│   ├── .env.example
│   ├── .eslintrc.cjs                            # Import ordering + code style
│   ├── .prettierrc
│   ├── public/
│   │   └── favicon.svg
│   ├── src/
│   │   ├── main.tsx                             # App entry point
│   │   ├── App.tsx                              # Root component + providers
│   │   ├── index.css                            # Tailwind CSS v4 imports
│   │   ├── vite-env.d.ts
│   │   ├── test-utils.tsx                       # Custom render with providers (QueryClient, Router, Auth)
│   │   ├── components/
│   │   │   ├── StatusBadge.tsx                  # Shared status indicator
│   │   │   ├── StatusBadge.types.ts
│   │   │   ├── StatusBadge.test.tsx
│   │   │   ├── ActionButton.tsx                 # Primary/Secondary/Destructive variants
│   │   │   ├── ActionButton.test.tsx
│   │   │   ├── EmptyState.tsx                   # Icon + text + action pattern
│   │   │   ├── EmptyState.test.tsx
│   │   │   ├── Toast/
│   │   │   │   ├── ToastProvider.tsx
│   │   │   │   ├── useToast.ts
│   │   │   │   └── Toast.test.tsx
│   │   │   ├── SkeletonLoader.tsx               # Loading placeholder
│   │   │   ├── ErrorBoundary.tsx                # Feature-level error boundary
│   │   │   └── PaginationControls.tsx           # Shared pagination
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   │   ├── AuthContext.tsx               # Auth state + token management
│   │   │   │   ├── AuthContext.test.tsx
│   │   │   │   ├── LoginRedirect.tsx
│   │   │   │   └── ProtectedRoute.tsx
│   │   │   ├── recruitments/
│   │   │   │   ├── RecruitmentList.tsx
│   │   │   │   ├── RecruitmentList.test.tsx
│   │   │   │   ├── CreateRecruitmentForm.tsx
│   │   │   │   ├── EditRecruitmentForm.tsx
│   │   │   │   ├── WorkflowStepEditor.tsx       # Add/remove/reorder steps
│   │   │   │   ├── CloseRecruitmentDialog.tsx
│   │   │   │   └── hooks/
│   │   │   │       ├── useRecruitments.ts
│   │   │   │       └── useRecruitmentMutations.ts
│   │   │   ├── candidates/
│   │   │   │   ├── CandidateList.tsx
│   │   │   │   ├── CandidateList.test.tsx
│   │   │   │   ├── CandidateDetail.tsx
│   │   │   │   ├── CreateCandidateForm.tsx
│   │   │   │   ├── ImportFlow/
│   │   │   │   │   ├── ImportWizard.tsx         # Multi-step import UX
│   │   │   │   │   ├── FileUploadStep.tsx
│   │   │   │   │   ├── ImportProgress.tsx       # Polling-based progress
│   │   │   │   │   ├── ImportSummary.tsx        # Created/updated/errored detail
│   │   │   │   │   ├── MatchReviewStep.tsx      # Low-confidence match review
│   │   │   │   │   └── WorkdayGuide.tsx         # FR55: Contextual export instructions
│   │   │   │   └── hooks/
│   │   │   │       ├── useCandidates.ts
│   │   │   │       └── useImportSession.ts
│   │   │   ├── screening/
│   │   │   │   ├── ScreeningLayout.tsx          # Split-panel container
│   │   │   │   ├── ScreeningLayout.test.tsx
│   │   │   │   ├── CandidatePanel.tsx           # Left panel: candidate list + nav
│   │   │   │   ├── PdfViewer.tsx                # Right panel: react-pdf + SAS URL
│   │   │   │   ├── OutcomeForm.tsx              # Outcome recording + reason
│   │   │   │   ├── OutcomeForm.test.tsx
│   │   │   │   └── hooks/
│   │   │   │       ├── useScreeningSession.ts   # Session state coordination
│   │   │   │       ├── usePdfPrefetch.ts        # Pre-fetch N+1, N+2
│   │   │   │       └── useKeyboardNavigation.ts # Keyboard-first screening
│   │   │   ├── overview/
│   │   │   │   ├── OverviewDashboard.tsx
│   │   │   │   ├── OverviewDashboard.test.tsx
│   │   │   │   ├── StepSummaryCard.tsx          # Per-step candidate count + stale indicator
│   │   │   │   ├── PendingActionsPanel.tsx
│   │   │   │   └── hooks/
│   │   │   │       └── useRecruitmentOverview.ts
│   │   │   ├── team/
│   │   │   │   ├── MemberList.tsx
│   │   │   │   ├── MemberList.test.tsx
│   │   │   │   ├── InviteMemberDialog.tsx       # Directory search + invite
│   │   │   │   └── hooks/
│   │   │   │       └── useTeamMembers.ts
│   │   │   └── audit/
│   │   │       ├── AuditTrail.tsx
│   │   │       └── hooks/
│   │   │           └── useAuditTrail.ts
│   │   ├── hooks/
│   │   │   ├── useDebounce.ts                   # Shared debounce for search
│   │   │   └── useFocusReturn.ts                # Focus management utility
│   │   ├── lib/
│   │   │   ├── api/
│   │   │   │   ├── httpClient.ts                # Base fetch wrapper + auth token + Problem Details + 401 redirect
│   │   │   │   ├── recruitments.ts
│   │   │   │   ├── recruitments.types.ts
│   │   │   │   ├── candidates.ts
│   │   │   │   ├── candidates.types.ts
│   │   │   │   ├── import.ts
│   │   │   │   ├── import.types.ts
│   │   │   │   ├── screening.ts
│   │   │   │   ├── screening.types.ts
│   │   │   │   ├── team.ts
│   │   │   │   ├── team.types.ts
│   │   │   │   ├── audit.ts
│   │   │   │   └── audit.types.ts
│   │   │   │   # No documents.ts — document operations are candidate-scoped (in candidates.ts)
│   │   │   └── utils/
│   │   │       ├── formatDate.ts                # Intl.DateTimeFormat wrapper
│   │   │       ├── problemDetails.ts            # RFC 9457 error parser
│   │   │       └── problemDetails.test.ts       # Critical utility coverage
│   │   ├── mocks/                               # MSW mock handlers for testing
│   │   │   ├── handlers.ts                      # Shared MSW request handlers
│   │   │   ├── server.ts                        # MSW server setup for tests
│   │   │   └── fixtures/                        # Reusable mock data
│   │   │       ├── recruitments.ts
│   │   │       ├── candidates.ts
│   │   │       └── importSessions.ts
│   │   └── routes/
│   │       └── index.tsx                        # React Router v7 route definitions
│   └── e2e/
│       ├── playwright.config.ts
│       └── specs/
│           ├── auth.spec.ts
│           ├── recruitment-crud.spec.ts
│           ├── import-flow.spec.ts
│           ├── screening-flow.spec.ts
│           └── team-management.spec.ts          # Invite/remove + access control
└── _bmad/                                       # BMAD workflow artifacts (existing)
└── _bmad-output/                                # BMAD output artifacts (existing)
```

### Architectural Boundaries

**API Boundaries:**

| Boundary | Enforced By | Pattern |
|----------|------------|---------|
| External → API | Entra ID OIDC + TLS | JWT bearer tokens on every request |
| API → Feature handlers | MediatR pipeline | Validation → Logging → Audit → Handler |
| Feature handlers → Data | EF Core via `IApplicationDbContext` | `ITenantContext` global query filters |
| API → Blob Storage | `IBlobStorageService` | SAS tokens generated server-side, never direct frontend access |
| API → Entra ID Directory | `EntraIdDirectoryService` | Graph API for member search only |
| User identity | `ICurrentUserService` | Application-layer interface, Infrastructure implementation extracts from JWT |

**Component Boundaries (Frontend):**

| Boundary | Isolation Method | Communication |
|----------|-----------------|---------------|
| Feature modules | Self-contained folders | Features never import from other features |
| Server state | TanStack Query cache | Features read/write via query keys, not shared state objects |
| URL state | React Router params | `recruitmentId`, `candidateId` in URL — source of truth |
| Auth state | React Context | `AuthContext` provides user identity, consumed by any feature |
| Screening session | Component-local + Context | Three panels coordinate via `useScreeningSession` hook, isolated re-renders |
| Test isolation | MSW mock server | `mocks/handlers.ts` provides consistent API mocking across all component tests |

**Cross-feature rule:** Features communicate only through URL params (navigation), TanStack Query cache (data), and shared contexts (auth). No direct imports between feature folders.

**Data Boundaries:**

| Boundary | Storage | Access Pattern |
|----------|---------|----------------|
| Structured data | Azure SQL | EF Core, global query filters via `ITenantContext` |
| Documents (PDFs) | Azure Blob Storage | Server-side SAS token generation, react-pdf rendering |
| Audit trail | Azure SQL (append-only) | Insert-only from `AuditBehaviour`, read via `GetAuditTrail` query |
| Session/cache | None (stateless API) | TanStack Query client-side cache; no server-side session state |

**Document Access Boundary:**

Batch SAS URLs are embedded in the `GetCandidates` query response (Decision #6 — no separate endpoint needed for screening). The `GetCandidateById` query includes a single SAS URL for the candidate detail view. Document upload is exposed via `CandidateEndpoints.cs` as `POST /api/recruitments/{id}/candidates/{candidateId}/document`. No standalone document endpoints — all document operations are scoped to a candidate within a recruitment.

### Requirements to Structure Mapping

**FR Category → Backend + Frontend Locations:**

| FR Category | Backend Location | Frontend Location |
|-------------|-----------------|-------------------|
| Authentication (FR1-3) | `Web/Middleware/`, `Infrastructure/Identity/` | `features/auth/` |
| Recruitment Lifecycle (FR4-13) | `Features/Recruitments/` | `features/recruitments/` |
| Team Management (FR56-61) | `Features/Team/` | `features/team/` |
| Candidate Import (FR14-26) | `Features/Import/`, `Infrastructure/Services/ImportPipelineHostedService.cs`, `XlsxParser*`, `CandidateMatching*` | `features/candidates/ImportFlow/` |
| CV Document Mgmt (FR28-35) | `Features/Candidates/Commands/UploadDocument/`, `Infrastructure/Services/PdfSplitter*`, `BlobStorage*` | `features/screening/PdfViewer.tsx`, `features/candidates/CandidateDetail.tsx` |
| Workflow & Outcomes (FR36-40) | `Features/Screening/`, `Domain/Entities/WorkflowStep.cs`, `CandidateOutcome.cs` | `features/screening/OutcomeForm.tsx` |
| Batch Screening (FR41-46) | `Features/Candidates/Queries/GetCandidates/` (batch SAS URLs), `Features/Screening/` | `features/screening/` (entire folder) |
| Overview & Monitoring (FR47-51) | `Features/Recruitments/Queries/GetRecruitmentOverview/` | `features/overview/` |
| Audit & Compliance (FR52-55) | `Features/Audit/`, `Application/Common/Behaviours/AuditBehaviour.cs` | `features/audit/`, `features/candidates/ImportFlow/WorkdayGuide.tsx` |
| Workflow Defaults (FR62-63) | `Domain/Entities/Recruitment.cs` (default step placement), `appsettings.json` (GDPR config) | N/A (server-side only) |

**Cross-Cutting Concerns → Locations:**

| Concern | Backend Location | Frontend Location |
|---------|-----------------|-------------------|
| Per-recruitment access | `Infrastructure/Identity/TenantContext.cs`, `Web/Middleware/TenantContextMiddleware.cs`, EF global query filters | React Router guards + TanStack Query scoped by `recruitmentId` |
| Audit trail | `Application/Common/Behaviours/AuditBehaviour.cs` (MediatR pipeline) | `features/audit/AuditTrail.tsx` (read-only display) |
| GDPR retention | `Infrastructure/Services/GdprRetentionService.cs` | N/A (background job only) |
| Error handling | `Web/Middleware/ExceptionHandlingMiddleware.cs` → Problem Details | `lib/utils/problemDetails.ts`, TanStack Query `onError`, `components/Toast/` |
| Accessibility | N/A | All `components/` (ARIA, keyboard, focus), `features/screening/hooks/useKeyboardNavigation.ts` |
| Input validation | `Application/Common/Behaviours/ValidationBehaviour.cs` (FluentValidation) | On-blur + on-submit field validation (frontend is UX convenience only) |
| Test infrastructure | `CustomWebApplicationFactory.cs` | `test-utils.tsx` (provider wrappers), `mocks/` (MSW handlers + fixtures) |

### Integration Points

**Internal Communication:**

```
Browser → (HTTPS) → Vite dev proxy → ASP.NET Core Minimal API
                                        ↓
                              MediatR Pipeline (Validation → Logging → Audit → Handler)
                                        ↓
                              EF Core (ITenantContext filtered) → Azure SQL
                              IBlobStorageService → Azure Blob Storage
```

**Background Processing:**

```
ImportEndpoints (POST /api/recruitments/{id}/import)
  → Returns 202 + import session ID
  → Writes to Channel<T>
  → ImportPipelineHostedService (IHostedService) consumes from channel
      → IXlsxParser → ICandidateMatchingEngine → EF Core upsert
      → IPdfSplitter → IBlobStorageService → name-based matching
      → Updates ImportSession entity (progress, results)

GdprRetentionService (IHostedService, daily timer)
  → Queries expired recruitments (ITenantContext.IsServiceContext = true)
  → Anonymizes PII, preserves aggregate metrics
  → Deletes blob documents
```

**External Integrations:**

| Integration | Direction | Protocol | Location |
|-------------|-----------|----------|----------|
| Microsoft Entra ID (auth) | Outbound | OIDC / OAuth 2.0 | `Web/Configuration/AuthenticationConfiguration.cs`, `features/auth/AuthContext.tsx` |
| Microsoft Entra ID (directory) | Outbound | Microsoft Graph API | `Infrastructure/Identity/EntraIdDirectoryService.cs` |
| Azure Blob Storage | Outbound | Azure SDK + SAS tokens | `Infrastructure/Services/BlobStorageService.cs` |
| Azure SQL | Outbound | EF Core / SQL | `Infrastructure/Data/ApplicationDbContext.cs` |
| Application Insights | Outbound | Aspire telemetry | `aspire/ServiceDefaults/Extensions.cs` |

**Data Flow — Import Pipeline (highest complexity):**

```
1. User uploads XLSX + PDF bundle
2. API validates files (type, size) → 400 if invalid
3. API creates ImportSession (Processing) → returns 202
4. API writes to Channel<T> → ImportPipelineHostedService picks up
5. XLSX parsing → candidate rows extracted (configurable column mapping)
6. Matching engine → email match (high confidence) or name+phone (low confidence)
7. Upsert candidates → created/updated/flagged counts tracked
8. PDF splitting → TOC parsed, individual PDFs extracted
9. PDF matching → normalized name comparison to candidates
10. Documents stored in Blob Storage → SAS URLs generated on read
11. ImportSession updated (Completed/Failed) with row-level detail
12. Frontend polls GET /api/import-sessions/{id} → renders summary
```

### File Organization Patterns

**Configuration Files:**
- Root: `.gitignore`, `.github/workflows/ci.yml`
- Backend: `appsettings.json` / `appsettings.Development.json` for non-secrets, Azure Key Vault for secrets
- Frontend: `.env.example` for documentation, `vite.config.ts` for build + API proxy
- Aspire: `AppHost/Program.cs` orchestrates Azure service connections in development

**Source Organization:**
- Backend follows Clean Architecture layers (Domain → Application → Infrastructure → Web), no cross-layer shortcuts
- Frontend follows feature-based organization, with shared components at the top level
- API client modules in `lib/api/` with co-located `.types.ts` files
- `httpClient.ts` is the single point for auth token attachment, Problem Details parsing, and 401 redirect

**Test Organization:**
- Backend: Separate test projects per layer (matches Jason Taylor template convention)
- Frontend: Co-located test files (`Component.test.tsx` next to `Component.tsx`)
- Frontend test utilities: `test-utils.tsx` provides custom render with all providers (QueryClient, Router, Auth)
- Frontend API mocks: `mocks/` directory with MSW handlers and reusable fixtures per feature
- E2E: Separate `e2e/` folder in `web/` with Playwright specs covering auth, CRUD, import, screening, and team management

### Development Workflow Integration

**Development Server Structure:**
- .NET API: `dotnet run --project api/src/Web` on `localhost:5001`
- Vite dev server: `npm run dev` in `web/` on `localhost:5173`
- Vite proxy: `/api/*` → `localhost:5001` configured in `vite.config.ts`
- Aspire (optional): orchestrates API + Azure SQL + Blob Storage emulation

**Build Process Structure:**
- Backend: `dotnet build api/api.sln` → `dotnet test` → `dotnet publish`
- Frontend: `npm run build` in `web/` → static files in `web/dist/`
- CI: Single GitHub Actions pipeline builds and tests both on every PR

**Deployment Structure:**
- API → Azure App Service (via `azd deploy` or GitHub Actions)
- Frontend → Azure Static Web Apps (built from `web/dist/`)
- Database → Azure SQL (EF Core migrations)
- Documents → Azure Blob Storage
- Secrets → Azure Key Vault (App Service native references)

## Architecture Validation Results

### Coherence Validation

**Decision Compatibility:**

All technology choices are compatible and current:

| Stack Layer | Technologies | Compatibility |
|-------------|-------------|---------------|
| Backend | .NET 10, EF Core, MediatR, FluentValidation, NSubstitute, Aspire | Jason Taylor template bundles all — verified compatible |
| Frontend | React 19, Vite 7, Tailwind CSS v4, TanStack Query, React Router v7, MSAL React | Standard modern React stack, all actively maintained |
| Auth | Entra ID OIDC → MSAL.js (PKCE) → JWT → ASP.NET Core middleware → AuthContext | Standard OAuth 2.0 flow, well-documented integration path |
| Async | IHostedService + Channel\<T\> | Built into .NET — zero external dependencies |
| Storage | Azure SQL (EF Core) + Azure Blob Storage (Azure SDK) | Standard Azure pairing, Aspire orchestrates both |

No contradictory decisions found. All version choices align.

**Pattern Consistency:**

- CQRS via MediatR on backend maps cleanly to feature-based frontend folders — consistent vertical slicing
- `ITenantContext` is the single data isolation mechanism across all execution contexts — no competing patterns
- Problem Details (RFC 9457) is the single error contract, parsed by `problemDetails.ts` on frontend — consistent
- Manual DTO mapping via `From()` / `ToDto()` — no hidden mapping magic
- NSubstitute for all backend test mocking — no library divergence
- MSW for all frontend API mocking — consistent with `mocks/` directory convention
- `httpClient.ts` is the single HTTP entry point — auth, errors, and 401 redirect handled in one place

**Structure Alignment:**

- Clean Architecture layers match Jason Taylor template output
- Frontend feature folders map 1:1 to backend feature folders
- Document operations correctly scoped to candidates (no orphaned `DocumentEndpoints`)
- Test projects mirror source structure (backend) and co-locate with source (frontend)
- Shared frontend components gated as implementation step 5 — before any feature UI work

### Requirements Coverage Validation

**Functional Requirements Coverage (62 FRs):**

| FR Category | FRs | Architectural Support | Status |
|-------------|-----|----------------------|--------|
| Authentication & Access | FR1-3 | Entra ID OIDC via MSAL, middleware, AuthContext | Covered |
| Recruitment Lifecycle | FR4-13 | Features/Recruitments/ commands + queries, domain entities | Covered |
| Team Management | FR56-61 | Features/Team/, EntraIdDirectoryService, AuditBehaviour | Covered |
| Candidate Import | FR14-26 | Features/Import/, ImportPipelineHostedService, XlsxParser, CandidateMatching | Covered |
| CV Document Mgmt | FR28-35 | Features/Candidates/Commands/UploadDocument/, PdfSplitter, BlobStorage, SAS tokens | Covered |
| Workflow & Outcomes | FR36-40 | Features/Screening/, WorkflowStep + CandidateOutcome domain entities | Covered |
| Batch Screening | FR41-46 | features/screening/ split-panel, useKeyboardNavigation, usePdfPrefetch, batch SAS URLs | Covered |
| Overview & Monitoring | FR47-51 | GetRecruitmentOverview query (computed GROUP BY), StepSummaryCard stale indicator | Covered |
| Audit & Compliance | FR52-55 | AuditBehaviour MediatR pipeline, Features/Audit/, WorkdayGuide.tsx (FR55) | Covered |
| Workflow Defaults | FR62-63 | Domain entity defaults, appsettings.json for GDPR retention config | Covered |

**Non-Functional Requirements Coverage (34 NFRs):**

| NFR Category | NFRs | Architectural Support | Status |
|-------------|------|----------------------|--------|
| Performance (10) | NFR1-10 | SPA routing (300ms), computed GROUP BY (500ms), SAS tokens (2s PDF), async import with polling | Covered |
| Security (11) | NFR11-21 | Entra ID SSO via MSAL, TLS, encryption at rest, SAS tokens (15min), ITenantContext, AuditBehaviour, file validation, noindex, Azure PITR | Covered |
| Accessibility (8) | NFR22-29 | WCAG 2.1 AA, keyboard-first screening, useKeyboardNavigation, useFocusReturn, ARIA, StatusBadge (shape+icon per NFR27) | Covered |
| Integration (5) | NFR30-34 | Configurable XLSX column mapping, PDF bundle graceful failure, Blob separated from DB, Azure SQL ACID, REST + Problem Details | Covered |

All 62 FRs and 34 NFRs have architectural support. No coverage gaps.

### Implementation Readiness Validation

**Decision Completeness:**
- All critical decisions documented with specific technology versions and library names
- Authentication library (MSAL) explicitly named — no ambiguity
- Backend mocking library (NSubstitute) explicitly specified
- Code examples provided for: DTO mapping, API client pattern, httpClient foundation, TanStack Query hooks, audit events

**Structure Completeness:**
- Complete project tree with every file and directory defined
- All integration points specified with protocols and directions
- Component boundaries documented with isolation methods
- Document operations correctly scoped to candidates — no orphaned endpoints or API modules

**Pattern Completeness:**
- Naming conventions cover all layers (C#, SQL, API, TypeScript)
- Error handling chain specified end-to-end (domain exceptions → Problem Details → problemDetails.ts → Toast/inline)
- httpClient.ts pattern provides concrete implementation for auth token, error parsing, and 401 redirect
- Test infrastructure specified: NSubstitute (backend), MSW + test-utils.tsx (frontend)
- Shared frontend components gated before feature work in implementation sequence

### Gap Analysis Results

**Resolved Open Questions:**

| Open Question | Resolution |
|--------------|-----------|
| Per-recruitment access control | Resolved: EF Core global query filters + endpoint authorization via ITenantContext |
| Import session correlation model | Resolved: ImportSession.Id (GUID) as correlation ID spanning all import phases |
| Authentication library | Resolved: @azure/msal-browser + @azure/msal-react (party mode review) |
| Backend mocking convention | Resolved: NSubstitute per Jason Taylor template (party mode review) |

**Minor Inconsistency Resolved:**

Frontend `documents.ts` / `documents.types.ts` removed — all document operations are candidate-scoped. Upload via `candidates.ts`, SAS URLs via candidate list/detail responses.

**No Critical or Important Gaps Remaining.**

### Architecture Completeness Checklist

**Requirements Analysis**

- [x] Project context thoroughly analyzed (62 FRs, 34 NFRs, scale, complexity)
- [x] Scale and complexity assessed (15 users, 150 candidates, medium complexity)
- [x] Technical constraints identified (10 constraints from PRD/Brief)
- [x] Cross-cutting concerns mapped (7 concerns with implementation patterns)
- [x] Scope boundaries defined (PRD vs Brief alignment, accommodate vs defer)

**Architectural Decisions**

- [x] Critical decisions documented with rationale (5 blocking decisions)
- [x] Important decisions documented (5 shaping decisions)
- [x] Deferred decisions explicitly listed with rationale (5 post-MVP)
- [x] Technology stack fully specified with versions and library names
- [x] Data architecture defined (Azure SQL, EF Core, ITenantContext, computed overview)
- [x] Authentication & security defined (Entra ID via MSAL, defense in depth, SAS tokens)
- [x] API patterns defined (REST, Problem Details, async import)
- [x] Frontend architecture defined (React, MSAL, TanStack Query, feature-based, 1280px min viewport)
- [x] Infrastructure & deployment defined (App Service, Static Web Apps, Key Vault)

**Implementation Patterns**

- [x] Naming conventions established (backend, database, API, frontend)
- [x] Structure patterns defined (Clean Architecture, feature-based frontend)
- [x] Format patterns specified (API responses, data formats)
- [x] Communication patterns documented (MediatR events, audit events)
- [x] Process patterns defined (error handling, loading states, validation timing)
- [x] UI consistency rules established (StatusBadge, ActionButton, Toast, EmptyState + onboarding)
- [x] Enforcement guidelines documented (ESLint, dotnet format, CI, agent rules)
- [x] Test conventions specified (NSubstitute backend, MSW + test-utils frontend)
- [x] HTTP client foundation pattern documented with code example

**Project Structure**

- [x] Complete directory structure defined with all files
- [x] Architectural boundaries documented (API, component, data)
- [x] Requirements mapped to specific directories (10 FR categories → locations)
- [x] Cross-cutting concerns mapped to locations (7 concerns → files)
- [x] Integration points documented (internal, background, external, data flow)
- [x] Development workflow specified (dev servers, proxy, Aspire)
- [x] Build and deployment structure defined

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** High

Two rounds of party mode review (Steps 6 and 7) caught 18 practical issues — all resolved in the document. The architecture is specific enough for AI agents to implement consistently and comprehensive enough to prevent cross-agent conflicts.

**Key Strengths:**
- Clear vertical slicing from PRD requirements through backend features to frontend features — every FR has a traceable path
- ITenantContext provides a single, testable data isolation mechanism across all execution contexts
- Read-path vs write-path optimization simplifies daily-use code paths at the expense of setup-time complexity — correct trade-off
- Comprehensive naming, pattern, and test conventions eliminate ambiguity
- Concrete code examples for key patterns (DTO mapping, API client, httpClient, TanStack Query hooks)
- Shared frontend components and test infrastructure gated before feature work

**Areas for Future Enhancement:**
- Typed API client generation (evaluate during first API integration story)
- SignalR real-time updates (deferred to Growth phase)
- Role-based access control enforcement (schema accommodated, logic deferred)
- Advanced monitoring dashboards (Application Insights baseline via Aspire, custom deferred)

### Implementation Handoff

**AI Agent Guidelines:**

- Follow all architectural decisions exactly as documented
- Use implementation patterns consistently across all components
- Respect project structure and boundaries — file placement is non-negotiable
- Use shared components (StatusBadge, ActionButton, Toast, EmptyState) — never create feature-local equivalents
- Use NSubstitute for backend test mocking, MSW for frontend API mocking
- Use httpClient.ts as the single HTTP entry point — never call fetch directly from API modules
- Refer to this document for all architectural questions

**Scaffolding Story Acceptance Criteria:**

The first implementation story (project scaffolding) is complete when:
- [ ] `api/` initializes via Jason Taylor template (`dotnet new ca-sln -o api --database SqlServer`)
- [ ] `web/` initializes via Vite template (`npm create vite@latest web -- --template react-ts`)
- [ ] `dotnet build` succeeds, `dotnet test` passes (template's default tests)
- [ ] `npm run dev` starts Vite dev server, `npm run build` produces `dist/`
- [ ] Vite proxy routes `/api/*` to .NET backend in `vite.config.ts`
- [ ] Tailwind CSS v4 installed and configured via `@tailwindcss/vite`
- [ ] MSAL packages installed (`@azure/msal-browser`, `@azure/msal-react`)
- [ ] Vitest + Testing Library + MSW installed and configured
- [ ] `test-utils.tsx` created with custom render wrapping providers
- [ ] `mocks/server.ts` + `mocks/handlers.ts` created with MSW setup
- [ ] ESLint + Prettier configured with import ordering rules
- [ ] `.editorconfig` configured for backend
- [ ] CI pipeline (`.github/workflows/ci.yml`) builds and tests both `api/` and `web/`
- [ ] Monorepo structure matches the architecture document's project tree

**First Implementation Priority:**

```bash
# Backend
dotnet new install Clean.Architecture.Solution.Template::10.0.0
dotnet new ca-sln -o api --database SqlServer

# Frontend
npm create vite@latest web -- --template react-ts
cd web && npm install tailwindcss @tailwindcss/vite
```

Follow with authentication (Entra ID + OIDC via MSAL), then data model + ITenantContext + cross-recruitment isolation tests, then shared frontend components.
