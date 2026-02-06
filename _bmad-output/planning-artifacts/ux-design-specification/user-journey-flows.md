# User Journey Flows

## J0: Erik's First Five Minutes (Onboarding)

The zero-to-live journey. Erik arrives at an empty app and needs to leave with a functioning recruitment.

```mermaid
flowchart TD
    A[Erik clicks app link] --> B{SSO authenticated?}
    B -->|No| C[Redirect to Entra ID login]
    C --> D[SSO completes]
    D --> B
    B -->|Yes| E[App loads - empty state]

    E --> F["Empty state shows:<br/>• Value proposition<br/>• 'Create your first recruitment' CTA"]
    F --> G[Erik clicks 'Create Recruitment']
    G --> H["Dialog: Title + Description<br/>Default 7-step workflow shown"]
    H --> I{Customize steps?}
    I -->|Yes| J[Rename/add/remove steps]
    J --> K[Save recruitment]
    I -->|No| K

    K --> L["Recruitment created<br/>Empty candidate list<br/>Import CTA prominent"]
    L --> M[Erik clicks 'Import Candidates']
    M --> N["Import wizard Step 1:<br/>Upload area accepts XLSX and/or PDF bundle<br/>Workday export instructions visible"]

    N --> O{What was uploaded?}
    O -->|XLSX only| P1["Validate XLSX format"]
    O -->|PDF bundle only| P2["Validate PDF format"]
    O -->|Both files| P3["Validate both files"]

    P1 -->|Invalid| ERR["Blocking error:<br/>'Expected .xlsx/.pdf format'<br/>Clear retry path"]
    P2 -->|Invalid| ERR
    P3 -->|Invalid| ERR
    ERR --> N

    P1 -->|Valid| Q1["Processing state:<br/>Progress indicator<br/>'Importing candidates...'"]
    P2 -->|Valid| Q2["Processing state:<br/>Progress indicator<br/>'Splitting PDF bundle...'<br/>(up to 60s for large bundles)"]
    P3 -->|Valid| Q3["Processing state:<br/>Progress indicator<br/>'Importing candidates and splitting PDFs...'"]

    Q1 --> R1["Import summary:<br/>'127 candidates created'<br/>No CV matching (XLSX only)"]
    Q2 --> R2["Import summary:<br/>'124 CVs extracted'<br/>Manual matching needed for all"]
    Q3 --> R3["Import summary:<br/>'127 candidates, 124 CVs matched'"]

    R1 --> CONFIRM
    R2 --> MATCH{Unmatched CVs?}
    R3 --> MATCH

    MATCH -->|Yes| S["Amber notice: 'N CVs need manual matching'<br/>Manual assignment UI"]
    S --> T[Erik assigns unmatched CVs]
    T --> LOWCONF
    MATCH -->|No| LOWCONF

    LOWCONF{Low-confidence matches?} -->|Yes| W["Flag shown: 'N matches by name+phone only'<br/>Erik reviews and confirms/rejects"]
    W --> CONFIRM[Erik confirms import]
    LOWCONF -->|No| CONFIRM

    CONFIRM --> Y["Recruitment live!<br/>Candidate list populated<br/>Overview shows initial counts<br/>Toast: 'N candidates imported'"]

    Y --> Z["Success: Link click → live recruitment<br/>Target: under 5 minutes"]
```

**Key design decisions:**
- Empty state is functional guidance, not a tutorial overlay
- Three independent upload paths: XLSX only, PDF only, or both together (per FR14)
- Explicit async processing state with progress indicator between upload and summary (per NFR6/NFR7 -- XLSX up to 10s, PDF split up to 60s)
- Blocking errors (invalid format) are prominent and stop the process
- Non-blocking issues (unmatched CVs, low-confidence matches) use amber tones and let Erik continue
- Workday export instructions embedded in the import wizard, not in separate docs

## J1: Erik's Daily Status Check

Erik opens the app between meetings to understand pipeline state without asking anyone.

