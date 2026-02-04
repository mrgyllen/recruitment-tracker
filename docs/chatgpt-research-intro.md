# BMAD Research / Intro Prompt (Input for Analysis → Brainstorming / Research / Product Brief)

Copy/paste the text below into Claude Code (BMAD) as your starting prompt.

---

You are the BMAD orchestrator. Create BMAD-compliant requirements artifacts (brief + PRD + architecture outline + security model + initial backlog) for an Azure-based “Recruitment Execution” app that complements Workday.

## Context / Problem
- Our company uses Workday for recruiting, but it is poor for day-to-day execution and collaboration during recruitment.
- During a recruitment, recruiting leaders and SMEs currently coordinate via email/Teams + manual Word/Excel notes.
- Goal is to reduce admin work and friction by centralizing execution data: workflow progress, decisions, notes, reviewer assignments, and document review.
- Workday remains the long-term system of record. This app is not a Workday replacement.
- During execution, this app is the practical source of truth for execution artifacts that are not tracked elsewhere; outcomes are summarized to HR who updates Workday.

## Primary Users / Roles (product roles)
- **Recruiting Leader (admin for a recruitment case)**:
  - Full access within recruitments they create or are invited to.
  - Can configure recruitment workflow steps (template/edit), manage members/roles, assign reviewers, set candidate step states/outcomes, and close the recruitment.
- **Supportive Recruitment Role / SME (collaborator)**:
  - Can view candidate PII and documents for recruitments they are invited to.
  - Can review assigned candidates, add notes, and update statuses/outcomes as permitted.
- **Viewer (optional)**:
  - Read-only access for a recruitment (can view PII and statuses, cannot change).
- **HR role (future/optional)**:
  - May be added later; not required that HR uses the app initially.

## Authorization Roles (engineering/RBAC roles)
- Map product roles into deny-by-default RBAC roles such as:
  - **Admin** (Recruiting Leader equivalent for a recruitment)
  - **HiringManager / Recruiter** (SME/support roles; can be split if useful)
  - **Viewer** (read-only)
- Enforce authorization at API endpoints and for sensitive operations.

## Core Entities (Domain Model)
- **Recruitment**
  - id, title, owner(s), createdAt, closedAt, retentionPolicy, templateRef (optional)
  - workflowSteps: ordered list of steps (editable)
  - members: users with role per recruitment (case-level security boundary)
- **Workflow Step (definition)**
  - id, name, orderIndex, enabled, type=generic (MVP), optional metadata
- **Candidate (within a recruitment)**
  - PII fields: fullName, email, phone, location/country, dateApplied
  - Workday reference fields (informational only): workdayStage, workdayStepDisposition, lastImportedAt
  - documents: CV + optional cover letter (pdf/doc/docx), stored securely
  - assignedReviewers: list of userIds (enables “My assigned candidates” queue)
  - per-step tracking:
    - state: not_started / in_progress / done (store timestamps)
    - outcome: pending / approved / declined
    - reasonCode (configurable list) + freeTextNote
    - actor + timestamps
  - general notes/comments (thread or simple log)
  - activity/audit events (separate model; see Audit below)

## Workflow Requirements
- Each recruitment has a default workflow template that matches a typical process:
  - Example steps: Screening, Technical test (optional), Technical interview, Leader interview, Personality test (optional), Offer/Contract, Negotiation, Closed
- Recruiting leader can:
  - rename steps, reorder steps, add/remove steps, enable/disable steps
  - reuse a previous recruitment as template for a new one
- Workflow is dynamic:
  - recruitment workflow can change mid-process
  - per-candidate progress should remain consistent when steps change (define expected behavior for step add/remove/reorder)

## Import Requirements (Manual Workday Export)
- Input is a Workday-exported XLSX file (example provided). Format includes:
  - group header row, then real column header row (data starts at row 3)
  - columns like Candidate Name, Email, Complete Phone Number/Phone, Location, Date Applied, Stage, Step/Disposition, CV and Documents
- Import is manual upload. No Workday API integration. No pushback integration.
- Candidate identity matching rules (within a recruitment):
  - primary: email (case-insensitive)
  - fallback if email missing: (fullName + phone) with a “low-confidence match” flag
- Upsert behavior on import (for recruitment R):
  - if candidate not found: create candidate record; mark as new in app metadata
  - if candidate found: update only candidate profile fields and Workday reference fields
  - do NOT overwrite the app’s workflow states/outcomes/reason codes/notes/comments
- If a candidate is missing from a later import:
  - do not delete automatically
  - flag missingInLatestImport=true and store lastSeenInImportAt
- Create an Import Session record for each upload:
  - recruitmentId, uploadedBy, uploadedAt, sourceFilename
  - summary counts: processed / created / updated / missing-flagged / errors
  - row-level errors (e.g., missing email)

