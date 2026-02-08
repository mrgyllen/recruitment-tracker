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
shardedAt: '2026-02-08'
project_name: 'recruitment-tracker'
user_name: 'MrGyllen'
date: '2026-02-02'
---

# Architecture Decision Document

_This is the core architecture document. It contains project context, critical decisions, data architecture, authentication & security, and enforcement guidelines that every agent must read. For topic-specific details, see the [Architecture Shard Index](./architecture/index.md)._

## Architecture Shards

This document has been sharded for focused context loading. Load the core (this file) always, then add topic shards as needed:

| Your Task | Load These |
|-----------|-----------|
| Any backend story | This file + [`patterns-backend.md`](./architecture/patterns-backend.md) |
| Any frontend story | This file + [`patterns-frontend.md`](./architecture/patterns-frontend.md) |
| Full-stack story | This file + [`patterns-backend.md`](./architecture/patterns-backend.md) + [`patterns-frontend.md`](./architecture/patterns-frontend.md) |
| New API endpoint | Add [`api-patterns.md`](./architecture/api-patterns.md) |
| Frontend feature with new views | Add [`frontend-architecture.md`](./architecture/frontend-architecture.md) |
| Creating new files/folders | Add [`project-structure.md`](./architecture/project-structure.md) |
| Auth-related work | Add [`dev-auth-patterns.md`](./architecture/dev-auth-patterns.md) |
| DevOps/deployment | Add [`infrastructure.md`](./architecture/infrastructure.md) |

Full shard index with section-level anchors: [`architecture/index.md`](./architecture/index.md)

---

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

3. **Per-Recruitment Access Control** — Not global roles. Every data query filtered by recruitment membership. Implementation: EF Core global query filters + endpoint-level authorization via `ITenantContext`.

4. **Idempotency** — PRD emphasizes idempotent re-import, but idempotency as a design principle should influence the entire API layer, not just imports.

5. **Accessibility (WCAG 2.1 AA)** — Keyboard navigation, focus management, ARIA attributes, color-independent indicators. Must be designed into components, not bolted on.

6. **Error Handling & Validation** — Import pipeline has multiple failure modes. All must surface clear, actionable feedback without exposing internals.

7. **Structured Logging** — Correlation IDs, zero PII, Application Insights integration. Import session ID (GUID) as correlation ID spanning all import phases.

---

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

---

## Data Architecture

**Database:** Azure SQL with Entity Framework Core (Code First)

**Modeling approach:** Rich domain entities in the Domain project following pragmatic DDD. EF Core configurations in Infrastructure (Fluent API, no data annotations on domain entities). Entities own their business rules and invariants. Aggregates define consistency and testing boundaries.

### Aggregate Boundaries

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

### Ubiquitous Language

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

### Per-Recruitment Data Isolation via `ITenantContext`

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

---

## Authentication & Security

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

_For development authentication patterns (dev auth bypass, personas), see [`dev-auth-patterns.md`](./architecture/dev-auth-patterns.md)._

---

## Enforcement Guidelines

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

_For detailed naming, structure, and process patterns, see [`patterns-backend.md`](./architecture/patterns-backend.md) and [`patterns-frontend.md`](./architecture/patterns-frontend.md)._

---

## Implementation Handoff

**AI Agent Guidelines:**

- Follow all architectural decisions exactly as documented
- Use implementation patterns consistently across all components
- Respect project structure and boundaries — file placement is non-negotiable
- Use shared components (StatusBadge, ActionButton, Toast, EmptyState) — never create feature-local equivalents
- Use NSubstitute for backend test mocking, MSW for frontend API mocking
- Use httpClient.ts as the single HTTP entry point — never call fetch directly from API modules
- Refer to this document and relevant shards for all architectural questions

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
