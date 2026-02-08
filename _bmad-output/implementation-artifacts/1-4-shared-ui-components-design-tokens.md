# Story 1.4: Shared UI Components & Design Tokens

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **developer**,
I want the design system foundation with shared UI components and If Insurance brand tokens,
so that all feature stories use consistent, accessible components from the start.

## Acceptance Criteria

1. **Design tokens configured:** The Tailwind CSS v4 `@theme` block in `web/src/index.css` defines the If Insurance brand palette: `--color-brand-brown` (#331e11), `--color-bg-base` (#faf9f7), `--color-bg-surface` (#ffffff), `--color-border-default` (#ede6e1), `--color-interactive` (#005fcc). Semantic status colors defined: `--color-status-pass` (#1a7d37), `--color-status-fail` (#c4320a), `--color-status-hold` (#b54708). Font stack uses Segoe UI: `'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif`.

2. **shadcn/ui initialized:** Core shadcn/ui components installed in `web/src/components/ui/`: Button, Card, Table, Dialog, Select, Form, Input, Textarea, Toast, Badge, Tooltip, Skeleton, Separator, Collapsible, Progress, Alert, Sheet, DropdownMenu.

3. **StatusBadge component:** Renders with variants (Pass, Fail, Hold, Stale, Not Started). Each variant displays a distinctive icon + color + shape combination. Status is distinguishable by icon alone without color perception (WCAG). Each badge has an appropriate `aria-label`. **Note:** `Stale` is a frontend-derived display state (not a backend `OutcomeStatus` enum value). The mapping logic that determines when something is "stale" is deferred to the feature story that introduces staleness detection; this story only implements the visual variant.

4. **ActionButton component:** Renders with primary, secondary, and destructive variants. Primary uses filled style, secondary uses outlined, destructive uses red outline (never solid red fill).

5. **EmptyState component:** Renders with heading, description, and action props. Displays icon area, heading, description text, and a primary CTA button. Heading uses the appropriate heading level.

6. **Toast system:** Triggered via `useToast()`. Appears in bottom-right corner with slide-in animation (~150ms). Success toasts auto-dismiss after 3 seconds. Error toasts persist until dismissed. Info toasts auto-dismiss after 5 seconds. Animations respect `prefers-reduced-motion`.

7. **SkeletonLoader component:** Displays a placeholder matching the final layout shape.

8. **ErrorBoundary component:** When a child component throws a render error, a fallback UI is displayed instead of a crash.

9. **All shared components tested:** `npm run test` passes with assertions on rendering, accessibility, and interaction states.

## Tasks / Subtasks

- [ ] **Task 1: Configure Tailwind CSS v4 design tokens** (AC: 1)
  - [ ] Update `web/src/index.css` with `@theme` block containing all brand and semantic tokens
  - [ ] Define core palette: `--color-brand-brown` (#331e11), `--color-bg-base` (#faf9f7), `--color-bg-surface` (#ffffff), `--color-border-default` (#ede6e1), `--color-border-subtle` (#f5f0ec), `--color-interactive` (#005fcc), `--color-interactive-hover` (#004da6), `--color-text-secondary` (#6b5d54), `--color-text-tertiary` (#9c8e85)
  - [ ] Define status tokens: `--status-pass` (#1a7d37), `--status-pass-bg` (#ecfdf3), `--status-fail` (#c4320a), `--status-fail-bg` (#fef3f2), `--status-hold` (#b54708), `--status-hold-bg` (#fffaeb), `--status-info` (#005fcc), `--status-info-bg` (#eff8ff), `--status-stale` (#b54708)
  - [ ] Define font: `--font-primary: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif`
  - [ ] Set default border-radius to `rounded-md` (6px)
  - [ ] **Testing mode: Spike** — Design tokens are CSS configuration. Verified visually and through component tests.

- [ ] **Task 2: Initialize shadcn/ui** (AC: 2)
  - [ ] Run `npx shadcn@latest init` in `web/` directory
  - [ ] Configure shadcn/ui for Tailwind CSS v4 + React 19 (CSS variables mode, `web/src/components/ui/` as component directory)
  - [ ] Install `tailwind-merge` and `clsx` (shadcn/ui peer dependencies) — confirm if `class-variance-authority` is still needed with latest shadcn/ui
  - [ ] Create `web/src/lib/utils.ts` with `cn()` helper (shadcn/ui convention: `clsx` + `tailwind-merge`)
  - [ ] Add core components via CLI: `npx shadcn@latest add button card table dialog select input textarea toast badge tooltip skeleton separator collapsible progress alert sheet dropdown-menu`
  - [ ] **Note:** The `form` component requires `react-hook-form` + `@hookform/resolvers` + `zod`. Install these as peer dependencies: `npm install react-hook-form @hookform/resolvers zod`
  - [ ] Add form component: `npx shadcn@latest add form`
  - [ ] Verify all components are installed in `web/src/components/ui/`
  - [ ] **Testing mode: Spike** — CLI installation; verify components render without errors.

- [ ] **Task 3: Customize shadcn/ui theme to If Insurance brand** (AC: 1, 2)
  - [ ] Map shadcn/ui CSS variables to the brand tokens defined in Task 1
  - [ ] Override shadcn/ui defaults: `--background` → `--color-bg-base`, `--foreground` → `--color-brand-brown`, `--primary` → `--color-interactive`, `--border` → `--color-border-default`, `--ring` → `--color-interactive`
  - [ ] Verify shadcn/ui components render with brand styling (buttons use If blue, backgrounds use cream, text uses brown)
  - [ ] **Testing mode: Spike** — CSS variable mapping; verified visually.

- [ ] **Task 4: Create StatusBadge component** (AC: 3)
  - [ ] Create `web/src/components/StatusBadge.tsx`
  - [ ] Create `web/src/components/StatusBadge.types.ts` — export `StatusVariant` type: `'pass' | 'fail' | 'hold' | 'stale' | 'not-started'`
  - [ ] Implement variants using shadcn/ui Badge as base:
    - Pass: `#ecfdf3` background, `#1a7d37` text, checkmark icon
    - Fail: `#fef3f2` background, `#c4320a` text, X icon
    - Hold: `#fffaeb` background, `#b54708` text, pause icon
    - Stale: `#fffaeb` background with outline (not filled), `#b54708` text, clock icon
    - Not Started: `#f5f0ec` background, secondary text, no icon
  - [ ] Add `aria-label` prop that defaults to `"{status} outcome"` (e.g., "Pass outcome")
  - [ ] Icons: Use Lucide React icons (`Check`, `X`, `Pause`, `Clock`) — Lucide is shadcn/ui's default icon library, already installed
  - [ ] **Testing mode: Test-first** — Status indicators are WCAG-critical. Write tests first for: all 5 variants render correctly, each has correct aria-label, each has correct icon, color-independent distinguishability via icon presence.

- [ ] **Task 5: Create ActionButton component** (AC: 4)
  - [ ] Create `web/src/components/ActionButton.tsx`
  - [ ] Extend shadcn/ui Button with project-specific variants:
    - Primary: filled, If blue (`--color-interactive`), white text
    - Secondary: outlined, brand-brown border, brown text
    - Destructive: outlined, red border, red text — **never solid red fill**
  - [ ] Support loading state: text changes to present participle + inline spinner, button disabled
  - [ ] **Testing mode: Test-first** — Write tests for: all 3 variants render correctly, loading state disables button and shows spinner, destructive variant uses outline (never filled).

- [ ] **Task 6: Create EmptyState component** (AC: 5)
  - [ ] Create `web/src/components/EmptyState.tsx`
  - [ ] Props: `heading` (string), `description` (string), `actionLabel` (string, optional), `onAction` (callback, optional), `headingLevel` (`'h2' | 'h3'`, default `'h2'`), `icon` (ReactNode, optional)
  - [ ] Layout: icon area (optional), heading, description, CTA button (optional)
  - [ ] CTA button uses ActionButton primary variant
  - [ ] **Testing mode: Test-first** — Write tests for: renders heading at correct level, renders description, renders CTA when provided, doesn't render CTA when omitted, calls onAction callback when CTA clicked.

- [ ] **Task 7: Set up Toast system** (AC: 6)
  - [ ] Add `Toaster` provider from shadcn/ui toast to `web/src/App.tsx` (or app root)
  - [ ] Configure toast position: bottom-right
  - [ ] Configure durations: success = 3 seconds auto-dismiss, info = 5 seconds auto-dismiss, error = persistent until dismissed
  - [ ] Implement slide-in animation (~150ms) wrapped in `@media (prefers-reduced-motion: no-preference)` guard
  - [ ] Maximum 1 toast visible at a time (new toast replaces existing)
  - [ ] Export `useToast` hook for use throughout the app
  - [ ] **Testing mode: Test-first** — Write tests for: toast appears with correct content, success toast auto-dismisses, info toast auto-dismisses after 5s, error toast persists, `useToast` hook triggers toast.

- [ ] **Task 8: Create SkeletonLoader component** (AC: 7)
  - [ ] Create `web/src/components/SkeletonLoader.tsx`
  - [ ] Wrapper around shadcn/ui Skeleton with layout-matching presets
  - [ ] Variant props for common layouts: `variant="card"`, `variant="list-row"`, `variant="text-block"`
  - [ ] Uses `animate-pulse` (Tailwind default) — respects `prefers-reduced-motion` (no animation when motion reduced)
  - [ ] **Testing mode: Test-first** — Write tests for: renders with correct variant structure, applies animation class.

- [ ] **Task 9: Create ErrorBoundary component** (AC: 8)
  - [ ] Create `web/src/components/ErrorBoundary.tsx`
  - [ ] Implements React error boundary (class component with `componentDidCatch` and `getDerivedStateFromError`)
  - [ ] Fallback UI: "Something went wrong" heading, "Try refreshing the page" description, optional "Reload" button
  - [ ] Logs error to console (no external service in MVP)
  - [ ] Optional `fallback` prop for custom fallback UI
  - [ ] **Testing mode: Test-first** — Write tests for: catches render errors and shows fallback, shows default fallback when no custom fallback provided, shows custom fallback when provided.

- [ ] **Task 10: Update test-utils.tsx for component testing** (AC: 9) — *depends on Tasks 4-9*
  - [ ] Update `web/src/test-utils.tsx` to include `Toaster` and any other providers introduced by Tasks 4-9 in the custom render wrapper
  - [ ] Ensure test-utils render function wraps with any providers needed by shared components
  - [ ] Verify all existing tests still pass

- [ ] **Task 11: Write accessibility tests** (AC: 9) — *depends on Tasks 4-9*
  - [ ] Install `jest-axe` (or `vitest-axe`): `npm install -D vitest-axe` (Vitest-compatible version of jest-axe)
  - [ ] Add axe checks to StatusBadge tests: no WCAG violations for each variant
  - [ ] Add axe checks to ActionButton tests: all variants pass accessibility
  - [ ] Add axe checks to EmptyState tests: heading hierarchy, button accessibility
  - [ ] Verify all components pass axe automated accessibility checks

- [ ] **Task 12: Verify build and all tests pass** (AC: all) — *depends on Tasks 1-11*
  - [ ] Run `npm run build` — zero errors
  - [ ] Run `npm run test` — all tests pass
  - [ ] Run `npm run lint` — zero violations
  - [ ] Visually verify brand colors render correctly in dev server
  - [ ] **Testing mode: N/A** — Final verification.

## Dev Notes

- **Affected aggregate(s):** None — this is frontend-only. No backend code in this story.
- **Source tree:** All work in `web/` directory. No `api/` changes.

### Critical Architecture Constraints

**IMPORTANT: Stories 1-1 (scaffolding) and 1-2 (SSO authentication) must be completed first.** This story assumes:
- `web/` directory exists with Vite 7 + React 19 + TypeScript + Tailwind CSS v4
- Vitest + Testing Library + MSW configured
- ESLint + Prettier configured
- `web/src/test-utils.tsx` exists with custom render
- `web/src/index.css` exists with `@import "tailwindcss";`

**Tailwind CSS v4 — CSS-First Configuration:**
- All theme customization uses `@theme` block in CSS, NOT a JavaScript config file
- Tailwind v4 auto-detects content — no `content` array needed
- `@tailwindcss/vite` plugin is already installed (story 1.1)
- Add design tokens via `@theme` in `web/src/index.css`

**shadcn/ui Integration:**
- Components are **copied** into `web/src/components/ui/` — they're project code, not a library dependency
- shadcn/ui uses Radix UI primitives for accessibility (keyboard navigation, focus management, ARIA)
- The `cn()` utility function is the standard shadcn/ui pattern: `clsx` + `tailwind-merge`
- shadcn/ui v4 supports Tailwind CSS v4 + React 19

**Custom Components Location:**
- Shared custom components go in `web/src/components/` (top-level, NOT in `ui/` subdirectory)
- shadcn/ui base components stay in `web/src/components/ui/`
- Custom components extend shadcn/ui primitives: StatusBadge extends Badge, ActionButton extends Button

**Frontend Conventions:**
- Components: PascalCase (file and export) — `StatusBadge.tsx`
- Types: co-located `.types.ts` files — `StatusBadge.types.ts`
- Tests: co-located `.test.tsx` files — `StatusBadge.test.tsx`
- Hooks: `use` prefix, camelCase file — `useToast.ts`
- Utilities: camelCase file — `cn.ts`

**Icon Library:**
- Lucide React (`lucide-react`) is shadcn/ui's default icon library
- Installed automatically with shadcn/ui init
- Icons used in StatusBadge: `Check`, `X`, `Pause`, `Clock`

**Animation Policy (from UX spec):**
- Toast: slide-in from right (~150ms), auto-dismiss with timing
- All animations wrapped in `@media (prefers-reduced-motion: no-preference)`
- When reduced motion is preferred: transitions become instant
- No decorative animations — functional only

**Accessibility Requirements (WCAG 2.1 AA):**
- All text meets 4.5:1 contrast ratio
- Interactive elements have visible focus indicators (If blue, 2px outline, 2px offset)
- Status indicators use color + icon + shape (never color alone)
- All components must pass automated axe checks
- Install `vitest-axe` for accessibility testing in component tests

### Button Hierarchy (from UX spec — enforce in ActionButton)

| Level | Style | Usage |
|-------|-------|-------|
| Primary | Solid fill, If blue (#005fcc), white text | Single most important action per context |
| Secondary | Outline, brand-brown border, brown text | Supporting actions |
| Destructive | Outline, red border, red text. **Never solid red.** | Irreversible actions |
| Ghost | No border, brown text, hover background | Inline/low-emphasis actions |

Button text uses imperative verbs: "Create", "Import", "Confirm". Never "Submit" or "OK".
Loading state: text changes to present participle + spinner ("Creating...") and button becomes disabled.

### Toast Behavior Rules (from UX spec)

- Maximum 1 toast visible at a time — new toast replaces existing
- Success: checkmark icon, 3 seconds, auto-dismiss
- Error: X circle icon, persistent until dismissed
- Info: info circle icon, 5 seconds, auto-dismiss
- Position: bottom-right, 24px from edges
- Toast slide-in from right (~150ms), respects `prefers-reduced-motion`
- Toasts never contain critical information — they confirm actions

### StatusBadge Variants (from UX spec)

| Variant | Background | Icon | Text | Shape |
|---------|-----------|------|------|-------|
| Pass | `#ecfdf3` | Checkmark (Check) | "Pass" | Solid filled badge |
| Fail | `#fef3f2` | X icon (X) | "Fail" | Solid filled badge |
| Hold | `#fffaeb` | Pause icon (Pause) | "Hold" | Solid filled badge |
| Stale | `#fffaeb` | Clock icon (Clock) | "Stale" | **Outlined** badge (distinct from Hold) |
| Not Started | `#f5f0ec` | — (no icon) | "Not Started" | Solid filled badge |

**Key distinction:** Stale uses outlined style (border only, not filled) to be visually distinct from Hold, which shares the same amber color.

### Design Token Reference (complete)

```css
/* web/src/index.css — @theme block */
@import "tailwindcss";

@theme {
  /* Brand Colors */
  --color-brand-brown: #331e11;
  --color-bg-base: #faf9f7;
  --color-bg-surface: #ffffff;
  --color-border-default: #ede6e1;
  --color-border-subtle: #f5f0ec;
  --color-interactive: #005fcc;
  --color-interactive-hover: #004da6;
  --color-text-secondary: #6b5d54;
  --color-text-tertiary: #9c8e85;

  /* Status Colors */
  --color-status-pass: #1a7d37;
  --color-status-pass-bg: #ecfdf3;
  --color-status-fail: #c4320a;
  --color-status-fail-bg: #fef3f2;
  --color-status-hold: #b54708;
  --color-status-hold-bg: #fffaeb;
  --color-status-info: #005fcc;
  --color-status-info-bg: #eff8ff;
  --color-status-stale: #b54708;

  /* Typography */
  --font-primary: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;

  /* Border Radius */
  --radius-default: 0.375rem; /* 6px = rounded-md */
}
```

**Note on Tailwind v4 `@theme` syntax:** Tokens defined in `@theme` become available as Tailwind utilities (e.g., `bg-brand-brown`, `text-interactive`). The exact token naming convention depends on the version of Tailwind CSS v4 installed — verify during implementation that the `@theme` block syntax matches the installed version's docs. If the installed Tailwind v4 version uses a different `@theme` syntax (e.g., `--color-*` auto-prefixing), adapt accordingly.

### Previous Story Intelligence

**Story 1-1 (Project Scaffolding):**
- `web/` initialized with Vite 7 + React 19 + TypeScript
- Tailwind CSS v4 installed via `@tailwindcss/vite` — `@import "tailwindcss"` already in `index.css`
- Vitest + Testing Library + MSW configured
- `web/src/test-utils.tsx` exists with minimal custom render (no providers yet)
- ESLint + Prettier configured with import ordering
- CI: `npm ci`, `npm run lint`, `npm run build`, `npm run test -- --run`

**Story 1-2 (SSO Authentication):**
- `web/src/features/auth/` directory created with AuthContext, DevAuthProvider, msalConfig
- `web/src/lib/api/httpClient.ts` created with `apiGet<T>()`, `apiPost<T>()`
- `web/src/test-utils.tsx` updated to wrap renders with MsalProvider mock
- `web/src/mocks/auth.ts` created with mock MSAL helpers

**Story 1-3 (Core Data Model):**
- Backend-only story (domain entities, EF Core, query filters)
- No frontend impact on this story
- Defines `OutcomeStatus` enum (`NotStarted`, `Pass`, `Fail`, `Hold`) — StatusBadge variants should match these backend values

### File Structure (What This Story Creates)

```
web/src/
  components/
    ui/                          # shadcn/ui base components (installed via CLI)
      button.tsx
      card.tsx
      table.tsx
      dialog.tsx
      select.tsx
      form.tsx
      input.tsx
      textarea.tsx
      toast.tsx
      toaster.tsx
      badge.tsx
      tooltip.tsx
      skeleton.tsx
      separator.tsx
      collapsible.tsx
      progress.tsx
      alert.tsx
      sheet.tsx
      dropdown-menu.tsx
      ... (other shadcn files)
    StatusBadge.tsx               # Custom: status indicator with icon + color
    StatusBadge.types.ts          # StatusVariant type
    StatusBadge.test.tsx          # Rendering + accessibility tests
    ActionButton.tsx              # Custom: primary/secondary/destructive buttons
    ActionButton.test.tsx
    EmptyState.tsx                # Custom: empty state with heading + CTA
    EmptyState.test.tsx
    SkeletonLoader.tsx            # Custom: layout-matching skeleton presets
    SkeletonLoader.test.tsx
    ErrorBoundary.tsx             # React error boundary
    ErrorBoundary.test.tsx
  lib/
    utils.ts                     # cn() utility (clsx + tailwind-merge)
  index.css                      # Updated with @theme design tokens
```

### Libraries Installed by This Story

| Library | Purpose | Notes |
|---------|---------|-------|
| shadcn/ui (CLI) | Component scaffolding | Copies components to `components/ui/` |
| tailwind-merge | CSS class merging | shadcn/ui peer dep |
| clsx | Conditional class names | shadcn/ui peer dep |
| lucide-react | Icons | shadcn/ui default icon library |
| react-hook-form | Form management | Required by shadcn/ui Form component |
| @hookform/resolvers | Form validation resolvers | Required by Form + zod |
| zod | Schema validation | Frontend validation schemas |
| vitest-axe | Accessibility testing | axe-core integration for Vitest |
| @radix-ui/* | UI primitives | Installed automatically by shadcn/ui CLI per component |

**Note:** `class-variance-authority` (cva) may or may not be required depending on the shadcn/ui version — latest versions may have migrated to a different variant system. Check during `npx shadcn@latest init` output.

### What This Story Does NOT Include

- No API endpoints or backend code
- No routing or navigation (story 1.5)
- No app shell or header layout (story 1.5)
- No TanStack Query (story 1.5)
- No React Router (story 1.5)
- No feature-specific components (PDFViewer, SplitPanel, KPICard, etc.)
- No react-pdf or react-virtuoso
- No responsive breakpoint handling (story 1.5 handles the <1280px message)

### Anti-Patterns to Avoid

- **Do NOT install libraries that belong to later stories** (React Router, TanStack Query, react-pdf, react-virtuoso)
- **Do NOT create feature-specific components** (PDFViewer, SplitPanel, CandidateRow, KPICard, ImportWizard). Only shared foundation components.
- **Do NOT use `tailwind.config.js`** — Tailwind CSS v4 uses `@theme` in CSS
- **Do NOT hardcode hex values in component code** — always use design tokens via Tailwind classes
- **Do NOT create custom status styling per-feature** — always use the shared StatusBadge component
- **Do NOT use color as the sole status differentiator** — every status must have a distinct icon
- **Do NOT use solid red fill for destructive buttons** — always use red outline
- **Do NOT use `tailwind.config.ts`** — Tailwind v4 is CSS-first with `@theme` block
- **Do NOT skip `prefers-reduced-motion` for animations** — wrap all transitions in the media query guard
- **Do NOT create feature-level error boundaries** — the shared ErrorBoundary serves all features
- **Do NOT install a separate icon library** — use Lucide React (already comes with shadcn/ui)
- **Do NOT implement Ghost button variant** — deferred to the first feature story that requires low-emphasis inline actions

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (design tokens) | Spike | CSS configuration, verified visually |
| Task 2 (shadcn/ui init) | Spike | CLI installation, verify components render |
| Task 3 (theme mapping) | Spike | CSS variable mapping, verified visually |
| Task 4 (StatusBadge) | Test-first | WCAG-critical — must verify accessibility before implementation |
| Task 5 (ActionButton) | Test-first | Core UI component, verify variants and states |
| Task 6 (EmptyState) | Test-first | UX-critical first impression component |
| Task 7 (Toast system) | Test-first | Timed behavior needs automated verification |
| Task 8 (SkeletonLoader) | Test-first | Layout matching needs structural assertions |
| Task 9 (ErrorBoundary) | Test-first | Error handling must be verified to actually catch errors |
| Task 11 (accessibility) | Test-first | Accessibility is non-negotiable |

**Tests added by this story:**
- `StatusBadge.test.tsx` — all 5 variants, correct icons, aria-labels, axe checks
- `ActionButton.test.tsx` — primary/secondary/destructive variants, loading state, axe checks
- `EmptyState.test.tsx` — heading level, description, CTA rendering and callback, axe checks
- `Toast.test.tsx` (or within App.test.tsx) — toast trigger, auto-dismiss timing, persistence for errors
- `SkeletonLoader.test.tsx` — variant rendering, animation class
- `ErrorBoundary.test.tsx` — catches errors, shows fallback, custom fallback

**Risk covered:** Shared components are used by every feature story. If StatusBadge accessibility fails or ActionButton variants are wrong, every subsequent feature inherits the defect. Test-first approach ensures the design system foundation is correct before any feature builds on it.

### Project Structure Notes

- All new shared components go in `web/src/components/` (NOT in a feature folder)
- shadcn/ui components installed via CLI go in `web/src/components/ui/`
- Tests co-locate with their source files (e.g., `StatusBadge.test.tsx` next to `StatusBadge.tsx`)
- Type files co-locate with their component (e.g., `StatusBadge.types.ts` next to `StatusBadge.tsx`)
- `web/src/lib/utils.ts` is the standard shadcn/ui utility location

### References

- [Source: `_bmad-output/planning-artifacts/architecture.md` (core) — Enforcement Guidelines]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` — Frontend Architecture]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` — UI Consistency Rules, Shared Components]
- [Source: `_bmad-output/planning-artifacts/epics/epic-1-project-foundation-user-access.md` — Story 1.4 definition and acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/design-system-foundation.md` — shadcn/ui selection, customization strategy, component list]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md` — Color system, typography, spacing, accessibility]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md` — StatusBadge, ActionButton, EmptyState specifications]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md` — Button hierarchy, toast rules, feedback patterns]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md` — WCAG requirements, accessibility testing strategy]
- [Source: `_bmad-output/implementation-artifacts/1-1-project-scaffolding-ci-pipeline.md` — Vite/React/Tailwind setup, test infrastructure]
- [Source: `_bmad-output/implementation-artifacts/1-2-sso-authentication.md` — Auth context, test-utils updates]
- [Source: `docs/testing-pragmatic-tdd.md` — Testing policy]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
