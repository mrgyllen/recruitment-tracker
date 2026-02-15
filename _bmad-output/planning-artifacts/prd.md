---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-06-innovation-skipped
  - step-07-project-type
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-e-01-discovery
  - step-e-02-review
  - step-e-03-edit
lastEdited: '2026-02-15'
editHistory:
  - date: '2026-02-15'
    changes: 'Added Deployment & Infrastructure NFRs (NFR35-NFR40): CI/CD pipeline, staging environment, IaC (Bicep), containerized deployment, database migration automation, health check endpoints'
inputDocuments:
  - _bmad-output/planning-artifacts/product-brief-recruitment-tracker-2026-02-01.md
  - docs/chatgpt-research-intro.md
documentCounts:
  briefs: 1
  research: 0
  brainstorming: 0
  projectDocs: 1
classification:
  projectType: web_app
  domain: HR/Recruitment Execution
  complexity: medium
  projectContext: greenfield
workflowType: 'prd'
---

# Product Requirements Document - recruitment-tracker

**Author:** MrGyllen
**Date:** 2026-02-01

## Executive Summary

**"Workday tracks the hiring. This tool runs the hiring."**

recruitment-tracker is a lightweight web application that fills the gap between Workday's recruitment management and the day-to-day execution of hiring. Workday handles job postings and applicant intake, but once the real work begins - screening CVs, coordinating interviews, recording decisions - recruiting leaders stitch together Teams chats, emails, and spreadsheets. The leader becomes everyone's secretary.

This app replaces that pattern with a **shared status board** where every participant self-serves. SMEs log in, see candidates, read CVs in-app, and record outcomes without a Teams ping. The recruiting leader opens the app after a meeting and sees work already done. The team collaborates without the leader as bottleneck.

**Target users:**
- **Recruiting Leaders** (Erik) - Create and manage recruitments, monitor pipeline health, make progression decisions
- **Subject Matter Experts / Collaborators** (Lina, Marcus) - Screen CVs, conduct interviews, record assessments
- **Viewers** (Anders, Sara) - Monitor recruitment progress without editing

**Technology:** SPA frontend + ASP.NET Core Web API backend, deployed on Azure with Microsoft Entra ID SSO. Enterprise-grade security and GDPR-compliant PII handling.

**Differentiator:** Not a Workday replacement, not an HR system, not a project management tool. A focused execution companion for the people who actually run the hiring.

## Success Criteria

### User Success

| Criteria | Measure | Target |
|----------|---------|--------|
| **Recruiting Leader adoption** | All active recruitments managed in-app instead of Teams/email | 100% of recruitments from first use onward |
| **SME self-service** | SMEs record screening outcomes and interview assessments without leader prompting | Outcomes recorded in-app without a preceding Teams ping |
| **Status relay elimination** | Self-reported reduction in manual status update messages (Teams/email) | From multiple per week to near-zero per recruitment |
| **SME time-to-action** | Time from candidate availability to outcome recorded | Begin within 24h, complete within 48h (aspirational, friction-dependent) |
| **Viewer self-service** | Stakeholders check pipeline status without asking the leader | Understood within 10 seconds without scrolling |
| **Screening flow efficiency** | Time for SME to complete a batch screening session | 4 candidates screened in under 10 minutes (<2.5 min per candidate including CV review) |

**Testable success moments:**
- Erik opens the app after a day of meetings, sees 3 SMEs completed screening - zero messages sent
- Lina opens her queue, reads CVs in-app, and records outcomes in a continuous batch flow without page reloads between candidates
- Anders opens a recruitment and sees the full pipeline at a glance: 12 candidates, 4 past screening, 2 in interviews - no clicks into individual candidates required

### Business Success

| Objective | Indicator | Timeframe |
|-----------|-----------|-----------|
| **Reduce leader admin overhead** | Leader spends time on decisions, not information relay | Within first completed recruitment |
| **Speed up screening pipeline** | Time from "CVs available" to "all candidates screened" decreases | Compare first app-managed recruitment to previous manual ones |
| **Centralize execution data** | All candidate data, documents, outcomes, and notes in one place | From first recruitment onward |
| **GDPR compliance for execution data** | PII managed with access control, audit trails, retention policies | From launch |
| **Create pull for HR adoption** | HR partner expresses interest based on seeing value | Within 6-12 months (organic, not forced) |

### Technical Success

| Criteria | Measure |
|----------|---------|
| **Real-time audit trail** | Append-only event log from day one. Any authorized user can export a complete audit trail for a recruitment. |
| **Zero PII in logs** | Structured logging with correlation IDs, no candidate data in application logs. |
| **Retention & anonymization** | Closed recruitments anonymized on schedule. **Preserved:** candidate counts per step, conversion ratios, time-in-step averages, recruitment duration. **Stripped:** names, emails, phones, documents, free-text notes, all direct identifiers. |
| **Encryption** | At rest and in transit via Azure platform defaults. |

