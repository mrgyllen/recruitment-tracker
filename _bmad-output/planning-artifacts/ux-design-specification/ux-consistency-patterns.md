# UX Consistency Patterns

## Button Hierarchy

| Level | Style | Usage | Examples |
|-------|-------|-------|----------|
| **Primary** | Solid fill, If blue (`#005fcc`), white text | The single most important action on any screen. Only one per visible context. | "Create Recruitment", "Confirm Import", "Save" |
| **Outcome** | Solid fill, semantic color (green/red/amber), white text + shortcut label | Outcome recording actions. Always appear as a group of three. | "Pass (1)", "Fail (2)", "Hold (3)" |
| **Secondary** | Outline, brand-brown border, brown text | Supporting actions that aren't the primary focus | "Back", "Cancel", "View Details" |
| **Ghost** | No border, brand-brown text, hover background | Inline actions, links-as-buttons, low-emphasis actions | "Undo" in toast, "Show more" in import summary |
| **Destructive** | Outline, red border, red text. Never solid red. | Irreversible actions requiring deliberate choice | "Close Recruitment", "Remove Candidate", "Remove Member" |

**Button rules:**

- One primary button per visible context. If two actions compete for primary, one is secondary.
- Destructive actions never use solid fill (too easy to click by accident). Outline forces visual pause.
- Outcome buttons are a unique variant -- they're a group, not individual buttons. Always show all three together.
- Button text uses imperative verbs: "Create", "Import", "Confirm", "Close". Never "Submit" or "OK".
- Loading state: button text changes to present participle + spinner ("Creating..." with spinner) and becomes disabled.

**Destructive action confirmation patterns:**

- **Inline confirm for reversible destructive actions** (Remove Candidate, Remove Member): Click destructive button → button text changes to "Confirm remove?" with same button width → second click executes → 5-second timeout reverts to original state if no second click. No modal, stays inline.
- **Dialog confirm for irreversible consequential actions** (Close Recruitment): Dialog with explicit consequences: "This will lock the recruitment. Outcomes, candidates, and documents will become read-only. The GDPR retention timer will begin." Primary action: "Close Recruitment" (destructive styling). Secondary action: "Cancel".

## Feedback Patterns

Four feedback channels, each with a specific purpose:

**1. Toast notifications (bottom-right, transient)**

| Type | Icon | Duration | Dismissal | Usage |
|------|------|----------|-----------|-------|
| Success | Checkmark | 3 seconds | Auto-dismiss or click | Outcome recorded, import complete, recruitment created |
| Success + Undo | Checkmark | 3 seconds | Auto-dismiss, click, or Undo link | Outcome recorded (screening flow) |
| Info | Info circle | 5 seconds | Auto-dismiss or click | Import summary, general notifications |
| Error | X circle | Persistent (manual dismiss) | Click dismiss only | API errors, save failures |

**Toast rules:**

- Maximum 1 toast visible at a time. New toast replaces existing.
- **Rapid screening behavior:** Recording a new outcome while a previous toast is visible commits the previous outcome immediately (API call fires, undo no longer available for the replaced toast). A new undo window starts for the latest outcome only. This is acceptable behavior -- document for testers.
- Position: bottom-right, 24px from edges. Never covers the overview or candidate list.
- Animation: slide-in from right (~150ms). Respects `prefers-reduced-motion` (appears instantly).
- Toasts never contain critical information that the user must read. They confirm actions; they don't communicate new data.

**2. Inline alerts (embedded in content flow)**

| Severity | Style | Icon | Usage |
|----------|-------|------|-------|
| Blocking error | Red border, `#fef3f2` background | X circle | Invalid file format, missing required fields |
| Warning | Amber border, `#fffaeb` background | Warning triangle | Unmatched CVs, low-confidence matches |
| Info | Blue border, `#eff8ff` background | Info circle | Import counts, Workday export instructions |

**Inline alert rules:**

- Blocking errors stop the flow. The user must resolve before proceeding.
- Warnings are non-blocking. The user can acknowledge and continue.
- Info alerts provide context. No action required.
- All alerts include specific counts and clear next steps. Never just "An error occurred."

**3. Validation errors (form-level)**

- Inline below the relevant field, red text, associated via `aria-describedby`
- Appear on blur or form submission, not on keystroke
- Clear when the error condition is resolved
- Never use toast for validation errors (they need to be near the field)
- **Exception: the outcome reason textarea has NO validation.** It is optional, freeform, plain text, no min/max length, no character restrictions. Any content (including empty) is acceptable.

**4. Processing states (async operations)**

- Determinate progress bar when the backend can report percentage
- Indeterminate progress bar with descriptive text when percentage unavailable
- Text describes what's happening: "Importing candidates..." not just "Loading..."
- Cancel button available if the operation supports cancellation (import does not in MVP)

## Form Patterns

**Form structure:**

- Labels above inputs, never floating or placeholder-only
- Required fields are the default (most fields in this app are required). Optional fields are explicitly marked "(optional)"
- Validation on blur + on submit. Not on keystroke.
- Error messages below the field in red text, specific ("Recruitment name is required" not "This field is required")
- All forms use react-hook-form + zod validation schema

**Input patterns:**

| Pattern | Component | Usage |
|---------|-----------|-------|
| Short text | Input | Recruitment name, candidate name, search |
| Long text | Textarea | Outcome reason, recruitment description |
| Selection | Select (shadcn/ui) | Step filter, step selection |
| File upload | Custom drop zone | XLSX and PDF in import wizard |

**Outcome recording form (special case):**

The outcome recording form in the screening flow is deliberately minimal -- it doesn't look or feel like a form:

- Three buttons (Pass/Fail/Hold) instead of a dropdown
- Reason textarea always visible (not hidden behind "Add reason"). No validation -- freeform, optional, plain text.
- Confirm button labeled "Confirm (Enter)"
- No field labels (the context makes labels redundant -- the panel is titled with the candidate name)
- This is the only place where form conventions are relaxed for speed

## Navigation Patterns

**Page-level navigation:**

recruitment-tracker is a single-page application with a single primary view per recruitment. Navigation is about changing context within that view, not moving between pages.

| Pattern | Mechanism | Persistence |
|---------|-----------|-------------|
| Recruitment switching | RecruitmentSelector in header breadcrumb | URL parameter (`/recruitment/:id`) |
| Step filtering | Click pipeline segments or use Select dropdown | URL query parameter (`?step=screening`) |
| Candidate selection | Click in candidate list | URL parameter (`/recruitment/:id/candidate/:id`) |
| Overview collapse | Click collapse toggle or summary bar | localStorage |
| Panel resize | Drag resize handle | localStorage |

**URL strategy:**

- Client-side routing with React Router
- Recruitment and candidate IDs in URL path for shareable deep links
- Step filter in query parameter (optional, doesn't affect URL if unfiltered)
- Deep linking: sharing a URL with a candidate ID opens directly to that candidate's screening panel

**Deep link loading (cold start):**

When a user opens a deep link (e.g., `/recruitment/123/candidate/456`), the app shows a single full-page skeleton matching the final layout (header + overview + three panels). All queries (recruitment detail, candidate list, candidate PDF) fire in parallel via TanStack Query. As each query resolves, the corresponding skeleton section transitions to real content progressively. No sequential loading waterfall.

**Back button behavior:**

- Browser back navigates between recruitment views and candidate selections
- Collapsing the overview or resizing panels does NOT create history entries
- Import wizard (Sheet overlay) does NOT create history entries -- closing the sheet returns to the recruitment view

**Breadcrumb pattern:**

- Always visible in the 48px app header
- Format: `[Recruitment Name] > [Step Name]` (step name shown when filtered to a step)
- Recruitment name is clickable (RecruitmentSelector) when multiple recruitments exist
- Step name is clickable to clear the filter (return to all candidates)

**Access denied pattern:**

When a URL points to a recruitment the user doesn't have access to (or a non-existent recruitment), show a clean page with: "You don't have access to this recruitment. Contact the recruitment owner to request access." No details about the recruitment itself -- same treatment for not-found and not-authorized to prevent information leakage and enumeration.

## Loading & Empty States

**Loading states:**

| Context | Pattern | Duration target |
|---------|---------|-----------------|
| Page load (cold start / deep link) | Full-page skeleton matching final layout, progressive fill as queries resolve | <3 seconds (NFR8) |
| Candidate list | Skeleton rows in the list area | <1 second (NFR3) |
| CV viewer | Skeleton in center panel with "Loading document..." text | <2 seconds (NFR4) |
| Overview data | Skeleton KPI cards + pipeline bar | <500ms (NFR2) |
| Outcome save | Button loading state (spinner + "Saving...") | <500ms (NFR5) |

**Loading rules:**

- Show skeleton placeholders, not spinners, for content areas. Skeletons communicate structure.
- Show spinners only on buttons and inline actions (small, focused loading states).
- Never show a blank white area. If data is loading, show a skeleton.
- If loading takes >3 seconds, add descriptive text ("Loading candidates..." not just a skeleton).

**Empty states:**

| Context | Empty State Message | CTA |
|---------|-------------------|-----|
| No recruitments | "Create your first recruitment" + value proposition | "Create Recruitment" button |
| No candidates in recruitment | "Import candidates from Workday or add them manually" | "Import Candidates" button + "Add Manually" link |
| No CV for candidate | "No CV uploaded for this candidate" | "Upload CV" button |
| No candidates at filtered step | "No candidates at [Step Name]" | "Clear filter" link |
| All candidates screened | "All candidates screened" + progress summary | No CTA (completion state) |
| No screening outcome | Center + right panel: "Select a candidate to review their CV" | No CTA (instruction state) |

**Empty state rules:**

- Empty states are instruction, not decoration. Every empty state tells the user what to do next.
- Include a CTA button when there's an action to take.
- When the empty state is a completion state (all screened), celebrate briefly but don't block.
- No illustrations or graphics in empty states. Text + CTA only. This is a tool, not a consumer app.

## Data Display Patterns

**Tables (candidate list):**

- Compact rows (48px screening, 56px browse)
- Sortable columns: click header to sort, click again to reverse. Sort indicator (▲/▼) in header.
- Default sort: import order (preserves Workday export sequence)
- No row hover menu or row-level actions. Actions happen in the side panel after selecting a candidate.
- Virtualized rendering for 130+ rows (react-virtuoso)

**Metrics (KPI cards):**

- Large number + small label format
- Always show the metric even when it's 0. Never hide a card because the count is zero.
- Clickable cards filter the candidate list (KPI card click = filter action)

**Status indicators:**

- Always use StatusBadge (color + icon + text). Never just a colored dot or text alone.
- Stale indicators use clock icon + amber, always with a count ("5 candidates >7 days")

**Timestamps:**

- Use `Intl.DateTimeFormat` with browser locale. No hardcoded date format strings. At If Insurance, most browsers will use Swedish locale ('sv-SE').
- Relative time for recent events: "2 hours ago", "Yesterday"
- Absolute date for older events: formatted per browser locale (e.g., '15 jan. 2026' for sv-SE, 'Jan 15, 2026' for en-US)
- Threshold: switch from relative to absolute at 7 days
- Always show full timestamp in tooltip on hover
