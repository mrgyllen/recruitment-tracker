# Epic 4: Screening & Outcome Recording

Lina can screen candidates using the split-panel layout with inline CV viewing, keyboard-first outcome recording, and batch flow with auto-advance.

## Story 4.1: Candidate List & Search/Filter

As a **user**,
I want to view a paginated list of candidates within a recruitment and search or filter them by name, email, step, or outcome status,
So that I can quickly find specific candidates or focus on candidates at a particular stage.

**Acceptance Criteria:**

**Given** an active recruitment has candidates
**When** the user navigates to the recruitment view
**Then** a candidate list is displayed showing each candidate's name, email, current workflow step, and outcome status
**And** results are paginated (up to 50 per page)
**And** the list loads within 1 second (NFR3)

**Given** the candidate list is displayed
**When** the user types in the search field
**Then** the list filters to candidates matching by name or email (case-insensitive, substring match)
**And** search is debounced to avoid excessive API calls

**Given** the candidate list is displayed
**When** the user selects a workflow step filter
**Then** only candidates currently at that step are shown

**Given** the candidate list is displayed
**When** the user selects an outcome status filter (Not Started, Pass, Fail, Hold)
**Then** only candidates with that outcome at their current step are shown

**Given** the user applies both a step filter and an outcome filter
**When** the list updates
**Then** both filters are applied together (AND logic)
**And** the active filters are visually indicated and individually clearable

**Given** the candidate list has more than 50 candidates matching the current filters
**When** the user views the list
**Then** pagination controls are displayed
**And** navigating between pages maintains the active search and filter state

**Given** a recruitment has no candidates
**When** the user views the candidate list
**Then** an empty state is shown with guidance: "No candidates yet" and actions for "Import from Workday" and "Add Candidate"

**Given** the candidate list has 130+ candidates
**When** the user scrolls the list
**Then** the list renders efficiently using virtualization (react-virtuoso) to keep the DOM light

**Given** the user clicks a candidate in the list
**When** the candidate is selected
**Then** the candidate's complete profile is displayed: imported data (name, email, phone, location, date applied), linked documents, and outcome history across all completed steps