### Measurable Outcomes

**Adoption KPIs (first 3 months):**
- 2+ recruitments actively managed in the app
- 3-6 users actively engaged per recruitment
- All candidate documents stored in-app

**Leading indicators (early warning signals):**
- Within the first week of a new recruitment, all invited members have logged in at least once
- First screening outcome recorded within 48 hours of candidate import
- Zero Teams messages for information that exists in the app

**Efficiency KPIs (per recruitment):**
- Leader sends <5 manual status messages for the entire recruitment lifecycle
- All step outcomes recorded in-app (zero verbal-only assessments)
- SMEs begin screening work without leader follow-up

**Compliance KPIs (ongoing):**
- 100% of PDF documents accessed in-app with near-zero local downloads (MVP accepts PDF uploads only; Word-to-PDF conversion deferred to Growth phase)
- Closed recruitments anonymized within configurable retention period
- Audit trail producible on demand

**The "Would I Go Back?" KPI:**
- After one full recruitment, nobody voluntarily returns to Teams + spreadsheets. If they do, immediate failure signal.

## User Journeys

### Journey Phase Mapping

| Journey | Phase | Notes |
|---------|-------|-------|
| **J0: Erik - First Five Minutes** | MVP | Core onboarding flow |
| **J1: Erik - Running a Recruitment** | MVP | Core management flow |
| **J2: Erik - Mid-Process Disruption** | MVP | Import and workflow flexibility |
| **J3: Lina - Batch Screening** | MVP | Core screening flow |
| **J4: Lina - Technical Interview** | Growth | Requires candidate notes (not in MVP) |
| **J5: Anders - Passive Monitoring** | Growth | Requires Viewer role (not in MVP; all users have full access in MVP) |
| **J6: Sara - Viewer Validation** | Growth | Requires Viewer role |

*Journeys J4-J6 are aspirational narratives showing Growth-phase capabilities. They inform the product vision and Growth feature prioritization but are not MVP delivery targets.*

### Journey 0: Erik - First Five Minutes (Onboarding)

**Opening Scene:** Erik has heard about recruitment-tracker and opens the link for the first time. He's already skeptical - he's tried "tools that will fix everything" before. He clicks the link and his company SSO kicks in. No registration form, no password creation, no email verification. He's in.

**Rising Action:** The home screen is empty - no recruitments yet. But it's not a blank void. A clear, prominent action invites him to create his first recruitment. He clicks it, types "Senior Backend Engineer Q1 2026," and sees a default workflow template: Screening, Technical Test, Technical Interview, Leader Interview, Personality Test, Offer/Contract, Negotiation. He renames one step, disables another. The interface doesn't fight him.

**Climax:** Erik uploads his first Workday XLSX export and CV bundle PDF together. 12 candidates appear with their CVs auto-matched. 2 CVs didn't match by name - the app shows them clearly and lets him manually assign them. The recruitment is live.

**Resolution:** From link click to live recruitment with candidates: under 5 minutes. Erik thinks "that was it?" - and that's exactly the reaction we're designing for.

**Requirements revealed:** SSO zero-friction onboarding, empty state design, workflow template with easy customization, paired XLSX + PDF import with auto-match and manual assignment.

---

### Journey 1: Erik - Running a Recruitment (Success Path)

**Opening Scene:** Erik's recruitment is a week old. 12 candidates imported, CVs uploaded, team invited. He's been in back-to-back meetings for two days.

**Rising Action:** Erik opens the app between meetings. The home screen shows his recruitment with a health indicator and summary: "8 of 12 candidates screened." He clicks in and sees the full pipeline: candidates listed with their current step, outcome, and who last touched them. Lina screened 7, Marcus handled 5. Three candidates are approved for the code challenge. He didn't send a single Teams message.

**Climax:** Two weeks later, interviews are underway. Erik notices one candidate sitting in "Technical Interview" for 5 days. A subtle visual cue flags the step as stale. He follows up directly. Meanwhile, Sara (HR) asks for a status update - Erik shares his screen showing the recruitment overview. Sara gets the full picture in 10 seconds.

**Resolution:** The recruitment closes with a signed contract. Erik clicks "Close Recruitment" - the app locks the recruitment, starts the GDPR retention timer. His total coordination overhead: fewer than 5 manual status messages.

**Requirements revealed:** Home screen with health and summary counts, pipeline view with step/outcome visibility, time-based stale step cues, close recruitment action (locks edits, starts retention timer).

---

### Journey 2: Erik - Mid-Process Disruption (Edge Case)

**Opening Scene:** Erik is running parallel recruitments. One has 3 candidates sitting in "Technical Interview" for 4+ days with stale visual indicators.

