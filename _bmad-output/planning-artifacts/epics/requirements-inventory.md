# Requirements Inventory

## Functional Requirements

FR1: Users can sign in using their organizational Microsoft Entra ID account (SSO)
FR2: Unauthenticated users are redirected to the SSO login flow
FR3: Users can sign out of the application
FR4: Users can create a new recruitment with a title, description, and associated job requisition reference
FR5: Users can configure the workflow steps for a recruitment (freeform text names, set sequence). A default template provides standard steps which users can rename, add to, or remove freely.
FR6: Users can view a list of all recruitments with their current status
FR7: Users can close a recruitment, which locks it from further edits and starts a GDPR retention timer
FR8: Users can view closed recruitments in read-only mode during the retention period
FR9: The system anonymizes recruitment data after the GDPR retention period expires. Anonymization preserves aggregate metrics while stripping all PII.
FR10: Users see contextual guidance when no recruitments exist, directing them to create their first recruitment
FR11: Users can edit a recruitment's title and description while it is active
FR12: Users can add and remove workflow steps on an active recruitment. Steps with recorded outcomes cannot be modified or removed. Steps with no recorded outcomes can be freely removed.
FR13: Users can navigate between recruitments they have access to from any screen
FR14: Users can import candidates by uploading a Workday XLSX file and optionally a CV bundle PDF in the same import session. Either file can also be uploaded independently.
FR15: The system extracts five fields from the XLSX: full name, email, phone, location, and date applied
FR16: The system matches imported candidates to existing records by email (primary), with name+phone as a low-confidence fallback
FR17: The system flags low-confidence matches for manual review
FR18: Users can view an import summary showing created, updated, and errored records with row-level detail
FR19: The system tracks import sessions (who uploaded, when, source filename, summary counts)
FR20: Re-importing the same file produces the same result without creating duplicates or corrupting data (idempotent)
FR21: The import never overwrites app-side data (workflow states, outcomes, reason codes, notes)
FR22: The import never auto-deletes candidates missing from a re-import
FR23: Users can manually create a candidate record within a recruitment by entering name, email, phone, location, and date applied
FR24: Users can manually remove a candidate from a recruitment
FR25: The system validates uploaded XLSX files before processing and reports clear errors when the file format is invalid or required columns are missing
FR26: The system reports count discrepancies between imported candidates and split CVs as informational notices, not errors
FR28: Users can upload a Workday CV bundle PDF for a recruitment
FR29: The system produces individual per-candidate PDF documents from an uploaded Workday CV bundle
FR30: The system extracts and stores the Workday Candidate ID from the bundle TOC as reference metadata
FR31: The system auto-matches split PDFs to imported candidates by normalized name
FR32: Users can manually assign unmatched PDFs to candidate records through the import summary
FR33: Users can manually upload a PDF document for an individual candidate, independent of the bundle import. MVP accepts PDF format only.
FR34: Users can view a candidate's individual PDF (CV + letter combined) within the application
FR35: Users can download a candidate's PDF document
FR36: Users can view which workflow step each candidate is currently at
FR37: Users can record an outcome for a candidate at their current step (Pass, Fail, Hold) with an optional freeform text reason
FR38: Users can advance a candidate to the next workflow step after recording a passing outcome
FR39: Users can view the outcome history for a candidate across all completed steps
FR40: The system enforces the configured workflow step sequence (candidates progress through steps in order)
FR41: Users can view candidates and their CV documents side-by-side in a split-panel layout
FR42: Users can navigate between candidates in the candidate list while the CV viewer and outcome form update accordingly
FR43: Users can record an outcome and move to the next candidate in a continuous flow
FR44: Users can perform the entire screening workflow using keyboard navigation alone
FR45: Users can search candidates within a recruitment by name or email
FR46: Users can filter candidates within a recruitment by current step and outcome status
FR47: Users can view a recruitment overview showing candidate counts per workflow step
FR48: Users can see visual indicators for steps that have candidates waiting beyond a configurable global threshold (default: 5 calendar days)
FR49: Users can see a summary of pending actions across the recruitment
FR50: The recruitment overview loads independently from the detailed candidate list
FR51: Users can view a candidate's complete profile including imported data, documents, and outcome history across all steps
FR52: The system records an audit entry for every state change (outcome recorded, candidate imported, document uploaded, recruitment created/closed)
FR53: Each audit entry captures who performed the action, when, and what changed
FR54: Users can view the audit trail for a recruitment
FR55: The system provides contextual Workday export instructions within the import flow
FR56: Users can invite authenticated users to a recruitment by searching the organizational directory (Entra ID)
FR57: Users can view the list of members who have access to a recruitment
FR58: Users can remove a member from a recruitment, except the recruitment creator who cannot be removed
FR59: The user who creates a recruitment is automatically added as a permanent member (cannot be removed)
FR60: Users can only view and access recruitments where they are a member
FR61: The system records member additions and removals in the audit trail
FR62: New candidates (imported or manually created) are placed at the first workflow step with outcome status "Not Started"
FR63: The GDPR retention period is configurable via deployment settings. Default: 12 months.

