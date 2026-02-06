# Component Strategy

## Design System Components

**shadcn/ui** provides the foundation layer. The following components are used directly (installed via CLI into `web/src/components/ui/`):

| Component | Journey Usage |
|-----------|---------------|
| **Button** | Outcome recording (J3), import confirmation (J0/J2), recruitment creation (J0) |
| **Card** | KPI summary cards in overview (J1) |
| **Table** | Candidate list structure (J1, J3) |
| **Sheet** | Import wizard container (J0/J2) -- slides from right, provides full-height space |
| **DropdownMenu** | Recruitment-level actions, recruitment selector (J1/J3) |
| **Select** | Step filter, recruitment selector |
| **Form** | Recruitment creation, import wizard inputs, outcome recording. Integrates with react-hook-form + zod validation |
| **Input / Textarea** | Reason field, search, recruitment name |
| **Toast** | Bottom-right transient notifications for outcome confirmation + undo. Centralized via Toaster provider |
| **Badge** | Status indicators, step labels, stale indicators (J1) |
| **Tooltip** | Keyboard shortcut hints on hover |
| **Skeleton** | Loading states for candidate list and CV viewer |
| **Separator** | Visual dividers in split-panel layout |
| **Collapsible** | Overview section collapse/expand (J1) |
| **Progress** | Screening progress (J3), import processing progress (J0/J2) |
| **Alert** | Import flow error and warning messages (blocking vs non-blocking) |

## Custom Components

### SplitPanel

**Purpose:** Three-column resizable layout for the screening flow -- candidate list, CV viewer, and outcome controls.

**Anatomy:**

- Left column: candidate list (min 250px, max 400px)
- Center column: CV viewer (flexible, takes remaining space, white background)
- Right column: outcome panel (fixed 300px)
- Draggable resize handle between left and center columns
- No resize handle between center and right (outcome panel is fixed width)

**States:**

- Default: three columns visible at persisted or default ratios
- Empty: center and right columns show empty state messaging ("Select a candidate to review their CV")
- Active: all three columns populated with candidate data, CV, and outcome controls
- Resizing: cursor changes to col-resize, visual feedback on the handle

**Accessibility:**

- Resize handle is keyboard-accessible (arrow keys adjust width in 10px increments)
- ARIA `role="separator"` with `aria-orientation="vertical"` on the resize handle
- Each panel is a landmark region with appropriate `aria-label`

**Implementation:**

- CSS Grid with `grid-template-columns` controlled by state
- Custom `useResizablePanel` hook: mousedown/mousemove/mouseup events, enforces min/max widths, persists ratio to localStorage
- `prefers-reduced-motion` respected (resize is always immediate, no animated transitions)

### PDFViewer

**Purpose:** Renders candidate CV PDFs inline, eliminating the download-open-read-close cycle.

**Anatomy:**

- PDF rendering area (white background -- "paper on desk" effect)
- Page indicator ("Page 1 of 3")
- Scroll area for multi-page documents

**States:**

- Loading: Skeleton placeholder while PDF renders (target: page 1 <500ms)
- Loaded: PDF visible, scrollable
- Error: clear error message ("Unable to load document. Try refreshing.")
- Pre-fetching: next 2-3 candidate CVs fetched via SAS-token URLs in background (invisible to user)

**Accessibility:**

- PDF content accessible via react-pdf's text layer (selectable text for screen readers)
- Scroll area has `aria-label="CV document viewer"`
- Page indicator announced via `aria-live="polite"` on page change

**Implementation:**

- Built on react-pdf (PDF.js wrapper)
- Page 1 rendered immediately, subsequent pages lazy-loaded on scroll intersection
- Scroll position stored per-candidate in a `useRef`-backed Map, managed by `useScreeningSession` hook -- survives PDFViewer unmount/remount cycles during panel resize or candidate switching
- SAS-token pre-fetching handled by a custom `usePDFPrefetch` hook integrated with TanStack Query

### StatusBadge

**Purpose:** Displays candidate outcome status with color + icon + shape for WCAG-compliant differentiation.

**Anatomy:**