**Rising Action:** He uploads a new Workday XLSX export. An import summary appears: "3 new candidates created, 9 updated (profile fields only), 0 errors." He can click into the summary for row-level detail. Nothing was overwritten - no outcomes, notes, or step states touched.

**Climax:** Erik needs an extra interview step. He adds "Director Interview" between Leader Interview and Offer/Contract. Existing candidates' progress is unaffected - the new step appears as "not started" for everyone. One new import has a low-confidence match warning (matched by name+phone, no email) - Erik reviews and confirms it.

**Resolution:** The app handled re-import safely, dynamic step changes cleanly, and surfaced concerns visually.

**Requirements revealed:** Re-import safety (upsert, never overwrite app-side data), import summary with row-level drill-down, low-confidence match flagging, dynamic step addition mid-process, import session audit log.

---

### Journey 3: Lina - Batch Screening Session (Success Path)

**Opening Scene:** Lina knows Erik started a new recruitment and added her. Tomorrow morning she has a window.

**Rising Action:** Lina opens recruitment-tracker. She sees the recruitment overview: 12 candidates, all at "Screening - Not Started." She clicks the first candidate. The interface is a **split-panel layout**: candidate list on the left, CV viewer and outcome form on the right. The CV opens as a PDF right in the browser.

**Climax:** Lina reads the first CV, records: "Approved" with a note: "Strong distributed systems experience, 3 years Kubernetes." She clicks the next candidate. The CV loads instantly - no page reload, no context loss. Decline. Next. Decline with reason code: "Insufficient experience." In 8 minutes she's screened every candidate.

**Resolution:** She didn't download a single file, open Teams, or write an email. Her outcomes are immediately visible to everyone.

**Requirements revealed:** Split-panel batch screening UX (candidate list + CV viewer + outcome form), in-app PDF viewer, quick outcome recording with optional reason codes, candidate search/filter by name and step status.

---

### Journey 4: Lina - Technical Interview Assessment (Growth Phase)

*Note: This journey requires candidate notes, a Growth-phase feature. Included to inform Growth prioritization.*

**Opening Scene:** Lina just finished a 90-minute technical interview. The candidate struggled with system design but showed strong problem-solving on the coding exercise.

**Rising Action:** She opens the candidate's detail and navigates to the "Technical Interview" step. She sets the step to "Done" with outcome "Approved" and adds a **step-level note**: "Strong coding fundamentals, creative problem-solver. System design was weak. Recommend progressing with caveat: needs mentoring on architecture."

She then adds a **general candidate note**: "Very personable, good cultural fit. Team would enjoy working with them."

**Resolution:** Erik reads both the step assessment and the general note. He has the full picture without a follow-up call.

**Requirements revealed:** Step-level notes (evaluation-specific) and general candidate notes (holistic, candidate-level). Both are first-class data in the model and UI.

---

### Journey 5: Anders - Passive Monitoring (Growth Phase)

*Note: This journey requires the Viewer role, a Growth-phase feature. Included to validate the Viewer role design.*

**Opening Scene:** Anders leads the platform team where the new hire will work. Previously he'd message Erik every few days for updates.

**Climax:** Anders opens the recruitment and immediately sees: 12 candidates total. 4 past screening. 2 in Technical Interview. 1 in Leader Interview. 5 declined or withdrawn. Recruitment health: "on track." He doesn't click into any candidate detail. The overview tells him everything in under 15 seconds.

**Resolution:** Anders stops messaging Erik for updates entirely.

**Requirements revealed:** At-a-glance overview with counts per step, recruitment health indicator, zero-click comprehension for Viewer role.

---

### Journey 6: Sara - Viewer Role Validation (Growth Phase)

*Note: This journey uses Viewer role capabilities. Included to validate organic HR adoption pull.*

**Opening Scene:** Sara manages the Workday case for Erik's recruitment. During a sync call, Erik shares his screen showing recruitment-tracker. Sara sees all candidates, all steps, all outcomes - the full picture in 10 seconds.

**Climax:** A week later, Sara asks for read access. Erik invites her. SSO logs her in. No training needed.

**Resolution:** Sara checks in independently, reducing sync calls from biweekly to as-needed. She starts mentioning the tool to other recruiting leaders. Organic pull begins.

**Requirements revealed:** Viewer role sufficient for HR visibility. SSO removes adoption friction. Screen-shareable interface design.

---

### Journey Requirements Summary

