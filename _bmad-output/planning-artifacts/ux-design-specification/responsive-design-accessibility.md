# Responsive Design & Accessibility

## Responsive Strategy

**Desktop-first (1280px minimum viewport):**

recruitment-tracker is an internal tool used at desks on corporate laptops and monitors. The PRD explicitly states "no mobile requirement" and "recruitment work happens at desks, not on phones."

| Viewport | Strategy | Layout |
|----------|----------|--------|
| **1280px+** (primary target) | Full three-panel screening layout, full overview with KPI cards + pipeline bar | CSS Grid three-column + collapsible overview section |
| **1024px - 1279px** (tablet/small laptop) | **Growth scope** -- not implemented in MVP. Show "best viewed on desktop" message. | N/A for MVP |
| **<1024px** (below minimum) | Not a target. Show accessible "best viewed on desktop" message. | `role="alert"` with `aria-live="assertive"` |

**Desktop layout (1280px+) -- primary design target:**

All design effort goes here. The three-panel screening layout requires 1280px minimum:

- Candidate list: 250-400px (resizable)
- Outcome panel: 300px (fixed)
- CV viewer: remaining space (~530-730px)
- App header: 48px fixed
- Overview section: collapsible, ~200px when expanded

At 1920px (typical external monitor), the CV viewer gets ~870-1070px. The layout scales gracefully up to ultra-wide monitors without modification.

**Tablet tolerance (1024px - 1279px) -- GROWTH SCOPE (not in MVP):**

> **MVP Decision:** The architecture specifies 1280px minimum viewport with no responsive breakpoints. Below 1280px, display a "please use a wider browser window" message. The tablet tolerance design below is preserved for Growth phase if small-laptop usage emerges.

<details>
<summary>Growth: Tablet degradation design (deferred)</summary>

The app doesn't redesign for tablet -- it gracefully degrades:

| Component | Desktop (1280px+) | Tablet (1024-1279px) |
|-----------|-------------------|----------------------|
| **Screening layout** | Three panels side-by-side | Two panels: candidate list + CV viewer. Outcome controls in a compact toolbar at the top of the CV viewer area (buttons + expandable reason field). Keyboard shortcuts work identically. |
| **Overview KPI cards** | Horizontal row (3-4 side-by-side) | Wrap to 2x2 grid |
| **Pipeline bar** | Full-width horizontal segments | Same, slightly compressed labels |
| **Import wizard** | Sheet from right, ~500px width | Sheet expands to full viewport width |
| **Candidate list rows** | 48px/56px with full content | Same, no reduction needed |
| **Resize handle** | Draggable between candidate list and CV viewer | Hidden (panels use fixed proportions) |

The tablet degradation is CSS-only -- no different component rendering, just media query adjustments. The compact outcome toolbar renders the same ActionButton and reason Textarea components in a different DOM position; the `useScreeningSession` hook is layout-agnostic and works with either arrangement.

</details>

## Breakpoint Strategy

| Breakpoint | Name | Layout change |
|------------|------|---------------|
| `≥1280px` | `desktop` | Full three-panel layout. Primary design target. |
| `<1280px` | `unsupported` | Accessible banner: "This application is designed for desktop browsers (1280px or wider)." `role="alert"`, `aria-live="assertive"` for screen reader announcement. No layout work. |

**Implementation approach (MVP):**

- Tailwind CSS v4: base styles target desktop (1280px+)
- Below 1280px: `@media (max-width: 1279px)` shows the unsupported message
- No `sm:`, `xs:`, or tablet breakpoint work -- this is a desktop-only app in MVP
- Test at 1280px (minimum), 1366px (common laptop), 1920px (external monitor)

## Accessibility Strategy

**Target: WCAG 2.1 Level AA** (per PRD NFR22)

Accessibility serves two goals for recruitment-tracker:

1. **Compliance** -- Enterprise tool at a Nordic insurance company. Accessibility standards are expected.
2. **Power-user efficiency** -- Keyboard-first design (the screening flow) IS the accessibility strategy. Making the app keyboard-accessible makes it faster for everyone.

**Accessibility requirements by component:**

| Component | Requirements |
|-----------|-------------|
| **SplitPanel** | Resize handle: `role="separator"`, `aria-orientation="vertical"`, keyboard-adjustable (arrow keys). Each panel: landmark region with `aria-label`. |
| **PDFViewer** | Text layer enabled (react-pdf) for screen reader access. `aria-label="CV document viewer"`. Page change announced via `aria-live="polite"`. |
| **StatusBadge** | `aria-label` with status + context. Icon + shape + color (never color alone). |
| **ActionButton** | `aria-label` with shortcut info. Focus management: after auto-advance, focus goes to first ActionButton (Pass) with no outcome pre-selected. Each candidate starts fresh. |
| **CandidateRow** | `role="option"` in `role="listbox"`. `aria-selected` for active row. Unscreened indicator has `aria-label`. |
| **KPICard** | `aria-label` with metric value. Clickable cards: `role="button"`, keyboard support. |
| **PipelineBar** | Per-segment `aria-label`. Keyboard navigation (arrow keys). Stale segments have extended `aria-label`. |
| **ImportWizard** | Step indicator announced to screen readers. File upload keyboard-accessible. Errors linked via `aria-describedby`. |
| **RecruitmentSelector** | Radix UI DropdownMenu handles focus trapping, keyboard navigation, ARIA attributes. |
| **Toast** | `role="status"`, `aria-live="polite"`. Undo link keyboard-accessible. |
| **Overview collapse** | Collapsible trigger has `aria-expanded`. Content has `aria-hidden` when collapsed. |