- Background fill (tinted status color)
- Icon (distinctive per status)
- Label text

**Variants:**

| Variant | Background | Icon | Text |
|---------|-----------|------|------|
| Pass | `#ecfdf3` | Checkmark (✓) | "Pass" |
| Fail | `#fef3f2` | X icon (✕) | "Fail" |
| Hold | `#fffaeb` | Pause icon (⏸) | "Hold" |
| Stale | `#fffaeb` (outlined, not filled) | Clock icon (⏱) | "Stale" |
| Not Started | `#f5f0ec` | -- (no icon) | "Not Started" |

**States:** Display-only component. No hover/active states.

**Accessibility:**

- Each badge has `aria-label` combining status and context: "Pass outcome" or "Stale: 7 days"
- Icon + shape + color ensures status is distinguishable without color perception

### ActionButton

**Purpose:** Outcome recording buttons with embedded keyboard shortcut labels.

**Anatomy:**

- Button label with shortcut hint: "Pass (1)", "Fail (2)", "Hold (3)"
- Visual state for selected outcome (highlighted before confirmation)

**States:**

- Default: standard button appearance with shortcut label
- Hover: standard hover (darker background)
- Selected: highlighted to indicate chosen outcome before confirmation
- Focused: focus ring (If blue, 2px outline, 2px offset)
- Disabled: greyed out (when no candidate is selected)

**Keyboard flow (precisely scoped):**

- Outcome panel focus: `1`/`2`/`3` selects outcome, `Enter` confirms
- Reason textarea focus: `Enter` types newline (standard textarea behavior), `1`/`2`/`3` types characters normally
- Confirm button focus: `Enter` confirms
- Confirmation is triggered only when focus is on the outcome panel (non-input elements) or the confirm button. No global Enter handler.

**Accessibility:**

- Each button has `aria-label` including the shortcut: "Pass, keyboard shortcut 1"
- Focus management: after outcome confirmation + auto-advance, focus returns to the outcome panel so shortcuts are immediately active

**Implementation:**

- Extends shadcn/ui Button with `variant="outcome"` and shortcut prop
- Scoped keydown listener via `useEffect` on the outcome panel container
- Event filtering: `event.target.tagName !== 'INPUT' && event.target.tagName !== 'TEXTAREA'`

### EmptyState

**Purpose:** Functional guidance for first-time users, replacing the empty app with a clear path forward.

**Anatomy:**

- Illustration area (minimal -- icon or simple graphic, optional)
- Heading: "Create your first recruitment"
- Description: "Track candidates from screening to offer. Your team sees the same status without meetings."
- Primary CTA button

**Variants:**

- **No recruitments:** Full onboarding guidance with value proposition
- **No candidates:** "Import candidates from Workday or add them manually" with import CTA
- **No CVs for candidate:** "Upload a CV for this candidate" with upload CTA

**Accessibility:**

- Heading uses appropriate level (h2 or h3 depending on context)
- CTA button follows standard button accessibility

### KPICard

**Purpose:** Overview summary card showing a key metric with label and optional indicator.

**Anatomy:**

- Metric value (24px bold, brand-brown)
- Metric label (14px medium, secondary text)
- Optional indicator (stale count with clock icon, or trend)

**Variants:**

- **Standard:** count + label (e.g., "130 / Total Candidates")
- **With indicator:** count + label + stale/alert indicator (e.g., "3 / Stale" with clock icon)
- **Action-oriented:** count + label + subtle CTA ("5 / Pending Action" -- clickable to filter)

**States:**

- Default: card with metric
- Hover (when clickable): subtle border color change
- Loading: Skeleton placeholder

**Accessibility:**

- Each card has `aria-label`: "Total candidates: 130"
- Clickable cards have `role="button"` and keyboard support

**Implementation:**

- Extends shadcn/ui Card with metric-specific layout
- Horizontal card layout (side-by-side in a row, not stacked)

### PipelineBar

**Purpose:** Per-step candidate count visualization showing pipeline distribution.

**Anatomy:**

- Horizontal segmented bar, one segment per workflow step
- Each segment: step name + candidate count
- Proportional width based on candidate count (minimum ~40px floor)
- Stale indicator overlay on affected segments