| Journey | Key Capabilities Revealed | Phase |
|---------|--------------------------|-------|
| **J0: Erik - First Five Minutes** | SSO onboarding, empty state UX, workflow customization, paired import with auto-match | MVP |
| **J1: Erik - Running a Recruitment** | Health indicator, pipeline view, stale step cues, close recruitment (lock + retention) | MVP |
| **J2: Erik - Mid-Process Disruption** | Re-import safety, import summary, low-confidence flags, dynamic step addition | MVP |
| **J3: Lina - Batch Screening** | Split-panel UX, in-app PDF viewer, quick outcome recording, search/filter | MVP |
| **J4: Lina - Technical Interview** | Step-level notes, general candidate notes | Growth |
| **J5: Anders - Passive Monitoring** | At-a-glance overview, health indicator, Viewer role | Growth |
| **J6: Sara - Viewer Validation** | Viewer role, SSO friction removal, screen-shareable design | Growth |

## Domain-Specific Requirements

*Domain: HR/Recruitment Execution | Complexity: Medium | Primary compliance driver: GDPR*

### Compliance & Regulatory

- **GDPR** is the primary compliance driver. All candidate data is PII. Requirements: access control, audit trails, encryption (at rest and in transit), configurable retention with anonymization, and data minimization.
- **DORA** is not a hard driver - this is not a critical availability system. Standard availability practices apply.
- **Data residency** - Data stored in EU by default. Recruiting spans Nordics, Baltics, and Kuala Lumpur. Enforce least privilege and auditability for all access regardless of candidate location.

### Import Data Integrity (Domain-Specific Constraint)

The Workday XLSX import has domain-specific safety rules driven by Workday's limitations and the manual nature of the import process:

- **Never overwrite app-side data** - Import updates candidate profile fields only. Workflow states, outcomes, reason codes, notes, and comments are never touched by import.
- **Never auto-delete candidates** - If a candidate is missing from a re-import, do not delete. Workday uses paging, and a missing candidate may be on another page. Manual removal only.
- **Flag low-confidence matches** - Primary match by email (case-insensitive). Fallback: name + phone with a "low-confidence match" flag for manual review. No silent assumptions.
- **Import session tracking** - Every import creates an auditable record: who uploaded, when, source filename, summary counts (created/updated/errors), row-level error reporting.
- **Idempotent re-import safety** - The same export uploaded twice produces the same result. No duplicate candidates, no data corruption.
- **Export instruction: always export all candidates.** The app handles deduplication. No date-range filtering or "new only" exports.

## Web Application Specific Requirements

### Project-Type Overview

recruitment-tracker is a **Single Page Application (SPA)** with an ASP.NET Core Web API backend, deployed on Azure. Internal enterprise tool behind Microsoft Entra ID SSO.

### Browser Support

| Browser | Support Level |
|---------|--------------|
| **Microsoft Edge (Chromium)** | Primary - fully supported and tested |
| **Google Chrome** | Primary - fully supported and tested |
| **Firefox** | Not supported |
| **Safari** | Not supported |

### Responsive Design

- **Desktop-first** - Optimized for desktop viewport widths (1280px+).
- **Tablet-tolerant** - Split-panel screening layout gracefully degrades to stacked layout on viewports narrower than ~1024px. No overlapping or clipped content.
- **Mobile** - Not a target.

### API Design Patterns

- **Recruitment overview** (`/api/recruitments/{id}/overview`) - Pre-aggregated summary: candidate counts per step, health status, pending actions.
- **Candidate list** (`/api/recruitments/{id}/candidates`) - Paginated with search by name/email and filter by step status/outcome.
- **Separation principle:** Overview and candidate list are independent endpoints.

### Document Viewing

- **PDF viewing** - Browser-native rendering via `<iframe>` or `<object>`. PDF served through authenticated API endpoint generating short-lived SAS tokens for direct Azure Blob Storage streaming.
- **Word documents** - Not accepted for upload in MVP (PDF only). Word-to-PDF conversion on upload planned for Growth phase.

### SEO Strategy

Not applicable. Internal tool behind SSO. Defense-in-depth: `noindex` meta tag, `X-Robots-Tag: noindex` response header, no public DNS exposure without authentication.

### Accessibility

- **Target:** WCAG 2.1 Level AA
- **Keyboard-first batch screening:** The entire screening flow must work with keyboard alone. This serves both accessibility compliance and power-user efficiency.
- **Key flows:** Candidate list navigation, batch screening, CV viewer, outcome recording, recruitment overview.

### Real-Time Features

- **MVP:** No real-time updates. Manual refresh. Sufficient for 3-6 concurrent users per recruitment.
- **Growth:** SignalR for live updates if concurrent usage creates staleness issues.

### SPA Framework Guidance

- **Recommendation: React or Vue** - Component-based with client-side routing. Supports split-panel layouts, PDF embedding, fast navigation.
- **Recommendation against Blazor WASM** - Startup payload too heavy for <3 second initial load target. PDF embedding is painful.
- **Final decision** deferred to architecture phase.

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Problem-solving MVP - the minimum that replaces the leader-as-secretary workflow with a shared status board. Validated by running one real recruitment end-to-end with the real team.

