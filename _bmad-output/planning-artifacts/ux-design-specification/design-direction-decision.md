# Design Direction Decision

## Design Directions Explored

The design direction for recruitment-tracker was established collaboratively through steps 3-8 rather than through competing visual alternatives. This was the right approach because the constraints (If Insurance brand, engineering user base, desktop-first, keyboard-driven screening flow) naturally converge on a single coherent direction. Generating artificial visual variations would have been counterproductive.

A comprehensive HTML mockup was generated at `_bmad-output/planning-artifacts/ux-design-directions.html` to validate the consolidated direction across five key views: screening flow, collapsed overview, empty state, import wizard, and color palette.

## Chosen Direction

**"Nordic Professional" -- If Insurance warmth meets engineering efficiency.**

The visual direction balances two forces:
- **Corporate legitimacy:** If's warm brand palette (brown text on cream backgrounds, taupe borders, blue interactive elements) signals "this is an organizational tool" to stakeholders and non-technical users.
- **Engineering respect:** The interaction density, keyboard shortcuts, split-panel layout, and instant feedback signal "this was built by someone who understands how we work" to engineers.

The result is a tool that looks like it belongs in the If ecosystem but behaves like the engineering tools its primary users prefer.

## Design Rationale

| Decision | Rationale |
|----------|-----------|
| **If brand colors as foundation** | Corporate acceptance. When Sara (HR) or Anders (stakeholder) sees the tool, it looks trustworthy and organizational, not like an engineer's side project. |
| **Sharper radius (6px vs If's 12px)** | More functional, tool-like feel. The marketing site needs to feel warm and approachable; the internal tool needs to feel efficient and precise. |
| **Three-panel always visible** | Eliminates layout shifts. Mirrors VS Code's stable panel structure. The layout is predictable from first load. |
| **Cream background + white surfaces** | Creates subtle depth hierarchy without shadows. White CV viewer panel preserves document fidelity ("paper on desk" effect). |
| **No web fonts** | Zero loading latency. Segoe UI is pre-installed on all target machines. The font choice is invisible to users -- which is the point. |
| **Compact type scale (24px max)** | This is a tool, not a content site. Every pixel serves information density. No hero headings, no decorative typography. |
| **Collapsed overview as summary bar** | Serves both Erik (expandable detail) and Lina (minimal footprint). Key numbers visible even when collapsed. Entire bar is clickable to expand (not just the button) -- reduces target area friction. |

## Mockup Refinements (from review)

**Candidate list rows -- context-adaptive display:**
- **During step-filtered screening** (Lina's flow): Single-line rows (48px) -- name + status badge. The step is implicit from the filter.
- **During overview drill-down** (Erik browsing): Two-line rows (56px) -- line 1: name + status badge, line 2: step name + time at step in secondary text. This gives Erik the context he needs when browsing across steps.

**Import wizard -- sequential flow:**
- The upload zone and the matching alert should not appear simultaneously. After files are uploaded, the drop zone transitions to an upload summary state ("127 CVs uploaded") with the amber matching alert below ("3 candidates have no matching CV"). This makes the flow feel sequential and avoids showing error states prematurely.

**Empty state -- value communication:**
- The empty state adds a brief value proposition below the description: "Track candidates from screening to offer. Your team sees the same status without meetings." This isn't marketing -- it's the 10-second pitch that helps Erik explain the tool when he invites Lina and Anders.
- Recruitment creation dialog is minimal: name + optional description, with workflow steps using sensible defaults (CV Screening → Technical Interview → Interview → Reference → Offer). Erik can customize steps after creation. Priority: first recruitment created in under 30 seconds.

**Pipeline bar minimum width:**
- Steps with zero candidates show a minimum width (~40px) so the step label remains readable. Proportional widths calculated from `candidateCount / totalCandidates` with minimum floor.

## Implementation Approach

The HTML mockup at `ux-design-directions.html` serves as the visual reference for implementation. Key implementation notes:

1. **Design tokens first:** All colors, spacing, and typography defined in Tailwind v4 `@theme` block before any component work. The mockup's CSS custom properties map directly to `@theme` values.
2. **shadcn/ui components:** Map mockup elements to shadcn/ui components (Card for KPI, Badge for status, Dialog for import wizard, Toast for confirmations).
3. **Custom components:** SplitPanel, PDFViewer, StatusBadge, ActionButton, KPICard, PipelineBar are custom builds on top of shadcn/ui primitives.
4. **List virtualization required:** The candidate list must use react-window or react-virtuoso for 130+ candidate lists. The mockup shows ~12 items but the real list needs virtualization to keep the DOM light and scrolling smooth.
5. **Validate secondary text color:** `#6b5d54` may appear muddy at 13px -- test during implementation and consider `#78716c` as fallback.
6. **The mockup is a reference, not a pixel-perfect spec.** Implementation should match the spirit (warm palette, dense layout, clean chrome) while adapting to real data and interaction states.