**States:**

- Default: all segments visible with counts
- Hover on segment: tooltip with full step name and count
- Click on segment: filters candidate list to that step
- Empty step: minimum width with step name, shows "0"
- Stale step: amber outline + clock icon on segment

**Accessibility:**

- Each segment has `aria-label`: "Screening: 47 candidates"
- Keyboard navigation between segments (arrow keys)
- Stale segments have additional `aria-label`: "5 candidates stale for more than 7 days"

**Implementation:**

- CSS Flexbox with proportional flex-basis per segment, minimum width enforced
- Click handler fires filter event consumed by candidate list
- Stale indicator driven by same threshold data from the overview API

### ImportWizard

**Purpose:** Multi-step import flow handling XLSX and/or PDF bundle upload with async processing and match review.

**Anatomy:**

- Step indicator (step 1/2/3 or progress dots)
- Content area (adapts per step)
- Step 1: file upload zone + Workday export instructions
- Processing state: progress indicator with descriptive text
- Step 2: import summary with counts and drill-down
- Step 3: match review and confirmation (if needed)
- Navigation: Back/Next/Confirm buttons

**Adaptive paths:**

- XLSX only → Processing → Summary (candidates created, no CV matching)
- PDF only → Processing → Summary (CVs extracted, manual matching needed)
- Both → Processing → Summary (candidates + matched CVs)

**Processing specification:**

- Polling interval: 2 seconds
- Timeout: 120 seconds (2x max expected processing time)
- Connection drop: shows retry option with clear messaging
- Progress indicator: determinate if backend provides percentage, indeterminate otherwise

**States:**

- File upload: drag-and-drop zone with file type validation
- Processing: progress indicator, descriptive text, no user action possible
- Summary: counts shown, drill-down expandable, amber notices for issues
- Error: blocking errors (invalid format) stop process; non-blocking (unmatched CVs) use amber treatment
- Complete: confirmation button enabled

**Accessibility:**

- Step indicator announced to screen readers
- File upload zone accessible via keyboard
- Error messages associated with relevant form elements via `aria-describedby`
- Back navigation always available (never trap in forward-only flow)

**Implementation:**

- Built on shadcn/ui Sheet (slides from right, full height) with custom multi-step state machine
- Sheet pattern provides more space than Dialog and keeps recruitment context partially visible
- File validation client-side (type + size per NFR20) before upload
- Processing state polls server for progress (202 Accepted pattern from NFR6)
- Summary data from import session API response

### CandidateRow

**Purpose:** Context-adaptive candidate list row that adjusts density based on viewing mode.

**Anatomy (screening mode -- 48px):**

- Single line: candidate name + StatusBadge (right-aligned)
- Unscreened indicator (dot, left of name)

**Anatomy (overview drill-down -- 56px):**

- Line 1: candidate name + StatusBadge
- Line 2: step name + time at step (secondary text, 13px)

**States:**

- Default: cream background (`#faf9f7`)
- Hover: warm shift background (`#f5f0ec`)
- Selected: left border accent (If blue `#005fcc`, 3px) + subtle blue tint (`#eff8ff`)
- Focused (keyboard): blue focus ring (2px outline, 2px offset)

**Accessibility:**

- Each row has `aria-selected` when active
- `role="option"` within `role="listbox"` for keyboard list navigation
- Unscreened indicator has `aria-label`: "Not yet screened"

**Implementation:**

- Virtualized with **react-virtuoso** (locked decision -- handles variable-height rows natively without VariableSizeList complexity)
- Mode prop controls layout: `mode="screening"` (48px) vs `mode="browse"` (56px)
- Integrates with SplitPanel's left column

### RecruitmentSelector

**Purpose:** Header breadcrumb that doubles as recruitment dropdown when user has multiple recruitments.

**Anatomy:**

- Current recruitment name as breadcrumb text
- Chevron/dropdown indicator (only visible when >1 recruitment)
- Dropdown contents: list of accessible recruitments with status indicator

**Three-state rendering:**