**Validation target:** Erik no longer manually tracks candidate statuses. The team self-serves.

**Resource model:** Solo developer with AI-assisted development. Full-stack: SPA frontend, ASP.NET Core API, Azure SQL, Azure Blob Storage.

### MVP Feature Set (Phase 1)

**Core User Journeys Supported:**
- J0: First recruitment setup (Erik creates recruitment, configures steps)
- J1: Ongoing recruitment management (Erik monitors dashboard, tracks health)
- J2: Candidate import (Erik imports from Workday XLSX)
- J3: Batch screening (Lina screens 130 CVs with split-panel UX)

**Must-Have Capabilities:**

| # | Capability | Rationale |
|---|-----------|-----------|
| 1 | **Microsoft Entra ID SSO** with per-recruitment membership (single role - all members are equal within a recruitment) | Security baseline. Without auth, nothing works. |
| 2 | **Recruitment lifecycle** (create, configure workflow steps, close with retention timer) | Core entity. Without recruitments, no candidates. |
| 3 | **Workday XLSX import** - 5 fields: full name, email, phone, location, date applied. Email-based matching, name+phone fallback with low-confidence flag, idempotent re-import. Async server-side processing. | Primary data entry path. |
| 4 | **Manual candidate creation** with individual CV upload for candidates outside Workday (referrals, direct applications) | Fallback path when candidates aren't in the Workday export. |
| 5 | **CV bundle PDF upload with split-on-upload** - Parse Workday bundle TOC, split into individual per-candidate PDFs. Store Workday Candidate ID as reference metadata. Auto-match by name; manual assignment fallback. | Individual PDFs are simpler to view, cache, and serve. |
| 6 | **Configurable workflow** with freeform step names, default 7-step template, and per-step outcome recording (Pass/Fail/Hold + optional freeform reason) | Core domain model. Steps are the whole point of tracking. |
| 7 | **Batch screening UX** (split-panel: candidate list + CV viewer + outcome form, keyboard-first) | The 130-CV scenario is the hardest UX problem. |
| 8 | **Recruitment overview dashboard** (per-step counts, stale step indicators, pending actions) via pre-aggregated API endpoint | Erik's daily view. Replaces "ask for an update." |
| 9 | **Candidate search/filter** (name, email, current step, outcome status) via paginated API endpoint | 150 candidates need search to be usable. |
| 10 | **Basic audit trail** (who changed what, when) | GDPR accountability baseline. |
| 11 | **Per-recruitment team management** (invite members, view membership, remove members; creator is permanent) | Access control. Users only see recruitments they're invited to. |

**Explicitly NOT in MVP:**
- Role-based access control (all recruitment members have equal access; no Lead/Member/Read-Only distinction)
- Candidate notes/comments
- Recruitment templates
- Advanced reporting/export
- Email notifications
- Real-time updates (manual refresh)
- Word-to-PDF conversion (Word docs download only)
- CV/letter separation within a candidate's PDF bundle

### Post-MVP Features

**Phase 2 (Growth):**

Promotion triggers: 3+ concurrent recruitments, 10+ unique users, or >200 candidates in a single recruitment.

| Feature | Depends On | Value |
|---------|-----------|-------|
| Role-based access (Lead / Member / Read-Only) | User growth beyond trusted inner circle | Security, delegation |
| Recruitment templates (save/reuse step configurations) | Pattern of repeated similar recruitments | Efficiency |
| Candidate notes and comments (threaded, per-candidate) | Team collaboration needs beyond outcomes | Communication |
| Reviewer assignment / "My Assignments" queue | >20 candidates or parallel SME screening | Workflow clarity |
| Advanced reporting and XLSX export | Management reporting requests | Visibility |
| Word-to-PDF conversion on upload | CV format inconsistency complaints | UX consistency |
| SignalR live updates | Concurrent usage causing stale data | Real-time awareness |
| CV/letter document type tagging within split PDFs | Users requesting filtered document views | Document management |

**Phase 3 (Expansion):**

| Feature | Depends On | Value |
|---------|-----------|-------|
| Multi-department support | Adoption beyond current team | Scale |
| Cross-recruitment candidate search | Repeat candidates across roles | Efficiency |
| Interview scheduling integration (Outlook/Teams) | Volume justifies integration cost | Workflow |
| Email notification system | User requests for proactive alerts | Awareness |
| Analytics dashboard with trend analysis | Enough historical data to be meaningful | Insights |
| API for external integrations | Integration requests from other systems | Extensibility |
| HR Partner features | HR expresses interest in direct access | Adoption |
| DSAR automated export | Volume of data subject access requests | Compliance |
| Mobile-friendly responsive design | Users requesting mobile access | Accessibility |
| Workday API integration | Volume justifies integration investment | Automation |