## NonFunctional Requirements

NFR1: Page-to-page SPA navigation completes in under 300ms (client-side routing)
NFR2: Recruitment overview endpoint returns pre-aggregated data in under 500ms
NFR3: Candidate list endpoint returns paginated results (up to 50 per page) in under 1 second
NFR4: Individual candidate PDF loads in the viewer within 2 seconds via SAS token streaming
NFR5: Step outcome save provides visual confirmation within 500ms
NFR6: Workday XLSX import (up to 150 rows) completes server-side within 10 seconds. If exceeding threshold, returns 202 Accepted with polling-based progress.
NFR7: CV bundle PDF splitting (up to 150 candidates, up to 100 MB) completes within 60 seconds. Runs asynchronously with progress reported.
NFR8: Initial application load (after SSO redirect) completes within 3 seconds on corporate network
NFR9: The application supports up to 15 concurrent authenticated users without degradation
NFR10: Import summary with row-level detail for up to 150 candidates renders within 2 seconds
NFR11: All authentication via Microsoft Entra ID SSO. No user credentials stored.
NFR12: All data in transit encrypted via TLS 1.2+
NFR13: All data at rest encrypted (database encryption and blob storage encryption)
NFR14: Candidate PII accessible only to authenticated users
NFR15: PDF documents accessible only through short-lived SAS tokens (maximum 15-minute validity)
NFR16: Application includes noindex meta tag and X-Robots-Tag: noindex response header
NFR17: Application not exposed to public DNS without authentication
NFR18: GDPR retention timer triggers automatic data deletion after configured period
NFR19: All state changes recorded in immutable audit trail (who, what, when)
NFR20: Uploaded files validated for type and size. Maximum XLSX: 10 MB. Maximum PDF bundle: 100 MB.
NFR21: Data recovery supported to any point within last 7 days (platform point-in-time restore + blob soft delete)
NFR22: Application meets WCAG 2.1 Level AA for all core user flows
NFR23: All interactive elements reachable and operable via keyboard alone
NFR24: Batch screening workflow fully operable via keyboard without mouse dependency
NFR25: All form inputs have associated labels. All images/icons have appropriate alt text or ARIA labels.
NFR26: Color contrast ratios meet WCAG AA minimums (4.5:1 normal text, 3:1 large text)
NFR27: Status indicators use shape/icon in addition to color (no color-only information)
NFR28: Dynamic content updates announced to assistive technologies via ARIA live regions
NFR29: Keyboard focus managed predictably during sequential workflows
NFR30: Workday XLSX import supports configurable column-name mapping for locale/format variations
NFR31: Workday CV bundle PDF parsing validated against known format. Fails gracefully with clear error messages.
NFR32: Document storage separated from application database. No documents in the database.
NFR33: Structured data stored in a relational database with ACID compliance
NFR34: API follows RESTful conventions with consistent error response format and appropriate HTTP status codes

## Additional Requirements

**From Architecture:**
- Starter template: Jason Taylor Clean Architecture (.NET 10) for backend, Vite + React + TypeScript for frontend
- Monorepo structure: api/ and web/ in single repository
- Three aggregate roots: Recruitment (owns WorkflowStep, RecruitmentMember), Candidate (owns CandidateOutcome, CandidateDocument), ImportSession
- ITenantContext with EF Core global query filters for per-recruitment data isolation
- Cross-recruitment isolation integration tests mandatory before feature tests
- IHostedService + Channel<T> for async import processing
- GDPR retention via IHostedService with daily timer
- Shared frontend components (StatusBadge, ActionButton, EmptyState, Toast, SkeletonLoader, ErrorBoundary) must exist before feature UI work
- Manual DTO mapping (no AutoMapper) — From() factory methods or ToDto() extensions
- NSubstitute for all backend test mocking
- MSW (Mock Service Worker) for all frontend API mocking
- httpClient.ts as single HTTP entry point (auth token, Problem Details parsing, 401 redirect)
- Problem Details (RFC 9457) for all API error responses
- Aspire integration for Azure service orchestration and telemetry
- CI/CD pipeline: single GitHub Actions workflow building and testing both api/ and web/
- Overview data: computed on read via GROUP BY query (no pre-aggregation at MVP scale)
- Implementation sequence specified: scaffolding → auth → data model → isolation tests → shared components → features

