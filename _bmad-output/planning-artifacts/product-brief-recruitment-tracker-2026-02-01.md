---
stepsCompleted: [1, 2, 3, 4, 5, 6]
status: complete
inputDocuments:
  - docs/chatgpt-research-intro.md
date: 2026-02-01
author: MrGyllen
---

# Product Brief: recruitment-tracker

## Executive Summary

**"Workday tracks the hiring. This tool runs the hiring."**

recruitment-tracker is a lightweight, enterprise-grade web application that fills the critical gap between Workday's recruitment management and the day-to-day execution of hiring. Workday handles job postings and applicant intake, but once the real work begins - screening CVs, coordinating interviews, recording decisions - recruiting leaders are left stitching together emails, Teams chats, and spreadsheets. The recruiting leader becomes everyone's secretary: manually distributing documents, relaying status updates, and tracking outcomes in personal files.

This app replaces that chaos with a shared status board where every participant - recruiting leaders, subject matter experts, and collaborators - can self-serve. SMEs log in, see their assigned candidates, read CVs in-app, and record their screening outcomes without a single Teams ping. The recruiting leader opens the app after a meeting and sees that work has already moved forward. That's the unlock: **the team collaborates without the leader as the bottleneck**.

Built on Azure with Microsoft Entra ID for seamless SSO, the application delivers enterprise-grade security and GDPR-compliant PII handling through appropriately engineered architecture - not through unnecessary complexity. It is intentionally not a Workday replacement, not an HR system, and not a project management tool. It is a focused execution companion for the people who actually run the hiring.

---

## Core Vision

### Problem Statement

Recruiting leaders run multiple concurrent hiring processes involving cross-functional teams of SMEs, colleagues, and HR partners. Workday handles the front door - posting ads and collecting applications - but once execution begins, there is no shared tool. Workday's web interface requires navigating through multiple unfamiliar menus, offers no collaboration features, and provides no support for the operational work of screening, interviewing, evaluating, and deciding.

The result: the recruiting leader becomes a manual information hub. They distribute CVs through Teams, relay status via email, track outcomes in personal spreadsheets, and coordinate schedules across participants. SMEs and collaborators cannot self-serve - they wait for the leader to push information to them. The leader's bandwidth directly limits how fast and effectively the entire team can work.

### Problem Impact

- **The recruiting leader is the bottleneck** - their calendar and availability gate everyone else's ability to contribute, especially during parallel recruitments
- **SMEs and collaborators cannot self-serve** - they depend on the leader to share documents, relay status, and assign work, creating idle time and delays
- **No single source of truth exists** during execution - status lives in scattered emails, Teams threads, and personal files that nobody else can access
- **Compliance risk is real** - candidate PII flows through uncontrolled channels (email, Teams, local files) with no access control, audit trail, or retention management
- **Scaling pressure is increasing** - the organization is growing, replacing consultants with employees, and running more parallel recruitments than the manual approach can handle

### Why Existing Solutions Fall Short

**Workday** is effective as a system of record for postings and applicant intake but fails at execution support. Its web interface is unfamiliar to daily users, navigation to candidate details requires multiple steps, and it offers no collaboration features for the recruiting team. It tracks the administrative recruitment lifecycle, not the operational one.

**Email + Teams + Excel** is the current workaround. Recruitments get completed, but at the cost of the leader's time, fragmented information, zero auditability, and uncontrolled PII handling. There is no central status view, no structured decision tracking, and no compliant data management.

**No existing tool** bridges the gap between an enterprise HR system of record and the tactical, collaborative execution needs of a recruiting leader and their team.

### Proposed Solution

Picture this: a recruiting leader finishes a meeting, opens recruitment-tracker, and sees that two SMEs have already completed their CV screenings - with notes, outcomes, and reason codes - without a single Teams message. The HR partner checks in and sees the current status across all candidates without asking for an update. The leader didn't relay anything. The tool did.

That's the core of recruitment-tracker: **a shared status board that lets everyone involved in a recruitment self-serve**.

The application provides:

- **Centralized recruitment workspaces** - one place where all participants see the same candidates, statuses, documents, and decisions
- **Customizable workflow steps** - sensible defaults for common recruitment processes (screening, technical test, interviews, offer) with the ability to rename, reorder, add, or remove steps. Reuse a previous recruitment as a template
- **Per-candidate step tracking** - states (not started / in progress / done), outcomes (approved / declined), optional reason codes, and notes
- **Candidate-level status** - not started, active, rejected, withdrawn, on hold - to track overall progress across the full process
- **Secure in-app document viewing** - CVs and letters viewable as PDF directly in the browser. Word documents converted to PDF on upload for consistent viewing; originals stored for compliance
- **Manual Workday import** - upload XLSX exports with smart matching (email-based, with name+phone fallback). Safe upsert logic that never overwrites app-side decisions and never auto-deletes candidates, even when Workday's paging produces incomplete exports
- **Optional reviewer assignment** - distribute screening and review work among SMEs, giving each a personal "My Assignments" queue
- **Role-based access** - Recruiting Leader (admin), SME/Collaborator, Viewer, and a future-ready HR role, enforced per recruitment with deny-by-default security
- **GDPR-compliant PII management** - configurable retention periods, anonymization of closed recruitments for historical metrics, audit trails for sensitive actions, encryption at rest and in transit
- **Seamless SSO** - Microsoft Entra ID authentication using existing company accounts, no separate credentials

### Key Differentiators

1. **Execution-focused complement to Workday** - Purpose-built for what happens *after* the job posting. Not competing with Workday's system-of-record role. The product filter is simple: does it help *run* the hiring? If not, it's out of scope.
2. **Self-service collaboration that removes the leader bottleneck** - The defining moment is when SMEs work independently - reviewing candidates, recording outcomes, progressing steps - without waiting on the leader. The app replaces the leader-as-secretary pattern with a shared status board everyone can see and act on.
3. **Enterprise security with consumer-grade simplicity** - GDPR compliance, audit trails, encrypted PII, and role-based access control - delivered through appropriately engineered architecture (clean layered monolith on Azure) that stays fast, simple, and easy to operate. If a user needs a manual, the UX has failed.

---

## Target Users

### User Priority Hierarchy

| Priority | User | Rationale |
|----------|------|-----------|
| **1 - Primary** | Recruiting Leader (Erik) | THE user. If Erik doesn't adopt it, nobody else sees it. Every trade-off decision favors Erik first. |
| **2 - Primary** | SME / Collaborator (Lina) | The force multiplier. Often "the most important person in the room" during technical assessment. When Lina uses it, Erik's bottleneck dissolves. |
| **3 - Secondary** | Viewer (Anders) | Read-only visibility. Lowest effort to support, genuine value for stakeholders following along. |
| **4 - Future** | HR Partner (Sara) | RBAC role defined, no features built. Door open, room not furnished. Build something so useful that Sara asks to be included. |

---

### Primary Users

#### Recruiting Leader - "Erik"

**Design Principle: "Every screen answers: what needs my attention?"**

**Profile:** Erik is a line manager responsible for a team of 15-20 engineers. He runs 2-3 recruitment processes per year on top of his regular leadership duties - team development, project oversight, stakeholder management, and operational decisions. Recruitment is critical but competes with everything else on his calendar.

**Day-to-day reality:** Erik's involvement in recruitment shifts by phase. Early on, he's defining the role and drafting the ad with an SME and HR. Once applications arrive, he's coordinating screening across his team, relaying CVs through Teams, and manually tracking who's reviewed what. During interview periods, he's collecting outcomes, deciding whether to progress or decline candidates, and continuously syncing with HR. In the closing phase, it's Erik and HR driving the offer and contract negotiation. Throughout it all, he's the single point through which all information flows.

**Variable responsibility:** In some recruitments, Erik handles most logistics - sending tech tests, booking interviews, coordinating schedules. In others, especially technical recruitments, he delegates these to SMEs who are better positioned to drive the assessment process. The app must support both patterns without forcing one.

**Core frustration:** Erik is the bottleneck and he knows it. When he's pulled into other priorities, the entire recruitment stalls - not because people aren't willing to help, but because they're waiting on him to push information, share documents, or update status. He's become everyone's secretary instead of the decision-maker he should be.