### Risk Mitigation Strategy

**Technical Risks:**

| Risk | Severity | Mitigation |
|------|----------|------------|
| Workday XLSX format assumptions | Low | Sample file available (`docs/example-import-files/workday-example.xlsx`). Only 5 fields extracted. Column-name mapping configurable. |
| CV bundle PDF splitting | Medium | Parse TOC table + PDF link annotations for page boundaries. Sample bundle available. Original bundle retained as fallback. |
| PDF-to-candidate name matching | Low | Auto-match by normalized name. Manual assignment UI for edge cases. Low-frequency operation. |
| SPA framework choice | Low | React/Vue recommended. Final decision in architecture phase. |
| Solo developer bottleneck | Medium | AI-assisted development. MVP scoped lean. No hard deadline. |

**Market Risks:** Not applicable. Internal tool with known users and validated pain.

**Resource Risks:**

| Risk | Mitigation |
|------|------------|
| Solo developer capacity | MVP scoped to minimum. Growth features require documented triggers. |
| Scope creep | Explicit "not in MVP" list. Each growth feature requires a promotion trigger. |
| Stakeholder disengagement | Erik is a daily user of the painful workflow. Motivation is self-sustaining. |

### Reference Data for Development

Sample files in `docs/example-import-files/`:
- `workday-example.xlsx` - Workday candidate export format. 5 fields used: full name, email, phone, location, date applied. Column-name mapping should be configurable.
- `JobReq-bundle-ANON.pdf` - Workday CV bundle. Real bundle has 3-column TOC table (Candidate Name, Candidate ID, Attachments). Each TOC entry links to the candidate's section containing CV + letter as single combined entry.

## Functional Requirements

*62 FRs across 8 capability areas. This is the capability contract for all downstream work: UX designers design what's listed here, architects support what's listed here, epics implement what's listed here.*

### Authentication & Access

- **FR1:** Users can sign in using their organizational Microsoft Entra ID account (SSO)
- **FR2:** Unauthenticated users are redirected to the SSO login flow
- **FR3:** Users can sign out of the application

### Recruitment Lifecycle

- **FR4:** Users can create a new recruitment with a title, description, and associated job requisition reference
- **FR5:** Users can configure the workflow steps for a recruitment (freeform text names, set sequence). A default template provides standard steps (Screening, Technical Test, Technical Interview, Leader Interview, Personality Test, Offer/Contract, Negotiation) which users can rename, add to, or remove freely.
- **FR6:** Users can view a list of all recruitments with their current status
- **FR7:** Users can close a recruitment, which locks it from further edits and starts a GDPR retention timer
- **FR8:** Users can view closed recruitments in read-only mode during the retention period
- **FR9:** The system anonymizes recruitment data after the GDPR retention period expires. Anonymization preserves aggregate metrics (candidate counts per step, conversion ratios, time-in-step averages, recruitment duration) while stripping all PII (names, emails, phones, documents, free-text notes, all direct identifiers).
- **FR10:** Users see contextual guidance when no recruitments exist, directing them to create their first recruitment
- **FR11:** Users can edit a recruitment's title and description while it is active
- **FR12:** Users can add and remove workflow steps on an active recruitment. Steps with recorded outcomes cannot be modified or removed. Steps with no recorded outcomes can be freely removed.
- **FR13:** Users can navigate between recruitments they have access to from any screen

### Team Management

- **FR56:** Users can invite authenticated users to a recruitment by searching the organizational directory (Entra ID)
- **FR57:** Users can view the list of members who have access to a recruitment
- **FR58:** Users can remove a member from a recruitment, except the recruitment creator who cannot be removed
- **FR59:** The user who creates a recruitment is automatically added as a permanent member (cannot be removed)
- **FR60:** Users can only view and access recruitments where they are a member
- **FR61:** The system records member additions and removals in the audit trail

### Candidate Import

- **FR14:** Users can import candidates by uploading a Workday XLSX file and optionally a CV bundle PDF in the same import session. Either file can also be uploaded independently.
- **FR15:** The system extracts five fields from the XLSX: full name, email, phone, location, and date applied
- **FR16:** The system matches imported candidates to existing records by email (primary), with name+phone as a low-confidence fallback
- **FR17:** The system flags low-confidence matches for manual review
- **FR18:** Users can view an import summary showing created, updated, and errored records with row-level detail
- **FR19:** The system tracks import sessions (who uploaded, when, source filename, summary counts)
- **FR20:** Re-importing the same file produces the same result without creating duplicates or corrupting data (idempotent)
- **FR21:** The import never overwrites app-side data (workflow states, outcomes, reason codes, notes)
- **FR22:** The import never auto-deletes candidates missing from a re-import
- **FR23:** Users can manually create a candidate record within a recruitment by entering name, email, phone, location, and date applied
- **FR24:** Users can manually remove a candidate from a recruitment
- **FR25:** The system validates uploaded XLSX files before processing and reports clear errors when the file format is invalid or required columns are missing
- **FR26:** The system reports count discrepancies between imported candidates and split CVs as informational notices, not errors
- ~~**FR27:** Merged into FR55~~