**From UX Design:**
- shadcn/ui as design system foundation (copy-paste components on Radix UI primitives)
- react-pdf (PDF.js) for inline CV rendering with text layer accessibility and per-page lazy loading
- react-virtuoso for candidate list virtualization (130+ candidates)
- react-hook-form + zod for form validation
- If Insurance brand palette: warm browns (#331e11), cream backgrounds (#faf9f7), blue interactive (#005fcc)
- Segoe UI font stack (zero loading cost on Windows corporate machines)
- Three-panel split layout with resizable divider (CSS Grid + localStorage-persisted ratios)
- Keyboard shortcuts: 1/2/3 for Pass/Fail/Hold, scoped to outcome panel only
- Optimistic UI with 3-second delayed server persist + undo affordance via bottom-right toast
- PDF pre-fetching for next 2-3 candidates during screening
- Collapsible overview section with localStorage-persisted state; collapsed shows inline summary bar
- Import wizard uses Sheet component (slides from right, full height)
- Context-adaptive candidate rows: 48px screening mode, 56px browse mode
- Dual screening progress: total ("47 of 130") + session ("12 this session")
- Auto-advance to next unscreened candidate after outcome recording
- Empty state as onboarding: value proposition + CTA for first-time users
- Recruitment selector in header breadcrumb (dropdown when multiple recruitments)
- Minimum viewport width: 1280px with message for narrower windows

## FR Coverage Map

FR1: Epic 1 - SSO sign-in via Entra ID
FR2: Epic 1 - Redirect unauthenticated users to SSO
FR3: Epic 1 - Sign out
FR4: Epic 2 - Create recruitment with title, description, job requisition reference
FR5: Epic 2 - Configure workflow steps (freeform names, sequence, default template)
FR6: Epic 2 - View recruitment list with status
FR7: Epic 2 - Close recruitment (lock edits, start GDPR retention timer)
FR8: Epic 2 - View closed recruitments in read-only mode
FR9: Epic 6 - Anonymize recruitment data after GDPR retention period
FR10: Epic 1 - Empty state guidance for first-time users
FR11: Epic 2 - Edit recruitment title and description
FR12: Epic 2 - Add/remove workflow steps (protected if outcomes exist)
FR13: Epic 2 - Navigate between recruitments from any screen
FR14: Epic 3 - Import candidates via XLSX and/or PDF bundle
FR15: Epic 3 - Extract five fields from XLSX (name, email, phone, location, date applied)
FR16: Epic 3 - Match imported candidates by email (primary), name+phone fallback
FR17: Epic 3 - Flag low-confidence matches for manual review
FR18: Epic 3 - Import summary with created/updated/errored counts and row-level detail
FR19: Epic 3 - Track import sessions (who, when, filename, counts)
FR20: Epic 3 - Idempotent re-import (no duplicates, no corruption)
FR21: Epic 3 - Import never overwrites app-side data
FR22: Epic 3 - Import never auto-deletes missing candidates
FR23: Epic 3 - Manual candidate creation
FR24: Epic 3 - Manual candidate removal
FR25: Epic 3 - XLSX validation with clear error reporting
FR26: Epic 3 - Count discrepancies between candidates and CVs reported as informational
FR28: Epic 3 - Upload Workday CV bundle PDF
FR29: Epic 3 - Split bundle into individual per-candidate PDFs
FR30: Epic 3 - Extract Workday Candidate ID from bundle TOC
FR31: Epic 3 - Auto-match split PDFs to candidates by normalized name
FR32: Epic 3 - Manual assignment of unmatched PDFs
FR33: Epic 3 - Individual PDF upload per candidate
FR34: Epic 4 - View candidate PDF in-app
FR35: Epic 4 - Download candidate PDF
FR36: Epic 4 - View candidate's current workflow step
FR37: Epic 4 - Record outcome (Pass/Fail/Hold) with optional freeform reason
FR38: Epic 4 - Advance candidate to next step after passing outcome
FR39: Epic 4 - View outcome history across all steps
FR40: Epic 4 - Enforce workflow step sequence
FR41: Epic 4 - Split-panel layout (candidate list + CV viewer + outcome form)
FR42: Epic 4 - Navigate between candidates with CV and outcome form updating
FR43: Epic 4 - Record outcome and move to next candidate in continuous flow
FR44: Epic 4 - Keyboard-only screening workflow
FR45: Epic 4 - Search candidates by name or email
FR46: Epic 4 - Filter candidates by step and outcome status
FR47: Epic 5 - Recruitment overview with candidate counts per step
FR48: Epic 5 - Stale step visual indicators (configurable threshold, default 5 days)
FR49: Epic 5 - Pending actions summary
FR50: Epic 5 - Overview loads independently from candidate list
FR51: Epic 4 - Candidate complete profile (imported data, documents, outcome history)
FR52: Epic 6 - Audit entry for every state change
FR53: Epic 6 - Audit entry captures who, when, what changed
FR54: Epic 6 - View audit trail for a recruitment
FR55: Epic 3 - Contextual Workday export instructions in import flow
FR56: Epic 2 - Invite members by searching Entra ID directory
FR57: Epic 2 - View recruitment member list
FR58: Epic 2 - Remove member (except creator)
FR59: Epic 2 - Creator auto-added as permanent member
FR60: Epic 2 - Users only see recruitments where they are a member
FR61: Epic 2 - Member changes recorded in audit trail
FR62: Epic 2 - New candidates placed at first step with "Not Started"
FR63: Epic 6 - GDPR retention period configurable via deployment settings
