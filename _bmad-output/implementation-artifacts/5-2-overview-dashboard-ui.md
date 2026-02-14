# Story 5.2: Overview Dashboard UI

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **user (Erik)**,
I want a **collapsible overview section at the top of the recruitment page showing KPI cards, a pipeline breakdown with per-step counts, stale indicators, and pending actions**,
so that **I can understand the pipeline status at a glance without clicking into individual candidates**.

## Acceptance Criteria

### AC1: Overview section visible on page load
**Given** the user navigates to a recruitment
**When** the page loads
**Then** the overview section is visible at the top of the page (unless previously collapsed)
**And** the overview data loads independently from the candidate list below (FR50)

### AC2: KPI cards and pipeline breakdown
**Given** the overview section is expanded
**When** the user views it
**Then** KPI cards display: total candidates, pending action count, and stale candidate count
**And** a pipeline breakdown shows candidate counts per workflow step as a horizontal bar or segmented display

### AC3: Collapse to inline summary bar
**Given** the overview section is expanded
**When** the user clicks the collapse toggle
**Then** the overview collapses to an inline summary bar: "130 candidates - 47 screened - 3 stale"
**And** the collapse state is persisted to localStorage
**And** on next visit, the section loads in the persisted state

### AC4: Expand from collapsed state
**Given** the overview section is collapsed
**When** the user clicks the summary bar or expand toggle
**Then** the full overview expands with KPI cards and pipeline breakdown

### AC5: Stale indicator per step
**Given** a workflow step has candidates exceeding the stale threshold
**When** the overview is displayed
**Then** the step shows a stale indicator using a clock icon + amber color (not color only, NFR27)
**And** the indicator shows the count: "5 candidates > 5 days"

### AC6: Click step to filter candidate list
**Given** the pipeline breakdown shows per-step counts
**When** the user clicks a step name or count
**Then** the candidate list below filters to show only candidates at that step
**And** the filter is applied without a page transition
**And** the active filter is visually indicated and clearable

### AC7: Click stale indicator to filter stale candidates
**Given** the stale indicator is visible on a step
**When** the user clicks the stale indicator
**Then** the candidate list below filters to show only stale candidates at that step

### AC8: Overview updates on outcome recording
**Given** the overview data is loaded
**When** an outcome is recorded in the screening flow below
**Then** the overview KPI cards and pipeline counts update via TanStack Query cache invalidation (background refetch, no loading state)

### AC9: Visual design and brand compliance
**Given** the overview section is rendered
**When** the user inspects it
**Then** KPI numbers use the 24px bold type scale
**And** all status indicators use the shared `StatusBadge` component
**And** the section respects the If Insurance brand palette (cream backgrounds, warm browns)

### Prerequisites
- **Story 5.1** (Overview API & Data) -- `GET /api/recruitments/{id}/overview` endpoint returning `RecruitmentOverviewDto`
- **Story 4.1** (Candidate List & Search/Filter) -- `CandidateList.tsx` with filtering support (`useCandidates` hook with `stepFilter` and `staleFilter` params)
- **Story 1.4** (Shared UI) -- `StatusBadge`, `SkeletonLoader`, `EmptyState`, `useAppToast`
- shadcn/ui `Collapsible` component (already installed at `web/src/components/ui/collapsible.tsx`)

### FRs Fulfilled
- **FR47:** Overview dashboard shows candidate counts per workflow step
- **FR48:** Stale candidate detection with configurable threshold indicator
- **FR49:** Pending actions count displayed
- **FR50:** Overview data loads independently from candidate list (UI layer)

### NFRs Addressed
- **NFR2:** Overview renders within 500ms (API performance from Story 5.1 + client rendering)
- **NFR22:** WCAG 2.1 AA compliance (keyboard accessible, ARIA labels)
- **NFR27:** Status indicators use shape+icon, not color only (stale = clock icon + amber)

## Tasks / Subtasks