### CV Document Management

- **FR28:** Users can upload a Workday CV bundle PDF for a recruitment
- **FR29:** The system produces individual per-candidate PDF documents from an uploaded Workday CV bundle
- **FR30:** The system extracts and stores the Workday Candidate ID from the bundle TOC as reference metadata
- **FR31:** The system auto-matches split PDFs to imported candidates by normalized name
- **FR32:** Users can manually assign unmatched PDFs to candidate records through the import summary
- **FR33:** Users can manually upload a PDF document for an individual candidate, independent of the bundle import. MVP accepts PDF format only; Word document upload deferred to Growth phase (with Word-to-PDF conversion).
- **FR34:** Users can view a candidate's individual PDF (CV + letter combined) within the application
- **FR35:** Users can download a candidate's PDF document

### Workflow & Outcome Tracking

- **FR36:** Users can view which workflow step each candidate is currently at
- **FR37:** Users can record an outcome for a candidate at their current step (Pass, Fail, Hold) with an optional freeform text reason
- **FR38:** Users can advance a candidate to the next workflow step after recording a passing outcome
- **FR39:** Users can view the outcome history for a candidate across all completed steps
- **FR40:** The system enforces the configured workflow step sequence (candidates progress through steps in order)

### Batch Screening

- **FR41:** Users can view candidates and their CV documents side-by-side in a split-panel layout
- **FR42:** Users can navigate between candidates in the candidate list while the CV viewer and outcome form update accordingly
- **FR43:** Users can record an outcome and move to the next candidate in a continuous flow
- **FR44:** Users can perform the entire screening workflow using keyboard navigation alone
- **FR45:** Users can search candidates within a recruitment by name or email
- **FR46:** Users can filter candidates within a recruitment by current step and outcome status

### Recruitment Overview & Monitoring

- **FR47:** Users can view a recruitment overview showing candidate counts per workflow step
- **FR48:** Users can see visual indicators for steps that have candidates waiting beyond a configurable global threshold (default: 5 calendar days). Threshold is set via deployment configuration.
- **FR49:** Users can see a summary of pending actions across the recruitment. Pending actions are defined as candidates at each step with no outcome recorded yet.
- **FR50:** The recruitment overview loads independently from the detailed candidate list
- **FR51:** Users can view a candidate's complete profile including imported data, documents, and outcome history across all steps

### Audit & Compliance

- **FR52:** The system records an audit entry for every state change (outcome recorded, candidate imported, document uploaded, recruitment created/closed)
- **FR53:** Each audit entry captures who performed the action, when, and what changed
- **FR54:** Users can view the audit trail for a recruitment
- **FR55:** The system provides contextual Workday export instructions within the import flow (what to select, which exports to run, the "always export all candidates" rule)

### Workflow Defaults & Configuration

- **FR62:** New candidates (imported or manually created) are placed at the first workflow step with outcome status "Not Started"
- **FR63:** The GDPR retention period is configurable via deployment settings (environment variable / app configuration). Default: 12 months.

## Non-Functional Requirements

*47 NFRs across 6 categories. Only categories relevant to this product are included. The Deployment & Infrastructure category was added post-initial PRD to close a gap identified during implementation. The Testing category was added per [ADR-001](architecture/adrs/ADR-001-test-pyramid-e2e-decomposition.md) after the first live integration test revealed gaps in the test pyramid.*

### Performance

- **NFR1:** Page-to-page SPA navigation completes in under 300ms (client-side routing)
- **NFR2:** Recruitment overview endpoint returns pre-aggregated data in under 500ms
- **NFR3:** Candidate list endpoint returns paginated results (up to 50 per page) in under 1 second
- **NFR4:** Individual candidate PDF loads in the viewer within 2 seconds via SAS token streaming
- **NFR5:** Step outcome save provides visual confirmation within 500ms
- **NFR6:** Workday XLSX import (up to 150 rows) completes server-side within 10 seconds. If exceeding threshold, returns 202 Accepted with polling-based progress.
- **NFR7:** CV bundle PDF splitting (up to 150 candidates, up to 100 MB) completes within 60 seconds. Runs asynchronously with progress reported. Original bundle retained as fallback if splitting partially fails.
- **NFR8:** Initial application load (after SSO redirect) completes within 3 seconds on corporate network
- **NFR9:** The application supports up to 15 concurrent authenticated users without degradation
- **NFR10:** Import summary with row-level detail for up to 150 candidates renders within 2 seconds