**Keyboard navigation map:**

| Key | Context | Action |
|-----|---------|--------|
| `Tab` | Global | Move focus to next interactive element |
| `Shift+Tab` | Global | Move focus to previous interactive element |
| `1` / `2` / `3` | Outcome panel (non-input focus) | Select Pass / Fail / Hold |
| `Enter` | Outcome panel / confirm button | Confirm outcome |
| `Tab` (after outcome select) | Outcome panel | Move focus to reason textarea |
| `Tab` (from textarea) | Outcome panel | Move focus to confirm button |
| `Arrow Up/Down` | Candidate list | Navigate between candidates |
| `Arrow Left/Right` | Pipeline bar | Navigate between pipeline segments |
| `Arrow Left/Right` | Resize handle | Adjust panel width (10px increments) |
| `Escape` | Sheet overlay (import wizard) | Close the sheet |
| `Escape` | Dropdown menus | Close the dropdown |

**Skip links:**

- Single "Skip to main content" link targeting the candidate list area (always present regardless of overview state)
- Only visible on keyboard focus (not visually present by default)

**Focus management rules:**

- After outcome confirmation + auto-advance: focus returns to first ActionButton (Pass), no outcome pre-selected. Each candidate starts fresh.
- After opening import sheet: focus trapped within sheet (Radix UI handles this)
- After closing import sheet: focus returns to the element that triggered it
- After selecting a candidate from the list: focus moves to the CV viewer area
- Dynamic content updates (overview counts after outcome recording): announced via `aria-live="polite"` -- no focus disruption

**Reduced motion:**

- All animations wrapped in `@media (prefers-reduced-motion: no-preference)` guard
- When reduced motion is preferred: toast appears instantly (no slide-in), dialog appears instantly (no fade), panel resize is immediate (it already is)
- Functional transitions (auto-advance candidate loading) still occur but without visual animation

## Testing Strategy

**Automated accessibility testing (CI/CD):**

| Tool | Integration | Purpose |
|------|-------------|---------|
| `jest-axe` | Component test suite | WCAG violation checks on rendered components. Violations fail the build. |
| Playwright accessibility assertions | Integration test suite | `toPassAxe()` assertions on full page renders. Run against J0-J3 journey flows. |
| axe-core browser extension | Developer workflow | Ad-hoc checks during component development. Not CI -- developer discretion. |

WCAG violations in automated tests fail the build. This prevents accessibility regression.

**Manual accessibility testing:**

| Test | Tool | When |
|------|------|------|
| Keyboard-only navigation | Unplug mouse, use keyboard exclusively | Every interactive component + full J3 screening flow |
| NVDA screen reader | NVDA on Windows (primary screen reader) | Each user journey flow (J0-J3) before deployment |
| Narrator screen reader | Windows Narrator (baseline, always available on corporate machines) | Baseline check on key flows -- candidate list, outcome recording, overview |
| Color blindness simulation | Chrome DevTools → Rendering → Emulate vision deficiency | StatusBadge verification, pipeline bar, overview |
| Color contrast verification | axe-core automated + manual spot checks | During design token setup + each new component |

**Responsive testing:**

| Test | Tool | When |
|------|------|------|
| Desktop layout at 1280px | Chrome DevTools viewport | Every layout change |
| Desktop layout at 1920px | Chrome DevTools viewport | Verify CV viewer scaling |
| Unsupported message at <1280px | Chrome DevTools viewport | Once during initial implementation. Verify `role="alert"` for screen readers. |
| Real laptop testing (13" - 15.6") | Physical devices at If | Before first deployment |

**Testing priority (based on adoption risk):**

1. J3 screening flow keyboard navigation (highest risk, highest value)
2. PDF viewer accessibility (text layer for screen readers)
3. Overview metrics for screen readers (KPI cards, pipeline bar)
4. Import wizard step navigation and error announcement

## Implementation Guidelines

**Responsive development (MVP):**

- Desktop-only: base styles target 1280px+ viewport
- All dimensions use relative units where appropriate (`rem` for typography, `px` for fixed structural elements like 48px header)
- Below 1280px: show unsupported viewport message, do not render application content
- No tablet breakpoint work in MVP (deferred to Growth)

**Accessibility development:**

- Semantic HTML first: `<nav>`, `<main>`, `<section>`, `<header>` before any ARIA attributes
- ARIA attributes only where semantic HTML is insufficient (e.g., custom components like PipelineBar)
- All interactive elements must be reachable via Tab and usable via Enter/Space
- Never remove focus outlines (`outline: none`) without providing a visible alternative
- Use Radix UI primitives (via shadcn/ui) for all overlay components -- they handle focus trapping, keyboard navigation, and ARIA automatically
- Run `jest-axe` on every component during development. Integrate into CI so violations fail the build.
- Integration test: run entire J3 screening flow with keyboard only -- this is the acceptance test for accessibility
