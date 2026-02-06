# Epic List

## Epic 1: Project Foundation & User Access
Users can access the application through SSO and see the initial landing experience with guidance to get started.
**FRs covered:** FR1, FR2, FR3, FR10
**Implementation scope:** Monorepo scaffolding (api/ + web/), CI/CD pipeline, Entra ID SSO (backend JWT + MSAL frontend), data model + EF Core + ITenantContext with global query filters, cross-recruitment isolation integration tests, shared frontend components (StatusBadge, ActionButton, EmptyState, Toast, SkeletonLoader, ErrorBoundary), app shell with header/routing, AuditEntry entity + AuditBehaviour infrastructure.

## Epic 2: Recruitment & Team Setup
Erik can create recruitments with configurable workflow steps, invite team members, and manage the full recruitment lifecycle including closing.
**FRs covered:** FR4, FR5, FR6, FR7, FR8, FR11, FR12, FR13, FR56, FR57, FR58, FR59, FR60, FR61, FR62
**Implementation scope:** Recruitment CRUD, workflow step editor (add/remove/reorder), team membership management with Entra ID directory search, close recruitment action (locks edits, sets retention timestamp), recruitment list with status, recruitment selector/navigation.

## Epic 3: Candidate Import & Document Management
Erik can import candidates from Workday, upload CV bundles that auto-split and match, manually create candidates, and upload individual CVs.
**FRs covered:** FR14, FR15, FR16, FR17, FR18, FR19, FR20, FR21, FR22, FR23, FR24, FR25, FR26, FR28, FR29, FR30, FR31, FR32, FR33, FR55
**Implementation scope:** Import wizard (Sheet), XLSX parsing + column mapping, async import pipeline (IHostedService + Channel<T>), candidate matching engine, PDF bundle splitting, name-based CV matching, manual assignment UI, individual CV upload, import summary with drill-down, Workday export instructions, manual candidate CRUD.

## Epic 4: Screening & Outcome Recording
Lina can screen candidates using the split-panel layout with inline CV viewing, keyboard-first outcome recording, and batch flow with auto-advance.
**FRs covered:** FR34, FR35, FR36, FR37, FR38, FR39, FR40, FR41, FR42, FR43, FR44, FR45, FR46, FR51
**Implementation scope:** Three-panel split layout (resizable), react-pdf CV viewer with pre-fetching, outcome recording with keyboard shortcuts (1/2/3), optimistic UI + 3s undo, auto-advance, candidate search/filter, candidate complete profile (imported data, documents, outcome history across all steps), workflow step enforcement.

## Epic 5: Recruitment Overview & Monitoring
Erik and the team can see pipeline status at a glance — candidate counts per step, stale indicators, and pending actions — replacing status meetings.
**FRs covered:** FR47, FR48, FR49, FR50
**Implementation scope:** Collapsible overview section with KPI cards, pipeline bar with per-step counts, stale step indicators (shape+icon), pending actions summary, overview API endpoint (computed GROUP BY), click-through to filtered candidate list.
**Parallelism note:** Epic 5 and Epic 6 can be developed in parallel with Epic 4 once Epics 1-3 are complete. The overview is a read-only query against existing tables; audit viewing reads from AuditEntry accumulated since Epic 1.

## Epic 6: Audit Trail & GDPR Compliance
Users can view the complete audit trail for a recruitment, and the system handles data retention and anonymization after recruitment close.
**FRs covered:** FR9, FR52, FR53, FR54, FR63
**Implementation scope:** Audit trail viewing UI, GDPR retention job (IHostedService daily timer), anonymization logic (preserve aggregate metrics, strip PII), configurable retention period. AuditEntry entity + AuditBehaviour recording set up in Epic 1; this epic adds viewing UI and GDPR lifecycle.

---