### Security

- **NFR11:** All authentication via Microsoft Entra ID SSO. No user credentials stored.
- **NFR12:** All data in transit encrypted via TLS 1.2+
- **NFR13:** All data at rest encrypted (database encryption and blob storage encryption)
- **NFR14:** Candidate PII accessible only to authenticated users
- **NFR15:** PDF documents accessible only through short-lived SAS tokens (maximum 15-minute validity)
- **NFR16:** Application includes `noindex` meta tag and `X-Robots-Tag: noindex` response header
- **NFR17:** Application not exposed to public DNS without authentication
- **NFR18:** GDPR retention timer triggers automatic data deletion after configured period
- **NFR19:** All state changes recorded in immutable audit trail (who, what, when)
- **NFR20:** Uploaded files validated for type and size. Maximum XLSX: 10 MB. Maximum PDF bundle: 100 MB.
- **NFR21:** Data recovery supported to any point within last 7 days (platform point-in-time restore + blob soft delete)

### Accessibility

- **NFR22:** Application meets WCAG 2.1 Level AA for all core user flows
- **NFR23:** All interactive elements reachable and operable via keyboard alone
- **NFR24:** Batch screening workflow fully operable via keyboard without mouse dependency
- **NFR25:** All form inputs have associated labels. All images/icons have appropriate alt text or ARIA labels.
- **NFR26:** Color contrast ratios meet WCAG AA minimums (4.5:1 normal text, 3:1 large text)
- **NFR27:** Status indicators use shape/icon in addition to color (no color-only information)
- **NFR28:** Dynamic content updates announced to assistive technologies via ARIA live regions
- **NFR29:** Keyboard focus managed predictably during sequential workflows (focus moves to next logical interaction point after actions)

### Integration

- **NFR30:** Workday XLSX import supports configurable column-name mapping for locale/format variations
- **NFR31:** Workday CV bundle PDF parsing validated against known format. Fails gracefully with clear error messages identifying which candidates could not be extracted.
- **NFR32:** Document storage separated from application database. No documents in the database.
- **NFR33:** Structured data stored in a relational database with ACID compliance
- **NFR34:** API follows RESTful conventions with consistent error response format and appropriate HTTP status codes

### Deployment & Infrastructure

- **NFR35:** Automated CI/CD pipeline builds, tests, and deploys on PR merge to main. Full pipeline completes within 10 minutes. PRs require passing build and test suite as merge gate.
- **NFR36:** A staging environment mirrors production configuration for end-to-end verification before production deployment.
- **NFR37:** All Azure infrastructure defined declaratively using Bicep templates. Full environment reproducible from source control within 30 minutes.
- **NFR38:** API deployed to Azure App Service using azd deployment pipeline. Docker Compose retained for local development environment.
- **NFR39:** EF Core database migrations execute automatically during deployment. Rollback strategy is deploy-previous-version, not reverse-migration.
- **NFR40:** Health check endpoints: `/health` (liveness — process alive) and `/ready` (readiness — database connected, dependencies available) enable Azure load balancer integration and automatic container restart on failure.

### Testing

- **NFR41:** Query and command handlers using EF Core LINQ-to-SQL features (`.Include()`, `.Where()` with navigation properties, `.Select()` projections, `.GroupBy()`) must have functional tests running against real SQL Server via Testcontainers. Unit tests with mocked `IApplicationDbContext` are insufficient for these handlers.
- **NFR42:** Security isolation verified by integration tests against real SQL Server for all 8 mandatory scenarios defined in `testing-standards.md`. Tests must use Testcontainers, not `UseInMemoryDatabase`.
- **NFR43:** E2E scenarios documented in `docs/e2e-scenarios.md` registry, traceable to lower-level tests via "Covered By" column. Automated browser-based E2E tests added only when no combination of lower tests covers the risk.
- **NFR44:** Frontend-backend DTO contract alignment verified by structural contract tests in `Application.UnitTests/Contracts/`. Required for every endpoint with a corresponding MSW handler.
- **NFR45:** CI runs all test layers (unit, contract, integration, functional) on every PR. Total CI pipeline completes within 10 minutes (shared with NFR35).
- **NFR46:** Post-deployment smoke tests in the CD pipeline verify liveness (`/health` → 200), readiness (`/ready` → 200), and authentication enforcement (`/api/recruitments` unauthenticated → 401) after each deployment.
- **NFR47:** No `UseInMemoryDatabase` in any test project. All database-dependent tests use Testcontainers with real SQL Server to ensure EF Core global query filters, FK constraints, and SQL-specific behavior are exercised.