**Success moment:** Erik opens the app after a day of back-to-back meetings and sees that three SMEs have completed their screening, one has flagged a strong candidate with notes, and HR can see the current state without asking. Nobody waited on him. The recruitment moved forward while he was busy.

**Testable success:** Erik sees screening progress from 3 SMEs without having sent any messages since creating the recruitment and assigning candidates.

**Tech profile:** Comfortable with standard business tools. Doesn't want to learn a complex system. If it takes more than a few clicks to do something, he'll fall back to Teams.

---

#### SME / Collaborator - "Lina"

**Design Principle: "Complete your task in one focused session."**

**Profile:** Lina is a senior software engineer with 8 years of experience. She's one of Erik's go-to people for technical recruitment because she has deep domain expertise and good judgment about candidates. Recruitment is not her primary job - she has sprint commitments, code reviews, and her own deliverables - but when she's involved in the assessment, her opinion carries the most weight. She is often "the most important person in the room" during technical evaluation.

**Day-to-day reality:** Lina is mostly inactive in the recruitment process until work is pushed to her. She gets a batch of CVs via Teams, screens them when she can fit it in, and reports back in the chat. During the technical phase, she may take on more responsibility: sending tech tests to candidates, booking technical interviews, and conducting the interviews herself. After interviews, she shares her assessment verbally or in a Teams message. Her decisions on technical candidates weigh heavily in the progress/decline decision.

**Variable responsibility:** Lina's involvement ranges from "review these 4 CVs and give me your thoughts" to "own the entire technical assessment phase - send tests, book interviews, conduct them, and record outcomes." The app must support both levels without requiring role changes or permissions adjustments.

**Core frustration:** The process is disjointed. CVs arrive in Teams messages to download, assessments go back as unstructured chat messages, and there's no clear queue or record of what she's done. If she forgets to respond, the process stalls until Erik follows up.

**Success moment:** Lina logs in, sees her 4 assigned candidates with CVs ready to view in-app, reads them, and marks her screening outcomes with a quick note. Later, after a technical interview, she opens the candidate's detail, records her interview assessment with outcome and notes directly in the step. Erik never had to ping her.

**Testable success:** Lina completes screening for 4 candidates in under 10 minutes without leaving the app. After a technical interview, she records her assessment in under 2 minutes.