```mermaid
flowchart TD
    A[Erik opens app] --> B[SSO auto-authenticates]
    B --> C{Multiple recruitments?}
    C -->|Yes| C1["Recruitment list/selector shown<br/>Erik picks recruitment"]
    C -->|One recruitment| C2[Auto-loads single recruitment]
    C1 --> D
    C2 --> D

    D["Single-page loads:<br/>Overview section (expanded/collapsed per localStorage)<br/>Candidate list below"]

    D --> E{Overview collapsed?}
    E -->|Collapsed| F["Summary bar visible:<br/>'130 candidates · 47 screened · 3 stale'"]
    F --> G{Needs detail?}
    G -->|Yes| H[Click bar to expand overview]
    G -->|No| I[Scroll to candidate list]
    E -->|Expanded| J["Full overview visible:<br/>KPI cards + pipeline breakdown"]
    H --> J

    J --> K{Stale indicator visible?}
    K -->|Yes| L["Stale badge on step:<br/>'5 candidates >7 days at Screening'<br/>Clock icon + amber"]
    L --> M[Erik clicks stale indicator]
    M --> N["Candidate list filters<br/>to stale candidates"]
    K -->|No| O{Want step detail?}

    O -->|Yes| P["Erik clicks step count<br/>in pipeline breakdown"]
    P --> Q["Candidate list filters<br/>to that step"]
    O -->|No| R["Status check complete<br/>Erik closes app or continues"]

    N --> S{Take action?}
    S -->|Follow up outside app| T["Erik contacts team member<br/>directly (Teams/in-person)<br/>MVP: no in-app notifications"]
    S -->|Record outcome| U["Erik clicks candidate<br/>→ Screening panel opens"]
    U --> V["Record outcome inline<br/>(same mechanics as J3)"]

    Q --> W{Review candidate?}
    W -->|Yes| U
    W -->|No| R

    T --> R
    V --> R
```

**Key design decisions:**
- Recruitment selection when user has multiple recruitments (per FR13)
- The overview remembers its collapse state -- Lina keeps it collapsed, Erik keeps it expanded
- Collapsed state still shows key numbers inline (not just a toggle button)
- Clicking pipeline step counts filters the candidate list below -- no page transition
- Stale candidate follow-up explicitly outside the app in MVP -- the app provides awareness, not action tooling for notifications

## J2: Erik's Import & Workflow Modification

Mid-recruitment disruption: re-import candidates and modify workflow steps.

```mermaid
flowchart TD
    A[Erik opens active recruitment] --> B{Action needed?}

    B -->|Re-import candidates| C[Erik clicks 'Import']
    C --> D["Import wizard Step 1:<br/>Upload new XLSX and/or PDF bundle"]
    D --> E{Files valid?}
    E -->|Invalid| F["Blocking error with clear message"]
    F --> D
    E -->|Valid| G0["Processing state:<br/>Progress indicator shown<br/>(async server-side processing)"]

    G0 --> G["Import summary:<br/>'3 new · 9 updated (profile only) · 0 errors'"]

    G --> H{Low-confidence matches?}
    H -->|Yes| I["Flagged matches shown<br/>Erik reviews each"]
    I --> J[Confirm or reject each match]
    H -->|No| J

    J --> K{Row-level issues?}
    K -->|Yes| L["Drill into summary<br/>for row-level detail"]
    L --> M[Erik resolves or acknowledges]
    K -->|No| M

    M --> N["Confirm import<br/>Existing outcomes/states untouched<br/>Import session logged to audit trail"]

    B -->|Modify workflow| O[Erik opens recruitment settings]
    O --> P{Add or remove step?}
    P -->|Add step| Q["Erik adds step name<br/>Positions in sequence"]
    Q --> R["New step appears for all candidates<br/>as 'Not Started'<br/>Existing progress unaffected"]
    P -->|Remove step| S{Step has outcomes?}
    S -->|Yes| T["Step locked: 'Cannot remove —<br/>outcomes recorded at this step'"]
    S -->|No| U1{Candidates at this step?}
    U1 -->|Yes| U2["Candidates moved to next step<br/>as 'Not Started'<br/>Step removed"]
    U1 -->|No| U3["Step removed cleanly"]

    T --> O
    R --> V[Recruitment updated]
    U2 --> V
    U3 --> V
    N --> V

    V --> VV["Overview reflects changes<br/>Pipeline counts updated"]
```

**Key design decisions:**
- Re-import is safe by default: never overwrites app-side data, never deletes candidates
- Explicit async processing state between upload and summary
- Import summary distinguishes new vs. updated with drill-down for details
- Step removal blocked if outcomes exist (FR12) -- protective constraint, not an error
- Step removal with candidates present: candidates move to next step as "Not Started"

## J3: Lina's Batch Screening Session

The make-or-break flow. This is where the app proves its value.