## Document Handling Requirements (PII/Security)
- Store documents securely (CV/letter). Support PDF and Word (doc/docx).
- Prefer in-app viewing to reduce downloads. Downloads must be explicit and logged.
- Access control: only members of that recruitment can view documents.
- Treat all candidate-related information as PII, including free-text notes and document content.

## Compliance Focus (GDPR / PII)
- Primary compliance driver is GDPR/PII; DORA is not a hard availability driver for this tool.
- Apply data minimization and explicit purpose limitation.
- Retention policy:
  - configurable per recruitment (default suggestion: 12 months after recruitment closure)
  - after retention: anonymize recruitment for historical metrics (counts/funnel/timestamps) and delete direct identifiers + documents
- Support DSAR-like needs:
  - export candidate data (when required)
  - delete/anonymize candidate data (and associated artifacts) per request, aligned with company policy
- Ensure encryption in transit and at rest; platform defaults are acceptable but must be explicitly verified/confirmed in the solution design.

## Audit, Logging, and Observability
- Logging:
  - structured logging with correlation IDs
  - no PII in logs; redact/mask where needed
- Telemetry:
  - basic metrics/telemetry using Azure-native tooling (e.g., Application Insights)
- Audit trail (minimal but real) for sensitive actions:
  - create/update/delete candidate
  - access to candidate details
  - document view/download
  - import sessions
  - export/delete operations
  - recruitment membership/role changes
  - audit events must not store unnecessary PII (use ids and minimal context)

## UX / Usability Goals
- Primary goal: reduce admin overhead and provide clear shared status without creating a heavy HR system.
- Home screen: list active recruitments with quick open; short summary (candidate counts by outcome; pending actions).
- “My assignments” view: candidates assigned to the user with next actions.
- Candidate list: filter/sort by step state/outcome/assigned reviewer; search by name/email.
- Candidate detail: step checklist with status/outcome/reason/notes + document viewer.

## Non-Goals (MVP)
- No Teams app or deep Teams integration.
- No Workday API integration.
- No pushback of statuses to Workday.
- Not building a full HR suite; keep UX lightweight.

## Tech Stack & Constraints (Engineering)
- Build on Azure + .NET:
  - Backend: ASP.NET Core Web API.
  - Frontend: SPA.
  - The SPA must not access the database directly; all data access via backend API.
- Prefer a modular/layered structure with clear boundaries:
  - separation between API layer, application/domain logic, and persistence/infrastructure
  - boundaries must be testable

## Identity, Security, and Access Control (Engineering)
- Authentication:
  - Microsoft Entra ID (OIDC) for employees in production.
  - Development may use Microsoft accounts or a simulated directory, but must follow the same OIDC/OAuth patterns.
- Authorization:
  - deny-by-default RBAC; enforce at API endpoints and for sensitive operations.
- Secrets:
  - no secrets in code or repos; use Azure Key Vault and managed identity where applicable.
- Secure coding:
  - input validation on all inbound data
  - protect against relevant web vulnerabilities for the design (XSS/CSRF/SSRF, etc.)
  - least privilege for Azure resources

## Engineering Quality Gates
- Enable .NET analyzers and formatting; keep code consistent and warnings actionable.
- Treat security-relevant warnings as errors if feasible.
- CI pipeline must:
  - run tests
  - run lint/format checks
  - run basic dependency/vulnerability checks on pull requests
- Git/repo hygiene:
  - standard branching + PR workflow (feature branches + PRs)
  - correct and working .gitignore for the chosen stack
  - minimal operational files: README (setup/run/test), basic lint/format config

## Testing Approach: Compact test pyramid with TDD
- Principle: TDD (red → green → refactor) for domain and application logic; tests written first for new behavior in these layers.
- Target test mix:
  - Unit tests (majority): domain rules, workflow transitions, validations, data transformations.
  - Integration tests (some): API endpoints + authorization (role checks), persistence behavior, key workflows (create candidate → progress steps → record evaluation). Include denial paths and validation errors.
  - End-to-end/UI smoke tests (few): critical happy-path flows only (login, list/search candidates, view candidate, move step/status).
- Definition of Done (testing):
  - domain/application changes have TDD coverage incl. at least one negative case (invalid input and/or unauthorized access)
  - integration tests cover authz for sensitive endpoints and at least one end-to-end workflow
  - all tests run in CI and pass; test data must not contain real PII

## Multi-region Recruiting Context
- Recruiting may involve Nordics/Baltics and Kuala Lumpur.
- Store data in EU by default; enforce least privilege and auditability for access.

## Deliverables expected from BMAD
1) Project brief
2) PRD (functional + non-functional requirements)
3) Short architecture outline (components + data flow, boundaries, storage approach, import flow)
4) Security model (authn/authz, PII handling, retention/anonymization, threat model for MVP)
5) Initial backlog: epics + user stories with acceptance criteria, including security/compliance/testing tasks from day one
6) Definition of Done + MVP test strategy aligned to the above