- **0 recruitments:** Hidden (EmptyState takes over the main area)
- **1 recruitment:** Static breadcrumb text, no dropdown affordance
- **2+ recruitments:** Clickable with dropdown indicator and recruitment list

**States:**

- Single recruitment: static breadcrumb text
- Multiple recruitments: clickable with dropdown indicator
- Dropdown open: list of recruitments with current highlighted
- Hover on dropdown item: standard hover styling

**Accessibility:**

- Uses shadcn/ui DropdownMenu primitives (Radix UI handles focus management)
- `aria-label`: "Switch recruitment" on the trigger
- Current recruitment marked with `aria-current="true"`

**Implementation:**

- Extends shadcn/ui DropdownMenu
- Conditionally renders based on recruitment count (0: hidden, 1: static, 2+: dropdown)
- Positioned in the app header breadcrumb area, left side

## Component Implementation Strategy

**Coordination hook: `useScreeningSession`**

The screening flow involves coordinated state across SplitPanel, CandidateRow, PDFViewer, and ActionButton. Rather than prop-drilling through three levels, a custom `useScreeningSession` hook manages the shared state:

- **Current candidate selection** -- which candidate is selected, drives PDF loading and outcome panel
- **Outcome recording lifecycle** -- pending outcome state, 3-second undo window, delayed API call, optimistic UI update
- **Auto-advance logic** -- determines next unscreened candidate after outcome recording, handles wrap-around and filtered list behavior
- **Session progress counters** -- total screened count and session-specific count (client-side, resets on refresh)
- **PDF pre-fetch triggers** -- signals next 2-3 candidates for SAS-token URL pre-fetching
- **Scroll position memory** -- per-candidate scroll positions stored in a `useRef`-backed Map, survives component lifecycle changes
- **Toast/undo lifecycle** -- outcome recorded → pending state → toast with undo callback → 3-second timer → API call → cache invalidation

This hook is the "brain" of the screening flow. Components consume its state and dispatch actions to it.

**Build strategy: Components as journeys require them.**

Don't pre-build the full component library. Build each component when its journey is being implemented. Foundation layer (design tokens + shadcn/ui base install) must come first, then components are built in journey priority order.

**Composition principle:** Every custom component is built on top of shadcn/ui primitives where possible. StatusBadge extends Badge. ActionButton extends Button. KPICard extends Card. ImportWizard uses Sheet + Form + Progress. This maintains design consistency and reduces custom code.

## Implementation Roadmap

**Phase 1 -- Foundation (blocks all other work):**

| Component | Needed for | Dependencies |
|-----------|-----------|-------------|
| Design tokens (`@theme` setup) | Everything | None |
| shadcn/ui base install (Button, Card, Table, Sheet, Toast, Badge, etc.) | Everything | Design tokens |
| SplitPanel | J3 screening flow | Design tokens |
| CandidateRow (with react-virtuoso) | J1 + J3 candidate list | shadcn/ui, react-virtuoso |

**Phase 2 -- Screening flow (J3 -- highest adoption risk):**

| Component | Needed for | Dependencies |
|-----------|-----------|-------------|
| `useScreeningSession` hook | J3 screening coordination | Phase 1 components |
| PDFViewer | J3 CV display | react-pdf, SAS-token API, useScreeningSession |
| ActionButton | J3 outcome recording | shadcn/ui Button, useScreeningSession |
| StatusBadge | J3 outcome display | shadcn/ui Badge |

**Phase 3 -- Overview (J1 -- daily value):**

| Component | Needed for | Dependencies |
|-----------|-----------|-------------|
| KPICard | J1 overview dashboard | shadcn/ui Card |
| PipelineBar | J1 pipeline visualization | Design tokens |
| RecruitmentSelector | J1/J3 multi-recruitment navigation | shadcn/ui DropdownMenu |

**Phase 4 -- Import and onboarding (J0/J2 -- setup flows):**

| Component | Needed for | Dependencies |
|-----------|-----------|-------------|
| ImportWizard | J0/J2 candidate import | shadcn/ui Sheet, Form, Progress, Alert |
| EmptyState | J0 first-time experience | shadcn/ui Button |
