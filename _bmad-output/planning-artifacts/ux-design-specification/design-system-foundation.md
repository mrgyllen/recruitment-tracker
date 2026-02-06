# Design System Foundation

## Design System Choice

**shadcn/ui** -- a collection of copy-paste React components built on Radix UI primitives, styled with Tailwind CSS. Components are copied into the project (not installed as a dependency), giving full ownership and customization control.

## Rationale for Selection

| Factor | shadcn/ui fit |
|--------|---------------|
| **Tech stack alignment** | Built for React + Tailwind CSS. Native pairing with the architecture's selected stack (React 19 + Tailwind CSS v4 + Vite 7). |
| **Accessibility** | Radix UI primitives handle keyboard navigation, focus management, ARIA attributes, and screen reader support. Critical for the keyboard-first screening flow (1/2/3 shortcuts, Tab, Enter). |
| **Customization** | Copy-paste model means every component lives in the project codebase. Full control over styling, behavior, and structure. No fighting a library's opinions. |
| **Aesthetic** | Default visual style is clean, minimal, and professional -- aligns with "serious business tool made by engineers" aesthetic. No consumer-app decoration. |
| **Solo dev efficiency** | Pre-built components for common patterns (Dialog, Select, Table, Toast, Tabs, Tooltip) reduce implementation time without sacrificing control. |
| **No vendor lock-in** | Components are source code in the project, not an npm dependency. No risk of breaking changes from upstream releases. |
| **Bundle size** | Only the components actually used are included. No unused component code shipped to the browser. |

## Implementation Approach

**Component installation:** Use the shadcn/ui CLI to add components as needed during feature development. Each component is copied into `web/src/components/ui/` and becomes project-owned code.

**Tailwind CSS v4 configuration:** All theme customization uses the CSS-first configuration pattern (`@theme` block in `index.css`), not a JavaScript config file. This is the Tailwind v4 standard and aligns with shadcn/ui's v4 support.

**Core components needed for MVP:**

| Component | Usage |
|-----------|-------|
| **Button** | Primary actions, outcome recording (Pass/Fail/Hold) |
| **Card** | KPI summary cards in overview section |
| **Table** | Candidate list with sortable columns |
| **Dialog** | Import wizard steps, recruitment creation |
| **DropdownMenu** | Recruitment-level actions (edit workflow, close recruitment, manage members) |
| **Select** | Step filter, recruitment selector |
| **Form** | Recruitment creation, import wizard inputs, outcome recording. Integrates with react-hook-form + zod validation (frontend validation is UX convenience per architecture) |
| **Input / Textarea** | Reason field on outcome recording, search |
| **Toast** | Bottom-right transient notifications for outcome confirmation + undo. Centralized via a Toaster provider at app root, invoked imperatively via `toast()` from any component |
| **Badge** | Status indicators, step labels, stale indicators |
| **Tooltip** | Keyboard shortcut hints on hover |
| **Skeleton** | Loading states for candidate list and CV viewer |
| **Separator** | Visual dividers in split-panel layout |
| **Collapsible** | Overview section collapse/expand |
| **Progress** | Screening progress indicator (total + session) |
| **Alert** | Import flow error and warning messages (blocking vs non-blocking) |

**Custom components built on top of shadcn/ui primitives:**

| Component | Purpose |
|-----------|---------|
| **StatusBadge** | Outcome status with color + icon for WCAG compliance: Pass (green + checkmark), Fail (red + X icon), Hold (amber + pause icon). Shape+icon ensures status is unambiguous regardless of color perception. |
| **ActionButton** | Outcome recording buttons with embedded keyboard shortcut labels ("Pass (1)") |
| **EmptyState** | Functional guidance for first-time users ("Create your first recruitment") |
| **PDFViewer** | Inline CV renderer using react-pdf (PDF.js). Renders page 1 immediately (<500ms target), subsequent pages lazy-loaded on scroll. Combined with SAS-token pre-fetching of next 2-3 candidate CVs for seamless screening flow. |
| **SplitPanel** | CSS Grid three-column layout with draggable resize handle. Custom `useResizablePanel` hook handles mousedown/mousemove events, enforces min/max column widths, and persists layout ratio to localStorage. |
| **KPICard** | Overview summary card extending shadcn Card with count + label + trend indicator |
| **PipelineBar** | Per-step candidate count visualization for overview section |

## Customization Strategy

**Design tokens (Tailwind CSS v4 `@theme` in CSS):**

- **Typography:** Segoe UI as primary font family (zero loading cost -- pre-installed on all Windows corporate machines, Microsoft ecosystem default). Inter as web font fallback for non-Windows environments. System font stack as final fallback. Clean, professional, readable at information-dense sizes.
- **Color palette:** Neutral base (gray scale for chrome/structure). Semantic colors for status only: green for Pass, red for Fail, amber for Hold/Warning, blue for informational. No decorative color. All status colors paired with distinctive icons (checkmark, X, pause, info) for WCAG compliance -- color is never the sole differentiator.
- **Spacing:** Tailwind's default spacing scale. Slightly more whitespace than VS Code but denser than typical consumer apps. Optimized for information density while remaining scannable for occasional users (Anders).
- **Border radius:** Minimal -- `rounded-md` (6px) as default. Sharp enough to feel professional, soft enough to not feel austere.
- **Shadows:** Minimal to none. Flat design with border-based separation. No decorative shadows.

**Animation policy:**
- Dialogs: fade only (~150ms), no scale/slide
- Toast notifications: subtle slide-in from right edge (~150ms) for spatial context ("notifications come from the right"), auto-dismiss after 3 seconds
- Panel resize: immediate response, no easing
- All other transitions: functional only, never decorative. Engineers notice and resent gratuitous animation.

**Dark mode:** Not in MVP scope. The architecture specifies no dark mode requirement. If added later, shadcn/ui's CSS variable-based theming and Tailwind v4's `@theme` approach make this straightforward.

**Component overrides:** shadcn/ui components customized in-place to match the professional aesthetic:
- Buttons use solid fills for primary actions, outline for secondary
- Toast notifications positioned bottom-right, slide-in from right, auto-dismiss after 3 seconds, include undo action link
- Tables use compact row height for information density in candidate list
- Forms integrate react-hook-form + zod for consistent validation patterns across all input surfaces