```mermaid
flowchart TD
    A[Lina opens app] --> B[SSO auto-authenticates]
    B --> B1{Multiple recruitments?}
    B1 -->|Yes| B2["Recruitment selector in header<br/>Lina picks recruitment"]
    B1 -->|One recruitment| B3[Auto-loads recruitment]
    B2 --> C
    B3 --> C

    C["Single-page loads<br/>Overview (collapsed if previous preference)<br/>Three-panel layout visible"]

    C --> D["Candidate list shows all candidates<br/>at current step filter<br/>Unscreened visually distinct (dot indicator)"]
    D --> E[Lina clicks first unscreened candidate]

    E --> F["Three panels populate:<br/>Left: candidate list (current highlighted)<br/>Center: CV renders inline (page 1 immediate)<br/>Right: outcome controls + reason field"]

    F --> G["System pre-fetches next 2-3 CVs<br/>via SAS-token URLs"]

    F --> I["Lina reads CV<br/>(scroll for additional pages)"]
    I --> J{Record outcome}

    J -->|Keyboard path| K["Press 1/2/3<br/>(Pass/Fail/Hold selected)"]
    K --> L{Add reason?}
    L -->|Yes| M["Tab → reason textarea<br/>Type reason text<br/>Tab → confirm button"]
    L -->|No| N[Press Enter to confirm]
    M --> N

    J -->|Mouse path| O["Click Pass/Fail/Hold button"]
    O --> L

    N --> P["Optimistic UI: outcome applied immediately"]
    P --> Q["Toast slides in (bottom-right):<br/>'✓ Pass recorded for [Name] · Undo'"]
    Q --> R["Auto-advance to next unscreened<br/>candidate below in list order"]

    R --> R1["Screened candidate exits filtered list<br/>AFTER auto-advance completes<br/>(no visual disruption during transition)"]

    R1 --> S{Undo clicked?}
    S -->|Yes within 3s| T["Outcome reversed<br/>No API call<br/>Candidate restored in filtered list<br/>Return to that candidate"]
    S -->|No / 3s passes| U["API call persists outcome to server<br/>Toast auto-dismisses<br/>Overview updates ~3-4s after recording<br/>(undo window + API roundtrip)"]

    T --> I
    U --> V{More unscreened?}
    V -->|Yes| W["Next candidate loaded<br/>CV pre-fetched renders instantly<br/>Focus on outcome panel<br/>Session counter increments"]
    W --> I
    V -->|All screened at current filter| X["Completion indicator shown<br/>'All candidates screened'<br/>Progress: 130/130"]

    X --> Y{Override auto-advance?}
    R --> Y
    Y -->|Click different candidate| Z["Selected candidate loads<br/>instead of auto-advance target"]
    Z --> I
    Y -->|No override| AA[Session complete]

    AA --> AB["No explicit 'end session'<br/>All outcomes already persisted<br/>Close browser or navigate away"]
```

**Key design decisions:**
- Recruitment selection entry point when Lina has multiple recruitments (per FR13)
- Three-panel layout is always present (empty states before first selection, no layout shift)
- Keyboard shortcuts scoped: `1`/`2`/`3` only active when focus is on outcome panel, not in text fields
- Dual progress: total ("47 of 130") + session ("12 this session")
- Auto-advance follows the current sort order, wraps to top if no unscreened below
- Filtered list update timing: screened candidate exits filtered list *after* auto-advance completes, preventing visual disruption
- Undo window (3 seconds) eliminates need for confirmation dialogs; undo restores candidate in filtered list
- Overview update latency: ~3-4 seconds between outcome recording and overview reflection (undo window + API roundtrip). Acceptable for MVP's 3-6 concurrent users.
- CV pre-fetching makes transitions feel instant

## J4: Lina's Technical Interview Assessment (Growth -- Sketch)

```mermaid
flowchart TD
    A[Lina finishes interview] --> B[Opens candidate detail]
    B --> C["Navigates to interview step"]
    C --> D["Records outcome: Pass/Fail/Hold"]
    D --> E["Adds step-level note:<br/>'Strong coding, weak system design'"]
    E --> F["Optionally adds general candidate note:<br/>'Good cultural fit'"]
    F --> G["Both notes visible to Erik<br/>in candidate detail view"]
```

**What this adds over MVP:** Step-level notes (evaluation-specific) and general candidate notes (holistic). The outcome recording mechanics remain identical to J3.

## J5: Anders's Passive Monitoring (Growth -- Sketch)

```mermaid
flowchart TD
    A[Anders opens shared link] --> B[SSO authenticates]
    B --> C{Has Viewer role?}
    C -->|Yes| D["Overview loads (read-only)<br/>KPI cards + pipeline breakdown"]
    D --> E["Anders reads status in <10 seconds<br/>No clicks needed"]
    E --> F{Want detail?}
    F -->|Yes| G["Click step count<br/>See candidate names + statuses<br/>Cannot edit"]
    F -->|No| H[Done - closes app]
    G --> H
    C -->|No role assigned| I["Access denied message<br/>'Ask recruitment owner for access'"]
```