**Tech profile:** Very tech-savvy (she's an engineer), but has zero patience for clunky tools. The app needs to be faster and simpler than the Teams chat it replaces, or she won't use it.

---

### Secondary Users

#### Viewer - "Anders"

**Design Principle: "Glance and know."**

**Profile:** Anders is a team lead in the department where the new hire will work. He's not actively screening CVs or conducting interviews, but he has a keen interest in who's being recruited for his team. He may occasionally weigh in on candidate assessments informally. Could also be another leader or a manager's manager who wants to stay informed.

**Needs:** Anders wants to see the current status of the recruitment - how many candidates are active, where they are in the process, and whether things are moving. He doesn't need to take any action in the app. He just wants visibility without having to ask Erik for an update.

**Success moment:** Anders checks the recruitment once a week, sees the pipeline at a glance - 12 candidates, 4 past screening, 2 in interviews - and knows exactly where things stand without asking anyone.

**Testable success:** Anders opens a recruitment and understands the full pipeline status within 10 seconds without clicking into any candidate detail.

#### Viewer Journey (Anders)

| Phase | Action | Experience |
|-------|--------|------------|
| **Invitation** | Receives link from Erik when recruitment starts | One click to access |
| **First visit** | SSO login, sees recruitment overview with candidate pipeline | "I can see everything without bothering Erik" |
| **Ongoing** | Checks in weekly, scans status at a glance | Informed in seconds, no coordination needed |

---

#### HR Partner - "Sara" (future - RBAC defined, no features built)

**Profile:** Sara is an HR specialist who supports Erik's organization. She owns the Workday recruitment case, handles the administrative lifecycle (posting, initial screening for basic criteria, contacting candidates, setting up personality tests), and keeps Erik on track. Workday is her primary tool and she's comfortable in it.

**Current relationship:** Sara and Erik sync regularly - sometimes she initiates when the recruitment feels slow, sometimes Erik reaches out with updates. Their communication happens through ad-hoc Teams calls and messages. Sara doesn't have structured visibility into what Erik's team is actually doing during the execution phase.

**Potential value:** If recruitment-tracker proves useful, Sara would benefit from real-time visibility into execution status without scheduling sync calls. She could see which candidates have been screened, who's progressing to interviews, and where the pipeline stands.

**Strategy:** The HR role is defined in the RBAC model so the architecture doesn't block future adoption. No HR-specific features are built for MVP. The goal is to build something so useful for Erik's team that Sara *asks* to be included, rather than being required to adopt another tool.

---

### User Journeys

#### Recruiting Leader Journey (Erik)

| Phase | Action | Experience |
|-------|--------|------------|
| **Discovery** | Erik commissions or hears about the tool | "Finally, something for the actual recruitment work" |
| **Onboarding** | SSO login. Creates first recruitment, customizes workflow steps from template | Under 5 minutes to be productive. No training needed |
| **First Import** | Uploads Workday XLSX export. Candidates populated with data and CVs | "Everything's in one place now" |
| **Team Setup** | Invites SMEs and optionally a viewer. Assigns candidates to reviewers | Replaces the "hey team, here are the CVs" Teams message |
| **Core Usage** | Checks dashboard between meetings. Sees step progress, outcomes, notes | The bottleneck dissolves - the team works independently |
| **Success Moment** | Opens app after a busy day, screening is complete without a single ping | Testable: progress from 3 SMEs, zero messages sent |
| **Ongoing** | Uses for every recruitment. Reuses previous ones as templates | Becomes the natural way to run hiring |

#### SME / Collaborator Journey (Lina)

| Phase | Action | Experience |
|-------|--------|------------|
| **Invitation** | Gets link to the recruitment from Erik | One click to access |
| **Onboarding** | SSO login. Sees "My Assignments" - candidates assigned for review | Immediately knows what's expected |
| **Screening** | Opens candidate, reads CV in-app, marks outcome with note | Faster than downloading from Teams and reporting back |
| **Interview Assessment** | After conducting technical interview, opens candidate detail, records outcome and assessment notes in the relevant step | Structured feedback replaces ad-hoc Teams messages |
| **Driving Logistics** (variable) | When responsible: sends tech test info, tracks responses, books interviews through the step tracking | App supports both "reviewer only" and "process driver" patterns |
| **Success Moment** | Completes all screening in one focused session, records interview assessment in 2 minutes | Testable: 4 candidates screened in under 10 minutes |

---

## Success Metrics

### User Success Metrics

These measure whether the app is solving the core problem for the people using it.

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Recruiting Leader adoption** | 100% of active recruitments managed in the app (not Teams/email) | Count of recruitments created in app vs. known active recruitments |
| **SME self-service rate** | SMEs record screening outcomes without being prompted by the leader | Ratio of outcomes recorded in-app vs. outcomes requested via Teams |
| **Status relay elimination** | Near-zero Teams/email messages for status updates within the recruitment team | Leader self-reports reduction in manual update messages (from multiple/week to near-zero) |
| **SME time-to-action** | SMEs begin screening within 24 hours of assignment, complete within 48 hours | Time between candidate assignment and outcome recording in-app |
| **"My Assignments" engagement** | SMEs check their queue without being prompted | Login frequency of SME users relative to new assignments |

### Business Objectives

These connect the tool to broader organizational value.

| Objective | Success Indicator | Timeframe |
|-----------|-------------------|-----------|
| **Reduce recruiting leader admin overhead** | Leader spends time on decisions and evaluation, not information relay. Self-reported reduction in coordination effort. | Within first completed recruitment |
| **Speed up the recruitment pipeline** | Time from "CVs available" to "all candidates screened" decreases because SMEs self-serve rather than wait on the leader | Compare first app-managed recruitment to previous manual ones |
| **Centralize execution data** | All candidate data, documents, outcomes, and notes in one place instead of scattered across Teams, email, and personal files | From first recruitment onward |
| **Achieve GDPR compliance for execution data** | Candidate PII managed with access control, audit trails, and retention policies instead of flowing through uncontrolled channels | From launch |
| **Create pull for HR adoption** | HR partner (Sara) expresses interest in using the tool based on seeing its value for the recruiting team | Within 6-12 months (organic, not forced) |

### Key Performance Indicators

Concrete, measurable indicators tied to the product filter: "Does it help run the hiring?"

**Adoption KPIs (first 3 months):**
- 2+ recruitments actively managed in the app
- 3-6 users actively engaged per recruitment
- All candidate documents stored in-app (zero CV distribution via Teams/email)

**Efficiency KPIs (per recruitment):**
- Leader sends <5 manual status messages for the entire recruitment lifecycle (down from multiple per week)
- Screening completion within 48 hours of candidate assignment
- All step outcomes recorded in-app with notes (zero verbal-only assessments)

**Compliance KPIs (ongoing):**
- 100% of candidate documents accessed in-app (tracked via audit trail), near-zero local downloads
- Closed recruitments anonymized within configurable retention period
- Audit trail producible on demand for any recruitment

**The "Would I Go Back?" KPI:**
- After completing one full recruitment in the app, the recruiting leader and SMEs choose to use it for every subsequent recruitment without being asked. If anyone voluntarily goes back to Teams + spreadsheets, that's a failure signal requiring immediate investigation.

---

## MVP Scope

### Core Features (MVP)

**1. Recruitment Management**
- Create a new recruitment with title and configurable workflow steps
- Default workflow template (Screening, Technical Test, Technical Interview, Leader Interview, Personality Test, Offer/Contract, Negotiation)
- Customize steps: rename, reorder, add, remove, enable/disable
- Reuse a previous recruitment as template for a new one
- Close a recruitment with final status

**2. Candidate Management**
- Import candidates from Workday XLSX export (structured data: name, email, phone, country, date applied)
- Smart upsert on import: match by email (primary), name+phone fallback with low-confidence flag
- Never overwrite app-side workflow states, outcomes, notes, or comments on re-import
- Never auto-delete candidates (manual removal only)
- Import session tracking: who uploaded, when, summary counts (created/updated/errors), row-level error reporting
- Candidate-level status: not started, active, rejected, withdrawn, on hold
- Manual candidate addition for edge cases (minimal form - escape hatch, not primary flow)

**3. Document Handling**
- Individual CV/letter upload per candidate (PDF, Word)
- Bulk upload of separate files with filename-to-candidate matching
- In-app PDF viewer for PDF files (browser-native rendering, no download required)
- Word files: upload and store, view via download link (download explicitly logged in audit trail)
- All documents stored securely in Azure Blob Storage with access policies
- Document access restricted to recruitment members only

**4. Workflow & Step Tracking**
- Per-candidate step tracking with states: not started, in progress, done
- Step outcome: pending, approved, declined
- Optional reason codes per step (configurable list)
- Notes/comments field per step (free text, no mandatory fields)
- General candidate notes thread (separate from step-specific notes)

**5. Team Collaboration**
- Invite members to a recruitment with role: Recruiting Leader, SME/Collaborator, Viewer
- Reviewer assignment: assign specific candidates to specific SMEs
- "My Assignments" queue: SME sees only their assigned candidates with pending actions
- All roles see candidate data, steps, and documents within their recruitment (role-based action permissions)

**6. Home Screen**
- List of active recruitments the user is a member of
- Per-recruitment summary: candidate counts by status, pending actions
- Quick open to recruitment detail

**7. Authentication & Authorization**
- Microsoft Entra ID (OIDC) SSO for production
- Microsoft accounts or simulated directory for development (same OIDC/OAuth patterns)
- Deny-by-default RBAC enforced at API endpoints
- Per-recruitment role enforcement: users only see recruitments they created or were invited to
- Roles: Recruiting Leader (admin), SME/Collaborator, Viewer, HR (defined but no specific features)

**8. GDPR & Compliance**
- All candidate data treated as PII with access control
- Encryption at rest and in transit (Azure platform defaults, explicitly verified)
- Configurable retention period per recruitment (default 12 months after closure)
- Anonymization on retention expiry: strip direct identifiers and documents, preserve anonymized metrics (counts, funnel, timestamps)
- Audit trail: append-only event log from day one (timestamp, userId, actionType, entityType, entityId, context as JSON with no PII)
- Audited actions: candidate CRUD, document view/download, import sessions, status changes, membership changes
- No PII in application logs; structured logging with correlation IDs
- Basic telemetry via Application Insights

**9. Technical Foundation**
- ASP.NET Core Web API backend
- SPA frontend (no direct database access)
- Azure SQL for structured data
- Azure Blob Storage for documents (with access policies)
- Modular layered architecture with clear boundaries (API, application/domain, infrastructure)
- Schema designed for anonymization from day one
- CI pipeline: tests, lint/format, dependency checks on PRs
- Pragmatic TDD for domain and application logic (see `docs/testing-pragmatic-tdd.md`)

---

### Early Development Spikes

These must be resolved before or during early implementation:

| Spike | Purpose | Dependency |
|-------|---------|------------|
| **Workday XLSX format** | Parse a real export file, validate column mapping, confirm row offsets | Need sample file from MrGyllen |
| **Word-to-PDF conversion** | Evaluate library options (Aspose, LibreOffice headless, Azure Functions) for feasibility and deployment complexity | Determines Fast Follow timeline |
| **Combined Workday PDF** | Examine export format, assess index page parsing reliability | Need sample combined PDF from MrGyllen |
| **Anonymization schema** | Define exactly which fields get stripped, hashed, or preserved during retention expiry | Must be resolved before entity schema is finalized |

---

### Fast Follow (post-MVP, pre-V2)

Ship immediately after MVP if spikes validate the approach:

- **Word-to-PDF conversion on upload** - consistent in-app viewing for all document types; store originals for compliance
- **Combined Workday PDF upload** - auto-split and candidate matching for bulk CV import
- **"Missing from import" flag** - detect candidates absent from re-imports once import patterns are understood

---

### Out of Scope for MVP

| Feature | Rationale |
|---------|-----------|
| **HR-specific features** | HR role defined in RBAC but no features built. Wait for organic pull. |
| **Teams integration / notifications** | Use Teams separately. No in-app notification system. |
| **Workday API integration** | Manual export/upload only. No bidirectional sync. |
| **Status pushback to Workday** | One-way flow. Leader verbally updates HR who updates Workday. |
| **DSAR export functionality** | Handle manually if needed. Build automated export if volume justifies it. |
| **Advanced reporting / dashboards** | Simple home screen with counts. No charts, analytics, or trends. |
| **Multi-language UI** | English only. |
| **Mobile-optimized UI** | Desktop-first. Responsive enough not to break on tablet. |
| **Email notifications** | No email sending from the app. |
| **DORA compliance measures** | Not a critical system. GDPR is the compliance driver. |

---

### MVP Success Criteria

The MVP is validated when:

1. **One complete recruitment** has been run through the app from import to close with a real team
2. **Recruiting leader** reports meaningful reduction in manual status updates (target: <5 for the entire recruitment lifecycle)
3. **SMEs** recorded screening outcomes and interview assessments in-app without falling back to Teams
4. **All candidate documents** were accessed in-app without email/Teams distribution
5. **Audit trail** can be produced on demand for the completed recruitment
6. **No user voluntarily returns** to the old Teams + spreadsheet approach for the next recruitment

---

### Future Vision

**Near-term (post-MVP fast follows resolved):**
- Word-to-PDF conversion for consistent in-app viewing
- Combined PDF upload with auto-split
- Missing from import detection
- HR Partner features once adoption pull exists

**Medium-term (6-12 months):**
- Email/in-app notifications for key events
- DSAR automated export
- Adoption by other organizational units
- Workflow templates library
- Basic reporting: time-to-hire, conversion rates, recruiter workload
- Mobile-friendly responsive design

**Long-term (if widely adopted):**
- Workday API integration (bidirectional sync)
- Calendar integration for interview scheduling
- Advanced analytics and funnel optimization
- Multi-tenant architecture for broader company deployment

**Design principle for future scope:** Each addition must pass the product filter - "Does it help run the hiring?" - and must not compromise the simplicity that made the MVP successful.