- [ ] Task 1: Frontend -- API types and client module for overview endpoint (AC: #1, #8)
  - [ ] 1.1 Add `RecruitmentOverview` and `StepOverview` types to `web/src/lib/api/recruitments.types.ts`
  - [ ] 1.2 Add `getOverview(id: string)` method to `web/src/lib/api/recruitments.ts`
  - [ ] 1.3 Verify type alignment with Story 5.1's `RecruitmentOverviewDto` (see Dev Notes for expected shape)

- [ ] Task 2: Frontend -- useRecruitmentOverview hook (AC: #1, #8)
  - [ ] 2.1 Create `web/src/features/overview/hooks/useRecruitmentOverview.ts`
  - [ ] 2.2 Hook accepts `recruitmentId: string`
  - [ ] 2.3 Query key: `['recruitment', recruitmentId, 'overview']`
  - [ ] 2.4 Consumes `recruitmentApi.getOverview(recruitmentId)`
  - [ ] 2.5 Create `web/src/features/overview/hooks/useRecruitmentOverview.test.ts`
  - [ ] 2.6 Test: "should fetch overview data for recruitment"
  - [ ] 2.7 Test: "should return loading state initially"
  - [ ] 2.8 Test: "should return error state on API failure"

- [ ] Task 3: Frontend -- OverviewDashboard.tsx collapsible container (AC: #1, #3, #4, #9)
  - [ ] 3.1 Create `web/src/features/overview/OverviewDashboard.tsx`
  - [ ] 3.2 Use shadcn/ui `Collapsible` component (`CollapsibleTrigger`, `CollapsibleContent`)
  - [ ] 3.3 Read collapse state from `localStorage` key `overview-collapsed:{recruitmentId}` on mount
  - [ ] 3.4 Write collapse state to `localStorage` on toggle
  - [ ] 3.5 When collapsed: render inline summary bar with text format "X candidates - Y screened - Z stale"
  - [ ] 3.6 When expanded: render KPI cards row + pipeline breakdown (StepSummaryCard list)
  - [ ] 3.7 Show `SkeletonLoader` (card variant, 3 items) during initial load
  - [ ] 3.8 Accept `onStepFilter` and `onStaleFilter` callback props for click-to-filter (AC6, AC7)
  - [ ] 3.9 Create `web/src/features/overview/OverviewDashboard.test.tsx`
  - [ ] 3.10 Test: "should render expanded by default when no localStorage value exists"
  - [ ] 3.11 Test: "should render collapsed when localStorage indicates collapsed"
  - [ ] 3.12 Test: "should persist collapse state to localStorage on toggle"
  - [ ] 3.13 Test: "should display KPI cards with correct values when expanded"
  - [ ] 3.14 Test: "should display inline summary when collapsed"
  - [ ] 3.15 Test: "should show skeleton loading state"
  - [ ] 3.16 Test: "should render empty state when overview returns zero candidates"

- [ ] Task 4: Frontend -- KpiCard sub-component (AC: #2, #9)
  - [ ] 4.1 Create `web/src/features/overview/KpiCard.tsx` (internal to overview feature, not shared)
  - [ ] 4.2 Props: `label: string`, `value: number`, `variant?: 'default' | 'warning'`
  - [ ] 4.3 Number uses `text-2xl font-bold` (24px) type scale
  - [ ] 4.4 Warning variant uses amber color for stale count
  - [ ] 4.5 Uses `aria-label` for accessibility (e.g., "Total candidates: 130")
  - [ ] 4.6 Test: "should render label and value"
  - [ ] 4.7 Test: "should apply warning styles for stale count"
  - [ ] 4.8 Test: "should have accessible aria-label"

- [ ] Task 5: Frontend -- StepSummaryCard.tsx per-step display (AC: #2, #5, #6, #7)
  - [ ] 5.1 Create `web/src/features/overview/StepSummaryCard.tsx`
  - [ ] 5.2 Props: `step: StepOverview`, `totalCandidates: number`, `staleDays: number`, `onStepFilter: (stepId: string) => void`, `onStaleFilter: (stepId: string) => void`
  - [ ] 5.3 Display step name, candidate count, and proportional width bar segment
  - [ ] 5.4 Show stale indicator when `step.staleCount > 0`: clock icon (`Clock` from lucide-react) + amber color + count text "N candidates > X days"
  - [ ] 5.5 Stale indicator uses `StatusBadge` with `status="stale"` for the icon+color (NFR27: not color only)
  - [ ] 5.6 Step name/count is clickable: calls `onStepFilter(step.stepId)` on click
  - [ ] 5.7 Stale indicator is clickable: calls `onStaleFilter(step.stepId)` on click
  - [ ] 5.8 Clickable elements have `cursor-pointer`, `hover:bg-surface-hover`, and `role="button"` + `aria-label`
  - [ ] 5.9 Create `web/src/features/overview/StepSummaryCard.test.tsx`
  - [ ] 5.10 Test: "should render step name and candidate count"
  - [ ] 5.11 Test: "should show proportional width bar segment"
  - [ ] 5.12 Test: "should show stale indicator with clock icon when staleCount > 0"
  - [ ] 5.13 Test: "should not show stale indicator when staleCount is 0"
  - [ ] 5.14 Test: "should call onStepFilter when step name is clicked"
  - [ ] 5.15 Test: "should call onStaleFilter when stale indicator is clicked"
  - [ ] 5.16 Test: "should have accessible labels on clickable elements"

- [ ] Task 6: Frontend -- PendingActionsPanel.tsx (AC: #2)
  - [ ] 6.1 Create `web/src/features/overview/PendingActionsPanel.tsx`
  - [ ] 6.2 Displays pending action count as a KPI card within the overview
  - [ ] 6.3 Shows the count of candidates with no outcome at their current step
  - [ ] 6.4 This is a simple display component (pending actions detail/list is deferred to Growth)
  - [ ] 6.5 Test: "should render pending action count"

- [ ] Task 7: Frontend -- Cache invalidation for overview refresh (AC: #8)
  - [ ] 7.1 Modify `web/src/features/screening/hooks/useRecordOutcome.ts` to add overview invalidation
  - [ ] 7.2 Add `queryClient.invalidateQueries({ queryKey: ['recruitment', variables.recruitmentId, 'overview'] })` to `onSuccess`
  - [ ] 7.3 Test: "should invalidate overview query when outcome is recorded"

- [ ] Task 8: Frontend -- Integration with recruitment page and candidate list filtering (AC: #1, #6, #7)
  - [ ] 8.1 Add `OverviewDashboard` to the recruitment detail page (above the candidate list)
  - [ ] 8.2 Wire `onStepFilter` callback to update `useCandidates` hook's `stepId` parameter (existing param)
  - [ ] 8.3 Wire `onStaleFilter` callback to set both `stepId` and `staleOnly: true` on `useCandidates` hook (staleOnly param added by Story 5.1 Task 7)
  - [ ] 8.4 Show active filter indicator (pill/badge) with clear button when a filter is active
  - [ ] 8.5 Clearing the filter resets the candidate list to show all candidates
  - [ ] 8.6 Ensure filter changes do not cause a page transition (client-side state only)
  - [ ] 8.7 Test: "should filter candidate list when step is clicked in overview"
  - [ ] 8.8 Test: "should filter to stale candidates when stale indicator is clicked"
  - [ ] 8.9 Test: "should show active filter indicator with clear button"
  - [ ] 8.10 Test: "should clear filter and show all candidates when clear button is clicked"

## Dev Notes

### Affected Aggregate(s)

**No backend changes in this story.** Story 5.2 is purely frontend -- it consumes the API endpoint defined in Story 5.1 (`GET /api/recruitments/{id}/overview`). The Recruitment aggregate is read-only from this story's perspective.

### API Response Shape (RecruitmentOverviewDto)

**Aligned with Story 5.1's DTO contract.** The backend DTO is authoritative. The following TypeScript types match the JSON response from `GET /api/recruitments/{id}/overview` (camelCase via System.Text.Json):

```typescript
// Add to web/src/lib/api/recruitments.types.ts

interface RecruitmentOverview {
  recruitmentId: string
  totalCandidates: number
  pendingActionCount: number   // Sum of all per-step pendingCount values
  totalStale: number           // Sum of all per-step staleCount values
  staleDays: number            // From deployment config (default: 5)
  steps: StepOverview[]        // Sorted by stepOrder
}

interface StepOverview {
  stepId: string
  stepName: string
  stepOrder: number
  totalCandidates: number      // Total candidates at this step
  pendingCount: number         // Candidates with no outcome at this step
  staleCount: number           // Candidates exceeding threshold at this step
  outcomeBreakdown: OutcomeBreakdown
}

interface OutcomeBreakdown {
  notStarted: number
  pass: number
  fail: number
  hold: number
}
```

[Source: `_bmad-output/implementation-artifacts/5-1-overview-api-data.md` -- DTO Shape (Contract for Story 5.2)]

**Key DTO design decisions (from Story 5.1):**
- `staleDays` at root level so the frontend can display "candidates > 5 days" without knowing server config
- `outcomeBreakdown` uses flat fields (not a dictionary) for type safety
- `pendingCount` per step = candidates at that step with no outcome (i.e., `notStarted` at current step)
- `pendingActionCount` at root = sum of all per-step `pendingCount` values
- `totalStale` at root = sum of all per-step `staleCount` values
- Steps are always sorted by `stepOrder` in the response

### TanStack Query Patterns

**Query key:** `['recruitment', recruitmentId, 'overview']`

This follows the existing codebase convention where recruitment-scoped queries use `['recruitment', id, ...]` (see `useRecruitmentMutations.ts` invalidation patterns). [Source: `web/src/features/recruitments/hooks/useRecruitmentMutations.ts`]

**Cache invalidation on outcome recording:**

`useRecordOutcome.ts` already invalidates `['screening', 'history', candidateId]` and `['candidates', recruitmentId]` on success. Story 5.2 adds a third invalidation: `['recruitment', recruitmentId, 'overview']`. This triggers a background refetch of overview data (no loading spinner shown) per the "Background refresh" pattern in `patterns-frontend.md`. [Source: `web/src/features/screening/hooks/useRecordOutcome.ts`]

**Independent loading (FR50):** The overview query and the candidate list query use different query keys and load independently. Neither blocks the other. The overview can show skeleton loading while the candidate list renders (and vice versa).

### localStorage Key Convention

Key: `overview-collapsed:{recruitmentId}`
Value: `"true"` | `"false"` (string, not boolean -- localStorage stores strings)
Default: expanded (when key doesn't exist)

Per-recruitment collapse state allows different recruitments to have different collapse preferences.

### Collapsible Component Pattern

Use the shadcn/ui `Collapsible` component already installed at `web/src/components/ui/collapsible.tsx`:

```tsx
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'

<Collapsible
  open={isOpen}
  onOpenChange={(open) => {
    setIsOpen(open)
    localStorage.setItem(`overview-collapsed:${recruitmentId}`, String(!open))
  }}
>
  <CollapsibleTrigger asChild>
    <button aria-label={isOpen ? 'Collapse overview' : 'Expand overview'}>
      {/* Chevron icon */}
    </button>
  </CollapsibleTrigger>

  {/* Collapsed summary bar (visible when closed) */}
  {!isOpen && <InlineSummaryBar />}

  <CollapsibleContent>
    {/* KPI cards + pipeline breakdown */}
  </CollapsibleContent>
</Collapsible>
```

[Source: `web/src/components/ui/collapsible.tsx`]

### Click-to-Filter Pattern (AC6, AC7)

The overview communicates filter selections to the candidate list via callback props. The recruitment detail page (integration point) manages filter state:

```typescript
// In the recruitment detail page
const [activeStepId, setActiveStepId] = useState<string | undefined>()
const [staleOnly, setStaleOnly] = useState(false)

// Pass to OverviewDashboard
<OverviewDashboard
  recruitmentId={recruitmentId}
  onStepFilter={(stepId) => { setStaleOnly(false); setActiveStepId(stepId); }}
  onStaleFilter={(stepId) => { setActiveStepId(stepId); setStaleOnly(true); }}
/>

// Pass to candidate list hook (stepId and staleOnly are existing params on useCandidates)
const { data: candidates } = useCandidates({
  recruitmentId,
  stepId: activeStepId,
  staleOnly: staleOnly || undefined,
})
```

**Active filter indicator:** When `activeStepId` is set or `staleOnly` is true, show a filter badge above the candidate list with the step name (and "stale" label if applicable) and a clear ("X") button. The badge uses the shared `StatusBadge` component style for consistency.

**Note:** The `staleOnly` param on `useCandidates` is added by Story 5.1 Task 7 — it extends the existing `GetCandidatesQuery` with stale filtering support.

**Cross-feature communication:** The overview and candidate list are in different feature folders (`features/overview/` and `features/candidates/`). They do NOT import from each other. The recruitment detail page in `routes/` coordinates them via props/state. [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- Component Structure, Feature isolation]

### Inline Summary Bar Format (AC3)

When collapsed, show: `"{totalCandidates} candidates - {screenedCount} screened - {totalStale} stale"`

Where `screenedCount` = `totalCandidates - pendingActionCount` (candidates who have at least one outcome recorded).

### Visual Design Requirements (AC9)

**KPI card styling:**
- Number: `text-2xl font-bold` (24px bold) -- `text-brand-brown-900` for default, `text-amber-600` for stale count
- Label: `text-sm text-brand-brown-600`
- Card background: `bg-brand-cream-50` (If Insurance brand cream)
- Card border: `border border-brand-cream-200`
- Card padding: `p-4`

**Pipeline bar segments:**
- Each step gets a proportional-width segment: `width = (step.totalCount / totalCandidates) * 100%`
- Segments use brand-compatible colors (warm palette: browns, ambers, greens)
- Minimum segment width of 2% for visibility when counts are low

**Brand palette references:**
- Cream backgrounds: Tailwind custom color `brand-cream-50` (defined in project theme)
- Warm browns: `brand-brown-600`, `brand-brown-900`
- Stale indicator: amber (`text-amber-600`, `bg-amber-50`, `border-amber-200`)

If custom brand colors are not yet defined in the Tailwind config, use the closest standard Tailwind equivalents:
- Cream: `bg-orange-50` or `bg-amber-50`
- Warm brown: `text-stone-700`, `text-stone-900`

### StatusBadge Usage

The `StatusBadge` component supports a `stale` variant (type: `StatusVariant = 'pass' | 'fail' | 'hold' | 'stale' | 'not-started'`). Use `<StatusBadge status="stale" />` for stale indicators. This renders a clock icon + amber color, satisfying NFR27 (not color only). [Source: `web/src/components/StatusBadge.types.ts`]

### Empty State

When a recruitment has zero candidates, the overview should still render all workflow steps with zero counts. Show a zero-state message in the KPI area: "No candidates imported yet." This is NOT a full `EmptyState` component -- the overview just shows zeros since the empty state for the page itself is handled by the candidate list area.

### Accessibility Requirements

- `CollapsibleTrigger` has `aria-label="Collapse overview"` / `"Expand overview"`
- KPI cards have `aria-label` combining label and value (e.g., "Total candidates: 130")
- Clickable step names have `role="button"` and `aria-label="Filter by step: Screening"`
- Clickable stale indicators have `role="button"` and `aria-label="Show stale candidates at step: Screening"`
- Active filter badge is announced: `aria-live="polite"` on the filter area
- All interactive elements are keyboard-reachable via Tab
- Focus indicators: `focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2`

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (API types + client) | **Characterization** | Thin type definitions + one-line API call, no business logic |
| Task 2 (useRecruitmentOverview) | **Test-first** | TanStack Query hook with specific query key -- verify key structure and data flow |
| Task 3 (OverviewDashboard) | **Test-first** | Collapsible state persistence, conditional rendering, and callback wiring are behavior-critical |
| Task 4 (KpiCard) | **Characterization** | Simple presentational component, render verification |
| Task 5 (StepSummaryCard) | **Test-first** | Click-to-filter, stale indicator logic, and proportional bar rendering are user-facing contracts |
| Task 6 (PendingActionsPanel) | **Characterization** | Simple count display, no logic |
| Task 7 (Cache invalidation) | **Test-first** | Verify the overview query is invalidated on outcome -- regression-critical |
| Task 8 (Integration) | **Test-first** | Filter state coordination between overview and candidate list -- integration behavior must be verified |

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| React | 19.x | `useState` for filter/collapse state, `useEffect` for localStorage read on mount |
| TypeScript | 5.7.x | Strict mode. Interface definitions for API types. |
| TanStack Query | Latest | `useQuery` for overview data fetch. Query key convention: `['recruitment', id, 'overview']`. |
| Tailwind CSS | 4.x | `text-2xl font-bold` for KPI numbers. Brand palette colors. `focus-visible:outline-*` for focus indicators. |
| shadcn/ui | Installed | `Collapsible`, `CollapsibleTrigger`, `CollapsibleContent` from `@/components/ui/collapsible` |
| lucide-react | Installed | `Clock` icon for stale indicator, `ChevronDown`/`ChevronUp` for collapse toggle, `X` for filter clear |

### Project Structure Notes

**New files to create:**
```
web/src/features/overview/
  OverviewDashboard.tsx
  OverviewDashboard.test.tsx
  KpiCard.tsx
  KpiCard.test.tsx
  StepSummaryCard.tsx
  StepSummaryCard.test.tsx
  PendingActionsPanel.tsx
  PendingActionsPanel.test.tsx
  hooks/
    useRecruitmentOverview.ts
    useRecruitmentOverview.test.ts
```

[Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- `web/src/features/overview/` folder structure]

**Existing files to modify:**
```
web/src/lib/api/recruitments.ts                              -- Add getOverview method
web/src/lib/api/recruitments.types.ts                        -- Add RecruitmentOverview + StepOverview types
web/src/features/screening/hooks/useRecordOutcome.ts         -- Add overview cache invalidation
```

**Integration file (route/page level):**
The recruitment detail page (location depends on existing routing structure -- likely in `web/src/routes/` or within `features/recruitments/`) needs to import `OverviewDashboard` and wire filter state. Identify the exact file during implementation.

**Files consumed from prerequisite stories (NOT modified):**
```
web/src/components/StatusBadge.tsx                           -- Story 1.4 (stale variant)
web/src/components/SkeletonLoader.tsx                        -- Story 1.4 (card variant)
web/src/components/ui/collapsible.tsx                        -- shadcn/ui (installed)
web/src/features/candidates/hooks/useCandidates.ts           -- Story 4.1 (stepFilter param)
```

**MSW mock handlers:**
Add overview mock handler to `web/src/mocks/handlers.ts`:
```typescript
http.get('/api/recruitments/:id/overview', () => {
  return HttpResponse.json(mockOverviewData)
})
```

Add overview fixture to `web/src/mocks/fixtures/` (new file or extend `recruitments.ts`).

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-5-recruitment-overview-monitoring.md` -- Story 5.2 acceptance criteria, FR47-50, NFR2/22/27]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Overview data strategy (computed on read, GROUP BY, sub-100ms), Aggregate Boundaries (Recruitment read-only)]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Enforcement Guidelines (#3 use shared components, #6 empty state handling)]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- TanStack Query Keys (all params in queryKey), Loading States (skeleton, background refresh), UI Consistency Rules (StatusBadge), Empty State Pattern]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- API Client Contract Pattern (recruitmentApi.getOverview), State Management (TanStack Query for server state)]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- `features/overview/` folder structure, `lib/api/recruitments.ts` for API client]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Frontend test naming ("should ... when ..."), Vitest + Testing Library, MSW for API mocking]
- [Source: `web/src/components/StatusBadge.types.ts` -- StatusVariant type includes 'stale']
- [Source: `web/src/features/screening/hooks/useRecordOutcome.ts` -- Existing cache invalidation pattern for screening]
- [Source: `web/src/lib/api/recruitments.ts` -- Existing API client module to extend]
- [Source: `web/src/lib/api/recruitments.types.ts` -- Existing type definitions to extend]
- [Source: `web/src/components/ui/collapsible.tsx` -- shadcn/ui Collapsible component (installed)]
- [Source: `_bmad-output/implementation-artifacts/4-5-keyboard-navigation-screening-flow.md` -- Story structure pattern, dev notes format, AC coverage table]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy, mode declarations]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

### Completion Notes List

- All 8 story tasks implemented with TDD (test-first for hooks/containers/integration, characterization for presentational components)
- 26 new tests added (311 total, up from 285)
- TypeScript compilation: 0 errors
- Backend build: 0 warnings, 0 errors
- Anti-pattern scan: 0 violations
- CandidateList extended with controlled external filter props (externalStepFilter, externalStaleOnly, onClearExternalFilters) to enable click-to-filter from overview without breaking existing internal filter behavior
- Brand colors: used closest Tailwind equivalents (amber-50 for cream, stone-700/900 for warm browns) since custom brand tokens not yet defined
- Test file for useRecruitmentOverview hook uses .tsx extension (contains JSX wrapper)

### File List

**New files created:**
- `web/src/features/overview/hooks/useRecruitmentOverview.ts` -- TanStack Query hook
- `web/src/features/overview/hooks/useRecruitmentOverview.test.tsx` -- 3 tests
- `web/src/features/overview/KpiCard.tsx` -- Presentational KPI card
- `web/src/features/overview/KpiCard.test.tsx` -- 3 tests
- `web/src/features/overview/StepSummaryCard.tsx` -- Per-step display with click-to-filter
- `web/src/features/overview/StepSummaryCard.test.tsx` -- 7 tests
- `web/src/features/overview/PendingActionsPanel.tsx` -- Pending actions count
- `web/src/features/overview/PendingActionsPanel.test.tsx` -- 1 test
- `web/src/features/overview/OverviewDashboard.tsx` -- Collapsible container
- `web/src/features/overview/OverviewDashboard.test.tsx` -- 7 tests
- `web/src/features/screening/hooks/useRecordOutcome.test.tsx` -- 1 test
- `docs/plans/2026-02-14-overview-dashboard-ui.md` -- Implementation plan

**Existing files modified:**
- `web/src/lib/api/recruitments.types.ts` -- Added RecruitmentOverview, StepOverview, OutcomeBreakdown types
- `web/src/lib/api/recruitments.ts` -- Added getOverview method
- `web/src/mocks/recruitmentHandlers.ts` -- Added overview MSW mock handler + fixture data
- `web/src/features/screening/hooks/useRecordOutcome.ts` -- Added overview cache invalidation
- `web/src/features/candidates/CandidateList.tsx` -- Added external filter props for overview integration
- `web/src/features/recruitments/pages/RecruitmentPage.tsx` -- Added OverviewDashboard + filter state coordination
- `web/src/features/recruitments/pages/RecruitmentPage.test.tsx` -- Added 4 integration tests + AuthProvider wrapper