**What this adds over MVP:** Viewer role with read-only access. The overview UI is identical -- the difference is permission enforcement.

## J6: Sara's Viewer Validation (Growth -- Sketch)

```mermaid
flowchart TD
    A["Sara sees Erik's screen share"] --> B["Recognizes value:<br/>'I can see everything'"]
    B --> C[Sara asks Erik for access]
    C --> D["Erik invites Sara<br/>(Viewer role)"]
    D --> E[Sara clicks link → SSO → in]
    E --> F["Same overview as Erik sees<br/>Read-only, no training needed"]
    F --> G["Sara checks independently<br/>Reduces sync meetings"]
    G --> H["Sara mentions tool<br/>to other recruiting leaders<br/>Organic adoption begins"]
```

**What this validates:** The overview is screen-shareable and self-explanatory. SSO removes all friction for new users. The "pull" adoption model works when the interface speaks for itself.

## Journey Patterns

Across all flows, these reusable patterns emerge:

**Navigation Patterns:**
- **Recruitment selection:** Header breadcrumb doubles as recruitment selector when multiple recruitments exist. Rare interaction but must be discoverable.
- **Filter-in-place:** Clicking overview elements (step counts, stale indicators) filters the candidate list without page transitions. The list is the universal navigation surface.
- **Persistent layout:** The three-panel structure is always present. Selection populates panels; deselection shows empty states. No layout shifts.

**Decision Patterns:**
- **Inline decisions:** All decisions (outcome recording, import confirmation, match review) happen inline without modal interrupts. The only dialog is recruitment creation -- a one-time setup action.
- **Reversible actions with graceful list updates:** Outcome recording uses optimistic UI + 3-second undo instead of confirmation dialogs. When a filtered list is active, the screened candidate exits the list *after* auto-advance completes, preventing visual disruption. Undo restores the candidate to its position.

**Feedback Patterns:**
- **Bottom-right toast notifications:** All confirmations (outcome recorded, import complete, recruitment created) use transient bottom-right toasts that auto-dismiss after 3 seconds. Never cover the working area.
- **Progressive error severity:** Blocking errors (invalid file) stop the process with prominent messaging. Non-blocking issues (unmatched CVs) use amber treatment and let the user continue.
- **Async processing visibility:** Import operations show explicit progress indicators during server-side processing. No instant-jump from upload to summary -- the user sees the system working.

**Entry Patterns:**
- **Zero-friction authentication:** SSO handles everything. No registration, no password, no email verification.
- **Context preservation:** The app remembers overview collapse state, panel sizes, and recruitment selection via localStorage. Return visits restore the user's preferred configuration.

**Latency Patterns:**
- **Optimistic UI with delayed persistence:** Outcomes appear immediately in the local UI. Server persistence occurs after the 3-second undo window. Overview updates reflect ~3-4 seconds after the action. This is acceptable for MVP's 3-6 concurrent users and should be documented for testers.

## Flow Optimization Principles

1. **Minimize steps to value:** Erik goes from link-click to live recruitment in under 5 minutes. Lina goes from app-open to CV-on-screen in under 10 seconds. Every extra click is scrutinized.

2. **Reduce cognitive load at decision points:** Outcome recording offers exactly three choices (Pass/Fail/Hold) with an optional reason. Import summary shows counts with drill-down available but not forced. Decisions are simple; detail is on-demand.

3. **Provide clear progress indicators:** Screening shows dual progress (total + session). Import wizard shows processing state with progress feedback. The overview itself is a progress indicator for the entire recruitment.

4. **Handle errors as guidance:** Blocking errors explain what happened and what to do next. Non-blocking issues use specific counts and clear resolution paths. The tone is "here's what needs your attention" not "something went wrong."

5. **Design for the return visit:** First-visit flows (J0) are guided with empty states and CTAs. Return-visit flows (J1, J3) are fast with preserved preferences. The app optimizes for the repeated action (screening, status checking) because that's where cumulative time is spent.

6. **Account for async operations:** Import processing is not instant for large datasets. The UX must show processing state with progress rather than blocking the UI or jumping to results prematurely.

7. **Graceful list transitions:** When filtering is active, list membership changes (candidate screened → exits "unscreened" filter) happen after navigation completes, never during. Visual stability during rapid-fire screening is essential for flow state.