**Technical notes:**
- Backend: `GetCandidatesQuery` (paginated, with search/filter params, includes batch SAS URLs for documents), `GetCandidateByIdQuery` (single candidate detail with SAS URL)
- Frontend: `CandidateList.tsx`, `CandidateDetail.tsx`
- react-virtuoso for list virtualization at scale
- SAS URLs embedded in list response for screening pre-fetch (architecture decision #6)
- FR36, FR45, FR46, FR51 fulfilled

## Story 4.2: PDF Viewing & Download

As a **user (Lina)**,
I want to view a candidate's CV inline within the application and download it when needed,
So that I can review CVs without downloading files and switching between applications.

**Acceptance Criteria:**

**Given** a candidate has a linked PDF document
**When** the user selects that candidate
**Then** the PDF renders inline in the viewer panel using react-pdf with the SAS-authenticated URL
**And** the PDF loads within 2 seconds (NFR4)

**Given** the PDF is displayed in the viewer
**When** the user scrolls
**Then** all pages of the document are accessible via react-pdf's per-page lazy loading on scroll

**Given** a candidate has a linked PDF document
**When** the user clicks the "Download" action
**Then** the PDF is downloaded to the user's device via the SAS URL

**Given** a candidate does not have a linked document
**When** the user selects that candidate
**Then** the viewer panel shows an empty state: "No CV available" with an "Upload CV" action (if recruitment is active)

**Given** the user is viewing candidate A's PDF and selects candidate B
**When** candidate B loads
**Then** candidate B's PDF replaces candidate A's in the viewer
**And** if candidate B's SAS URL was pre-fetched, the PDF loads from the pre-fetched URL

**Given** the user is reviewing candidate N in the candidate list
**When** the viewer is active
**Then** the system pre-fetches SAS URLs for the next 2-3 candidates' PDFs in the background
**And** pre-fetched PDFs load faster when those candidates are selected

**Given** a SAS token has expired (>15 minutes)
**When** the user attempts to view or download a PDF
**Then** a fresh SAS URL is requested from the API transparently
**And** the PDF loads without user intervention

**Technical notes:**
- Frontend: `PdfViewer.tsx` (react-pdf, SAS URL rendering), `usePdfPrefetch.ts` hook
- SAS URLs from batch candidate list response (pre-fetched during list load)
- react-pdf (PDF.js wrapper) with text layer for screen reader accessibility and per-page lazy loading
- NFR4: PDF load within 2 seconds via SAS token streaming
- NFR15: SAS tokens max 15-minute validity
- FR34, FR35 fulfilled

## Story 4.3: Outcome Recording & Workflow Enforcement

As a **user (Lina)**,
I want to record an outcome (Pass, Fail, or Hold) for a candidate at their current workflow step with an optional reason, and advance passing candidates to the next step,
So that screening decisions are documented and candidates progress through the hiring pipeline.

**Acceptance Criteria:**

**Given** a candidate is at a workflow step with no outcome recorded
**When** the user views the outcome controls
**Then** three outcome buttons are displayed: Pass, Fail, and Hold
**And** an always-visible reason textarea is shown (not hidden behind an "add note" button)
**And** a confirm button is available

**Given** the user selects an outcome (Pass, Fail, or Hold)
**When** they click the confirm button (or press Enter)
**Then** the outcome is recorded for the candidate at their current step
**And** the optional reason text is saved with the outcome
**And** visual confirmation is shown within 500ms (NFR5)

**Given** the user records a "Pass" outcome
**When** the outcome is saved
**Then** the candidate is automatically advanced to the next workflow step
**And** the candidate's status at the new step is "Not Started"

**Given** the user records a "Fail" or "Hold" outcome
**When** the outcome is saved
**Then** the candidate remains at their current step
**And** the candidate's outcome status reflects the recorded decision

**Given** a candidate is at the last workflow step
**When** the user records a "Pass" outcome
**Then** the candidate is marked as having completed all steps
**And** no further advancement occurs

**Given** a candidate already has an outcome recorded at their current step
**When** the user views the outcome controls
**Then** the previously recorded outcome and reason are displayed
**And** the user can update the outcome (re-record with a different decision)

**Given** a candidate is at step 3 of a 5-step workflow
**When** the user attempts to record an outcome at step 5 directly
**Then** the system enforces the workflow step sequence
**And** the candidate can only have outcomes recorded at their current step

**Given** the user views a candidate's detail
**When** they look at the outcome history section
**Then** all completed steps are shown with their recorded outcome, reason, who recorded it, and when

**Given** a closed recruitment
**When** the user views a candidate's outcome controls
**Then** outcome recording is disabled (read-only mode)

**Technical notes:**
- Backend: `RecordOutcomeCommand` + handler + FluentValidation, `GetCandidateOutcomeHistoryQuery`
- Domain: `Candidate.RecordOutcome()` enforces step sequence via `InvalidWorkflowTransitionException`, raises `OutcomeRecordedEvent`
- Frontend: `OutcomeForm.tsx` with Pass/Fail/Hold buttons + reason textarea
- NFR5: visual confirmation within 500ms
- FR37, FR38, FR39, FR40 fulfilled

## Story 4.4: Split-Panel Screening Layout

As a **user (Lina)**,
I want a split-panel layout where I can see the candidate list, their CV, and outcome controls side by side, with the ability to navigate between candidates and have auto-advance after recording an outcome,
So that I can screen candidates in a continuous flow without page reloads or context switching.

**Acceptance Criteria:**

**Given** the user navigates to a recruitment's screening view
**When** the page loads
**Then** a three-panel layout is displayed: candidate list (left, min 250px), CV viewer (center, flexible width), outcome controls (right, fixed ~300px)
**And** the layout uses CSS Grid
**And** before any candidate is selected, the center and right panels show empty states ("Select a candidate to review their CV")

**Given** the three-panel layout is displayed
**When** the user drags the divider between the left and center panels
**Then** the panels resize proportionally
**And** the resize ratio is persisted to localStorage
**And** on next visit, the persisted ratio is restored

**Given** the user selects a candidate in the left panel
**When** the candidate loads
**Then** the center panel shows their CV (via PdfViewer from Story 4.2)
**And** the right panel shows their outcome controls (via OutcomeForm from Story 4.3)
**And** the candidate name and current status are displayed in the right panel header

**Given** the user is viewing candidate A and clicks candidate B in the list
**When** candidate B is selected
**Then** the CV viewer updates to candidate B's document
**And** the outcome controls update to candidate B's current step and status
**And** the transition happens without a page reload

**Given** the user records an outcome for a candidate
**When** the outcome is confirmed
**Then** the outcome is applied optimistically (shown immediately in the UI without waiting for the API)
**And** a bottom-right toast slides in (~150ms): "Pass recorded for [Name] - Undo"
**And** the toast auto-dismisses after 3 seconds
**And** during those 3 seconds, clicking "Undo" reverses the action (no API call needed)
**And** after 3 seconds, the outcome is persisted to the server via API call

**Given** the user clicks "Undo" on the toast within 3 seconds
**When** the undo is processed
**Then** the outcome is reversed in the UI
**And** the candidate returns to their previous state
**And** no API call is made (the delayed persist is cancelled)

**Given** the user records an outcome and auto-advance is active
**When** the outcome is confirmed (after the brief ~300ms confirmation transition)
**Then** the next unscreened candidate below the current one in the list is automatically selected
**And** their CV and outcome controls load
**And** if no unscreened candidates remain below, it wraps to the top of the list
**And** if all candidates in the current filter are screened, the view stays on the current candidate with a completion indicator

**Given** the user wants to override auto-advance
**When** they click a different candidate in the list during or after the confirmation transition
**Then** the clicked candidate is selected instead of the auto-advance target

**Given** the user is in the screening layout
**When** they view the candidate list panel
**Then** screening progress is displayed: total progress ("47 of 130 screened") and session progress ("12 this session")
**And** session progress is a client-side counter that resets on page refresh

**Technical notes:**
- Frontend: `ScreeningLayout.tsx` (CSS Grid container with resizable divider), `CandidatePanel.tsx` (left panel), custom `useResizablePanel` hook with localStorage persistence
- Frontend: `useScreeningSession.ts` hook (session state coordination, optimistic updates, undo, auto-advance)
- Three isolated state domains: candidate list, PDF viewer, and outcome form coordinate but must not cascade re-renders
- Optimistic UI: TanStack Query cache updated immediately, API call delayed 3 seconds, rollback on undo
- FR41, FR42, FR43 fulfilled

## Story 4.5: Keyboard Navigation & Screening Flow

As a **user (Lina)**,
I want to perform the entire screening workflow using keyboard navigation alone, with shortcuts for outcome recording and predictable focus management,
So that I can screen candidates at maximum speed without reaching for the mouse.

**Acceptance Criteria:**

**Given** the screening layout is active with a candidate selected
**When** focus is on the outcome panel (not in a text input)
**Then** pressing `1` selects Pass, pressing `2` selects Fail, pressing `3` selects Hold
**And** the corresponding outcome button is visually highlighted

**Given** the user presses `1`, `2`, or `3` while typing in the reason textarea
**When** the keypress is handled
**Then** the character is typed into the textarea (normal text input behavior)
**And** the shortcut does NOT trigger outcome selection
**And** shortcuts are scoped to the outcome panel via keydown listener filtered by active element

**Given** an outcome is selected via keyboard shortcut
**When** the user presses `Tab`
**Then** focus moves to the reason textarea
**And** pressing `Tab` again moves focus to the confirm button
**And** pressing `Enter` on the confirm button records the outcome

**Given** an outcome is confirmed
**When** the auto-advance completes and the next candidate loads
**Then** focus returns to the outcome panel (not the reason textarea, not the candidate list)
**And** `1`/`2`/`3` shortcuts are immediately active for the next candidate

**Given** the candidate list panel has focus
**When** the user presses Arrow Up or Arrow Down
**Then** the selection moves to the previous or next candidate in the list
**And** the CV viewer and outcome controls update accordingly

**Given** the user navigates the candidate list with Arrow keys
**When** a new candidate is selected
**Then** focus remains on the candidate list (not stolen by the CV viewer or outcome panel)
**And** the user can press `Tab` to move focus to the outcome panel when ready to record

**Given** all interactive elements in the screening layout
**When** the user navigates via keyboard
**Then** all elements are reachable via Tab navigation in a logical order: candidate list → CV viewer → outcome controls
**And** focus indicators (blue 2px outline, 2px offset) are visible on all focused elements

**Given** the outcome buttons are rendered
**When** the user inspects the UI
**Then** each button displays the keyboard shortcut hint: "Pass (1)", "Fail (2)", "Hold (3)"

**Given** the screening layout is active
**When** any dynamic content updates (candidate switch, outcome recorded, auto-advance)
**Then** assistive technologies are notified via ARIA live regions
**And** the outcome panel has appropriate `role` and `aria-label` attributes

**Given** the user completes the screening flow using only keyboard
**When** they screen multiple candidates in sequence
**Then** the flow is: select outcome (1/2/3) → optional Tab to reason → Tab to confirm → Enter → auto-advance → repeat
**And** the entire flow operates without mouse interaction

**Technical notes:**
- Frontend: `useKeyboardNavigation.ts` hook (keyboard shortcut handling, focus management, scoping)
- Keyboard shortcut scoping: keydown listener on outcome panel, filtered to ignore events when `activeElement` is input/textarea
- Focus management contract: after outcome submission, focus MUST return to outcome panel for keyboard shortcuts
- NFR22: WCAG 2.1 AA compliance
- NFR23: all interactive elements keyboard-reachable
- NFR24: batch screening fully operable via keyboard
- NFR28: ARIA live regions for dynamic content
- NFR29: predictable focus management during sequential workflows
- FR44 fulfilled

---
