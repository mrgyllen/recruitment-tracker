# Overview Dashboard UI Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a collapsible overview dashboard at the top of the recruitment page showing KPI cards, per-step pipeline breakdown with stale indicators, and click-to-filter integration with the candidate list.

**Architecture:** The overview dashboard is a pure frontend feature consuming the `GET /api/recruitments/{id}/overview` endpoint from Story 5.1. It lives in `features/overview/` and communicates with the candidate list via callback props coordinated by the RecruitmentPage. Filter state is lifted from CandidateList to RecruitmentPage so both overview and candidate list can share it. The shadcn/ui Collapsible component provides expand/collapse with localStorage persistence.

**Tech Stack:** React 19, TypeScript (strict), TanStack Query, Tailwind CSS v4, shadcn/ui Collapsible, lucide-react icons, Vitest + Testing Library + MSW

---

## Key Design Decisions

- **CandidateList filter state lift:** Currently `CandidateList` manages `stepFilter` and `outcomeFilter` internally via `useState`. To support click-to-filter from the overview, we must add optional controlled props (`externalStepFilter`, `externalStaleOnly`, `onClearExternalFilters`) so the RecruitmentPage can coordinate. CandidateList keeps its own internal filters (search, outcome, page) but defers to external step/stale filters when provided.
- **No new shared components:** KpiCard, StepSummaryCard, PendingActionsPanel are internal to `features/overview/`. StatusBadge (stale variant) and SkeletonLoader (card variant) are reused from shared components.
- **Brand colors:** Use closest Tailwind equivalents since custom brand tokens may not exist: `bg-amber-50` for cream, `text-stone-700`/`text-stone-900` for warm browns, `text-amber-600` for stale.
- **MSW handler:** Add overview mock to `recruitmentHandlers.ts` (not a separate file) since it's a recruitment-scoped endpoint.

## Prerequisite Verification

Before starting, confirm these exist:
- `web/src/components/ui/collapsible.tsx` -- shadcn/ui Collapsible (installed)
- `web/src/components/StatusBadge.tsx` -- with `stale` variant
- `web/src/components/SkeletonLoader.tsx` -- with `card` variant
- `web/src/features/candidates/hooks/useCandidates.ts` -- with `staleOnly` param
- `web/src/mocks/recruitmentHandlers.ts` -- existing mock handlers
- `web/src/test-utils.tsx` -- custom render with providers

---

### Task 1: API Types + Client Module + MSW Mock

**Mode:** Characterization (thin types + one-line API call, no business logic)

**Files:**
- Modify: `web/src/lib/api/recruitments.types.ts`
- Modify: `web/src/lib/api/recruitments.ts`
- Modify: `web/src/mocks/recruitmentHandlers.ts`

**Step 1: Add TypeScript types to `recruitments.types.ts`**

Append after the existing `ReorderStepsRequest` interface:

```typescript
export interface OutcomeBreakdown {
  notStarted: number
  pass: number
  fail: number
  hold: number
}

export interface StepOverview {
  stepId: string
  stepName: string
  stepOrder: number
  totalCandidates: number
  pendingCount: number
  staleCount: number
  outcomeBreakdown: OutcomeBreakdown
}

export interface RecruitmentOverview {
  recruitmentId: string
  totalCandidates: number
  pendingActionCount: number
  totalStale: number
  staleDays: number
  steps: StepOverview[]
}
```

These match the backend `RecruitmentOverviewDto` (camelCase via System.Text.Json).

**Step 2: Add `getOverview` method to `recruitments.ts`**

Add to the `recruitmentApi` object and update the import:

```typescript
import type {
  // ... existing imports ...
  RecruitmentOverview,
} from './recruitments.types'

// Add to recruitmentApi object:
  getOverview: (id: string) =>
    apiGet<RecruitmentOverview>(`/recruitments/${id}/overview`),
```

**Step 3: Add MSW mock handler to `recruitmentHandlers.ts`**

Add mock overview data and handler. Import `RecruitmentOverview` type. Add handler to the `recruitmentHandlers` array:

```typescript
import type {
  PaginatedRecruitmentList,
  RecruitmentDetail,
  RecruitmentOverview,
  WorkflowStepDto,
} from '@/lib/api/recruitments.types'

// Add after recruitmentsById:
export const mockOverviewData: RecruitmentOverview = {
  recruitmentId: mockRecruitmentId,
  totalCandidates: 130,
  pendingActionCount: 47,
  totalStale: 3,
  staleDays: 5,
  steps: [
    {
      stepId: 'step-1',
      stepName: 'Screening',
      stepOrder: 1,
      totalCandidates: 80,
      pendingCount: 30,
      staleCount: 2,
      outcomeBreakdown: { notStarted: 30, pass: 35, fail: 10, hold: 5 },
    },
    {
      stepId: 'step-2',
      stepName: 'Technical Test',
      stepOrder: 2,
      totalCandidates: 35,
      pendingCount: 12,
      staleCount: 1,
      outcomeBreakdown: { notStarted: 12, pass: 15, fail: 5, hold: 3 },
    },
    {
      stepId: 'step-3',
      stepName: 'Technical Interview',
      stepOrder: 3,
      totalCandidates: 15,
      pendingCount: 5,
      staleCount: 0,
      outcomeBreakdown: { notStarted: 5, pass: 8, fail: 2, hold: 0 },
    },
  ],
}

// Add to recruitmentHandlers array:
  http.get('/api/recruitments/:id/overview', ({ params }) => {
    const { id } = params
    if (id === forbiddenRecruitmentId) {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.3', title: 'Forbidden', status: 403 },
        { status: 403 },
      )
    }
    if (!recruitmentsById[id as string]) {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }
    return HttpResponse.json({ ...mockOverviewData, recruitmentId: id })
  }),
```

**Step 4: Run tests to verify nothing is broken**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run --reporter=verbose 2>&1 | tail -20`
Expected: All existing tests pass (285+)

**Step 5: Commit**

```bash
git add web/src/lib/api/recruitments.types.ts web/src/lib/api/recruitments.ts web/src/mocks/recruitmentHandlers.ts
git commit -m "feat(overview): add API types, client method, and MSW mock for overview endpoint"
```

---

### Task 2: useRecruitmentOverview Hook + Tests

**Mode:** Test-first (TanStack Query hook with specific query key)

**Files:**
- Create: `web/src/features/overview/hooks/useRecruitmentOverview.ts`
- Create: `web/src/features/overview/hooks/useRecruitmentOverview.test.ts`

**Step 1: Write the failing tests**

Create `web/src/features/overview/hooks/useRecruitmentOverview.test.ts`:

```typescript
import { renderHook, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { useRecruitmentOverview } from './useRecruitmentOverview'
import { mockRecruitmentId, mockOverviewData } from '@/mocks/recruitmentHandlers'
import { server } from '@/mocks/server'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    )
  }
}

describe('useRecruitmentOverview', () => {
  it('should fetch overview data for recruitment', async () => {
    const { result } = renderHook(
      () => useRecruitmentOverview(mockRecruitmentId),
      { wrapper: createWrapper() },
    )

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.totalCandidates).toBe(mockOverviewData.totalCandidates)
    expect(result.current.data?.steps).toHaveLength(3)
    expect(result.current.data?.pendingActionCount).toBe(mockOverviewData.pendingActionCount)
  })

  it('should return loading state initially', () => {
    const { result } = renderHook(
      () => useRecruitmentOverview(mockRecruitmentId),
      { wrapper: createWrapper() },
    )

    expect(result.current.isPending).toBe(true)
    expect(result.current.data).toBeUndefined()
  })

  it('should return error state on API failure', async () => {
    server.use(
      http.get('/api/recruitments/:id/overview', () => {
        return HttpResponse.json(
          { title: 'Internal Server Error', status: 500 },
          { status: 500 },
        )
      }),
    )

    const { result } = renderHook(
      () => useRecruitmentOverview(mockRecruitmentId),
      { wrapper: createWrapper() },
    )

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/hooks/useRecruitmentOverview.test.ts --reporter=verbose 2>&1 | tail -20`
Expected: FAIL (module not found)

**Step 3: Implement the hook**

Create `web/src/features/overview/hooks/useRecruitmentOverview.ts`:

```typescript
import { useQuery } from '@tanstack/react-query'
import { recruitmentApi } from '@/lib/api/recruitments'

export function useRecruitmentOverview(recruitmentId: string) {
  return useQuery({
    queryKey: ['recruitment', recruitmentId, 'overview'],
    queryFn: () => recruitmentApi.getOverview(recruitmentId),
    enabled: !!recruitmentId,
  })
}
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/hooks/useRecruitmentOverview.test.ts --reporter=verbose 2>&1 | tail -20`
Expected: 3 tests PASS

**Step 5: Commit**

```bash
git add web/src/features/overview/hooks/useRecruitmentOverview.ts web/src/features/overview/hooks/useRecruitmentOverview.test.ts
git commit -m "feat(overview): add useRecruitmentOverview hook with tests"
```

---

### Task 3: KpiCard Sub-component + Tests

**Mode:** Characterization (simple presentational component)

**Files:**
- Create: `web/src/features/overview/KpiCard.tsx`
- Create: `web/src/features/overview/KpiCard.test.tsx`

**Step 1: Write the tests**

Create `web/src/features/overview/KpiCard.test.tsx`:

```typescript
import { render, screen } from '@/test-utils'
import { describe, expect, it } from 'vitest'
import { KpiCard } from './KpiCard'

describe('KpiCard', () => {
  it('should render label and value', () => {
    render(<KpiCard label="Total Candidates" value={130} />)

    expect(screen.getByText('130')).toBeInTheDocument()
    expect(screen.getByText('Total Candidates')).toBeInTheDocument()
  })

  it('should apply warning styles for stale count', () => {
    render(<KpiCard label="Stale" value={3} variant="warning" />)

    const valueElement = screen.getByText('3')
    expect(valueElement).toHaveClass('text-amber-600')
  })

  it('should have accessible aria-label', () => {
    render(<KpiCard label="Total Candidates" value={130} />)

    expect(
      screen.getByLabelText('Total Candidates: 130'),
    ).toBeInTheDocument()
  })
})
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/KpiCard.test.tsx --reporter=verbose 2>&1 | tail -20`
Expected: FAIL (module not found)

**Step 3: Implement KpiCard**

Create `web/src/features/overview/KpiCard.tsx`:

```tsx
import { cn } from '@/lib/utils'

interface KpiCardProps {
  label: string
  value: number
  variant?: 'default' | 'warning'
}

export function KpiCard({ label, value, variant = 'default' }: KpiCardProps) {
  return (
    <div
      className="rounded-md border border-stone-200 bg-amber-50 p-4"
      aria-label={`${label}: ${value}`}
    >
      <p
        className={cn(
          'text-2xl font-bold',
          variant === 'warning' ? 'text-amber-600' : 'text-stone-900',
        )}
      >
        {value}
      </p>
      <p className="text-sm text-stone-600">{label}</p>
    </div>
  )
}
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/KpiCard.test.tsx --reporter=verbose 2>&1 | tail -20`
Expected: 3 tests PASS

**Step 5: Commit**

```bash
git add web/src/features/overview/KpiCard.tsx web/src/features/overview/KpiCard.test.tsx
git commit -m "feat(overview): add KpiCard sub-component with tests"
```

---

### Task 4: StepSummaryCard + Tests

**Mode:** Test-first (click-to-filter, stale indicator logic, proportional bar)

**Files:**
- Create: `web/src/features/overview/StepSummaryCard.tsx`
- Create: `web/src/features/overview/StepSummaryCard.test.tsx`

**Step 1: Write the failing tests**

Create `web/src/features/overview/StepSummaryCard.test.tsx`:

```typescript
import { render, screen } from '@/test-utils'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { StepSummaryCard } from './StepSummaryCard'
import type { StepOverview } from '@/lib/api/recruitments.types'

const mockStep: StepOverview = {
  stepId: 'step-1',
  stepName: 'Screening',
  stepOrder: 1,
  totalCandidates: 80,
  pendingCount: 30,
  staleCount: 2,
  outcomeBreakdown: { notStarted: 30, pass: 35, fail: 10, hold: 5 },
}

const noStaleStep: StepOverview = {
  ...mockStep,
  staleCount: 0,
}

describe('StepSummaryCard', () => {
  it('should render step name and candidate count', () => {
    render(
      <StepSummaryCard
        step={mockStep}
        totalCandidates={130}
        staleDays={5}
        onStepFilter={vi.fn()}
        onStaleFilter={vi.fn()}
      />,
    )

    expect(screen.getByText('Screening')).toBeInTheDocument()
    expect(screen.getByText('80')).toBeInTheDocument()
  })

  it('should show proportional width bar segment', () => {
    const { container } = render(
      <StepSummaryCard
        step={mockStep}
        totalCandidates={130}
        staleDays={5}
        onStepFilter={vi.fn()}
        onStaleFilter={vi.fn()}
      />,
    )

    const bar = container.querySelector('[data-testid="step-bar"]')
    expect(bar).toBeInTheDocument()
    // 80/130 = ~61.5%, minimum 2%
    expect(bar).toHaveStyle({ width: expect.stringContaining('%') })
  })

  it('should show stale indicator with clock icon when staleCount > 0', () => {
    render(
      <StepSummaryCard
        step={mockStep}
        totalCandidates={130}
        staleDays={5}
        onStepFilter={vi.fn()}
        onStaleFilter={vi.fn()}
      />,
    )

    expect(screen.getByText(/2 candidates > 5 days/)).toBeInTheDocument()
  })

  it('should not show stale indicator when staleCount is 0', () => {
    render(
      <StepSummaryCard
        step={noStaleStep}
        totalCandidates={130}
        staleDays={5}
        onStepFilter={vi.fn()}
        onStaleFilter={vi.fn()}
      />,
    )

    expect(screen.queryByText(/candidates > 5 days/)).not.toBeInTheDocument()
  })

  it('should call onStepFilter when step name is clicked', async () => {
    const user = userEvent.setup()
    const onStepFilter = vi.fn()

    render(
      <StepSummaryCard
        step={mockStep}
        totalCandidates={130}
        staleDays={5}
        onStepFilter={onStepFilter}
        onStaleFilter={vi.fn()}
      />,
    )

    await user.click(screen.getByRole('button', { name: /filter by step: screening/i }))
    expect(onStepFilter).toHaveBeenCalledWith('step-1')
  })

  it('should call onStaleFilter when stale indicator is clicked', async () => {
    const user = userEvent.setup()
    const onStaleFilter = vi.fn()

    render(
      <StepSummaryCard
        step={mockStep}
        totalCandidates={130}
        staleDays={5}
        onStepFilter={vi.fn()}
        onStaleFilter={onStaleFilter}
      />,
    )

    await user.click(
      screen.getByRole('button', { name: /show stale candidates at step: screening/i }),
    )
    expect(onStaleFilter).toHaveBeenCalledWith('step-1')
  })

  it('should have accessible labels on clickable elements', () => {
    render(
      <StepSummaryCard
        step={mockStep}
        totalCandidates={130}
        staleDays={5}
        onStepFilter={vi.fn()}
        onStaleFilter={vi.fn()}
      />,
    )

    expect(
      screen.getByRole('button', { name: /filter by step: screening/i }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /show stale candidates at step: screening/i }),
    ).toBeInTheDocument()
  })
})
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/StepSummaryCard.test.tsx --reporter=verbose 2>&1 | tail -20`
Expected: FAIL (module not found)

**Step 3: Implement StepSummaryCard**

Create `web/src/features/overview/StepSummaryCard.tsx`:

```tsx
import { StatusBadge } from '@/components/StatusBadge'
import type { StepOverview } from '@/lib/api/recruitments.types'

interface StepSummaryCardProps {
  step: StepOverview
  totalCandidates: number
  staleDays: number
  onStepFilter: (stepId: string) => void
  onStaleFilter: (stepId: string) => void
}

export function StepSummaryCard({
  step,
  totalCandidates,
  staleDays,
  onStepFilter,
  onStaleFilter,
}: StepSummaryCardProps) {
  const barWidth = totalCandidates > 0
    ? Math.max(2, (step.totalCandidates / totalCandidates) * 100)
    : 0

  return (
    <div className="rounded-md border border-stone-200 bg-amber-50 p-3">
      <div className="flex items-center justify-between">
        <button
          role="button"
          aria-label={`Filter by step: ${step.stepName}`}
          className="cursor-pointer rounded px-1 text-left hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
          onClick={() => onStepFilter(step.stepId)}
        >
          <span className="font-medium text-stone-900">{step.stepName}</span>
        </button>
        <span className="text-lg font-bold text-stone-900">{step.totalCandidates}</span>
      </div>

      <div className="mt-2 h-2 w-full rounded-full bg-stone-200">
        <div
          data-testid="step-bar"
          className="h-2 rounded-full bg-stone-500"
          style={{ width: `${barWidth}%` }}
        />
      </div>

      {step.staleCount > 0 && (
        <button
          role="button"
          aria-label={`Show stale candidates at step: ${step.stepName}`}
          className="mt-2 inline-flex cursor-pointer items-center gap-1 rounded px-1 hover:bg-amber-100 focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
          onClick={() => onStaleFilter(step.stepId)}
        >
          <StatusBadge status="stale" />
          <span className="text-xs text-amber-600">
            {step.staleCount} candidates &gt; {staleDays} days
          </span>
        </button>
      )}
    </div>
  )
}
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/StepSummaryCard.test.tsx --reporter=verbose 2>&1 | tail -20`
Expected: 7 tests PASS

**Step 5: Commit**

```bash
git add web/src/features/overview/StepSummaryCard.tsx web/src/features/overview/StepSummaryCard.test.tsx
git commit -m "feat(overview): add StepSummaryCard with click-to-filter and stale indicators"
```

---

### Task 5: PendingActionsPanel + Test

**Mode:** Characterization (simple count display)

**Files:**
- Create: `web/src/features/overview/PendingActionsPanel.tsx`
- Create: `web/src/features/overview/PendingActionsPanel.test.tsx`

**Step 1: Write the test**

Create `web/src/features/overview/PendingActionsPanel.test.tsx`:

```typescript
import { render, screen } from '@/test-utils'
import { describe, expect, it } from 'vitest'
import { PendingActionsPanel } from './PendingActionsPanel'

describe('PendingActionsPanel', () => {
  it('should render pending action count', () => {
    render(<PendingActionsPanel count={47} />)

    expect(screen.getByText('47')).toBeInTheDocument()
    expect(screen.getByText('Pending Actions')).toBeInTheDocument()
    expect(screen.getByLabelText('Pending Actions: 47')).toBeInTheDocument()
  })
})
```

**Step 2: Implement PendingActionsPanel**

Create `web/src/features/overview/PendingActionsPanel.tsx`:

```tsx
import { KpiCard } from './KpiCard'

interface PendingActionsPanelProps {
  count: number
}

export function PendingActionsPanel({ count }: PendingActionsPanelProps) {
  return <KpiCard label="Pending Actions" value={count} />
}
```

**Step 3: Run tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/PendingActionsPanel.test.tsx --reporter=verbose 2>&1 | tail -20`
Expected: 1 test PASS

**Step 4: Commit**

```bash
git add web/src/features/overview/PendingActionsPanel.tsx web/src/features/overview/PendingActionsPanel.test.tsx
git commit -m "feat(overview): add PendingActionsPanel component with test"
```

---

### Task 6: OverviewDashboard Container + Tests

**Mode:** Test-first (collapsible state persistence, conditional rendering, callback wiring)

**Files:**
- Create: `web/src/features/overview/OverviewDashboard.tsx`
- Create: `web/src/features/overview/OverviewDashboard.test.tsx`

**Step 1: Write the failing tests**

Create `web/src/features/overview/OverviewDashboard.test.tsx`:

```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { OverviewDashboard } from './OverviewDashboard'
import { mockRecruitmentId, mockOverviewData } from '@/mocks/recruitmentHandlers'
import { server } from '@/mocks/server'
import { AuthProvider } from '@/features/auth/AuthContext'

function renderDashboard(
  props?: Partial<{
    recruitmentId: string
    onStepFilter: (stepId: string) => void
    onStaleFilter: (stepId: string) => void
  }>,
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter>
          <OverviewDashboard
            recruitmentId={props?.recruitmentId ?? mockRecruitmentId}
            onStepFilter={props?.onStepFilter ?? vi.fn()}
            onStaleFilter={props?.onStaleFilter ?? vi.fn()}
          />
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  )
}

describe('OverviewDashboard', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('should render expanded by default when no localStorage value exists', async () => {
    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Total Candidates')).toBeInTheDocument()
    })

    expect(screen.getByText(String(mockOverviewData.totalCandidates))).toBeInTheDocument()
  })

  it('should render collapsed when localStorage indicates collapsed', async () => {
    localStorage.setItem(`overview-collapsed:${mockRecruitmentId}`, 'true')

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText(/candidates/i)).toBeInTheDocument()
    })

    // Collapsed summary bar should be visible
    expect(screen.getByText(/130 candidates/)).toBeInTheDocument()
    // KPI labels should NOT be visible (collapsed)
    expect(screen.queryByText('Total Candidates')).not.toBeInTheDocument()
  })

  it('should persist collapse state to localStorage on toggle', async () => {
    const user = userEvent.setup()
    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Total Candidates')).toBeInTheDocument()
    })

    // Collapse
    await user.click(screen.getByLabelText('Collapse overview'))
    expect(localStorage.getItem(`overview-collapsed:${mockRecruitmentId}`)).toBe('true')

    // Expand
    await user.click(screen.getByLabelText('Expand overview'))
    expect(localStorage.getItem(`overview-collapsed:${mockRecruitmentId}`)).toBe('false')
  })

  it('should display KPI cards with correct values when expanded', async () => {
    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Total Candidates')).toBeInTheDocument()
    })

    expect(screen.getByLabelText('Total Candidates: 130')).toBeInTheDocument()
    expect(screen.getByLabelText('Pending Actions: 47')).toBeInTheDocument()
    expect(screen.getByLabelText('Stale Candidates: 3')).toBeInTheDocument()
  })

  it('should display inline summary when collapsed', async () => {
    const user = userEvent.setup()
    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Total Candidates')).toBeInTheDocument()
    })

    await user.click(screen.getByLabelText('Collapse overview'))

    // screenedCount = totalCandidates - pendingActionCount = 130 - 47 = 83
    expect(screen.getByText(/130 candidates/)).toBeInTheDocument()
    expect(screen.getByText(/83 screened/)).toBeInTheDocument()
    expect(screen.getByText(/3 stale/)).toBeInTheDocument()
  })

  it('should show skeleton loading state', () => {
    server.use(
      http.get('/api/recruitments/:id/overview', () => {
        return new Promise(() => {})
      }),
    )

    renderDashboard()

    expect(screen.getAllByTestId('skeleton-card')).toHaveLength(3)
  })

  it('should render empty state when overview returns zero candidates', async () => {
    server.use(
      http.get('/api/recruitments/:id/overview', () => {
        return HttpResponse.json({
          ...mockOverviewData,
          totalCandidates: 0,
          pendingActionCount: 0,
          totalStale: 0,
          steps: mockOverviewData.steps.map((s) => ({
            ...s,
            totalCandidates: 0,
            pendingCount: 0,
            staleCount: 0,
            outcomeBreakdown: { notStarted: 0, pass: 0, fail: 0, hold: 0 },
          })),
        })
      }),
    )

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('No candidates imported yet.')).toBeInTheDocument()
    })
  })
})
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/OverviewDashboard.test.tsx --reporter=verbose 2>&1 | tail -20`
Expected: FAIL (module not found)

**Step 3: Implement OverviewDashboard**

Create `web/src/features/overview/OverviewDashboard.tsx`:

```tsx
import { useState, useEffect } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { KpiCard } from './KpiCard'
import { PendingActionsPanel } from './PendingActionsPanel'
import { StepSummaryCard } from './StepSummaryCard'
import { useRecruitmentOverview } from './hooks/useRecruitmentOverview'

interface OverviewDashboardProps {
  recruitmentId: string
  onStepFilter: (stepId: string) => void
  onStaleFilter: (stepId: string) => void
}

export function OverviewDashboard({
  recruitmentId,
  onStepFilter,
  onStaleFilter,
}: OverviewDashboardProps) {
  const storageKey = `overview-collapsed:${recruitmentId}`
  const [isOpen, setIsOpen] = useState(() => {
    const stored = localStorage.getItem(storageKey)
    return stored !== 'true'
  })

  const { data, isPending } = useRecruitmentOverview(recruitmentId)

  useEffect(() => {
    const stored = localStorage.getItem(storageKey)
    setIsOpen(stored !== 'true')
  }, [storageKey])

  function handleOpenChange(open: boolean) {
    setIsOpen(open)
    localStorage.setItem(storageKey, String(!open))
  }

  if (isPending) {
    return (
      <section aria-label="Overview">
        <div className="grid grid-cols-3 gap-4">
          <SkeletonLoader variant="card" />
          <SkeletonLoader variant="card" />
          <SkeletonLoader variant="card" />
        </div>
      </section>
    )
  }

  if (!data) return null

  const screenedCount = data.totalCandidates - data.pendingActionCount

  return (
    <section aria-label="Overview">
      <Collapsible open={isOpen} onOpenChange={handleOpenChange}>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold">Overview</h2>
          <CollapsibleTrigger asChild>
            <button
              className="rounded p-1 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
              aria-label={isOpen ? 'Collapse overview' : 'Expand overview'}
            >
              {isOpen ? (
                <ChevronUp className="h-5 w-5" />
              ) : (
                <ChevronDown className="h-5 w-5" />
              )}
            </button>
          </CollapsibleTrigger>
        </div>

        {!isOpen && (
          <p className="text-sm text-stone-600">
            {data.totalCandidates} candidates - {screenedCount} screened - {data.totalStale} stale
          </p>
        )}

        <CollapsibleContent>
          {data.totalCandidates === 0 ? (
            <p className="text-sm text-stone-500">No candidates imported yet.</p>
          ) : (
            <>
              <div className="mb-4 grid grid-cols-3 gap-4">
                <KpiCard label="Total Candidates" value={data.totalCandidates} />
                <PendingActionsPanel count={data.pendingActionCount} />
                <KpiCard label="Stale Candidates" value={data.totalStale} variant="warning" />
              </div>

              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.steps.map((step) => (
                  <StepSummaryCard
                    key={step.stepId}
                    step={step}
                    totalCandidates={data.totalCandidates}
                    staleDays={data.staleDays}
                    onStepFilter={onStepFilter}
                    onStaleFilter={onStaleFilter}
                  />
                ))}
              </div>
            </>
          )}
        </CollapsibleContent>
      </Collapsible>
    </section>
  )
}
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/overview/OverviewDashboard.test.tsx --reporter=verbose 2>&1 | tail -20`
Expected: 7 tests PASS

**Step 5: Commit**

```bash
git add web/src/features/overview/OverviewDashboard.tsx web/src/features/overview/OverviewDashboard.test.tsx
git commit -m "feat(overview): add OverviewDashboard container with collapsible state and localStorage persistence"
```

---

### Task 7: Cache Invalidation on Outcome Recording + Test

**Mode:** Test-first (regression-critical: verify overview query is invalidated)

**Files:**
- Modify: `web/src/features/screening/hooks/useRecordOutcome.ts`
- Create or modify: `web/src/features/screening/hooks/useRecordOutcome.test.ts`

**Step 1: Write the failing test**

Create `web/src/features/screening/hooks/useRecordOutcome.test.ts`:

```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, act, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useRecordOutcome } from './useRecordOutcome'
import type { ReactNode } from 'react'

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return { Wrapper: ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  ), queryClient }
}

describe('useRecordOutcome', () => {
  it('should invalidate overview query when outcome is recorded', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useRecordOutcome(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate({
        recruitmentId: 'rec-1',
        candidateId: 'cand-1',
        data: { workflowStepId: 'step-1', status: 'Pass', notes: null },
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(invalidateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: ['recruitment', 'rec-1', 'overview'],
      }),
    )
  })
})
```

**Step 2: Run test to verify it fails**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/screening/hooks/useRecordOutcome.test.ts --reporter=verbose 2>&1 | tail -20`
Expected: FAIL (invalidateQueries not called with overview key)

**Step 3: Add overview invalidation to useRecordOutcome**

Modify `web/src/features/screening/hooks/useRecordOutcome.ts` -- add the third invalidation in `onSuccess`:

```typescript
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: ['screening', 'history', variables.candidateId],
      })
      queryClient.invalidateQueries({
        queryKey: ['candidates', variables.recruitmentId],
      })
      queryClient.invalidateQueries({
        queryKey: ['recruitment', variables.recruitmentId, 'overview'],
      })
    },
```

**Step 4: Run test to verify it passes**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run src/features/screening/hooks/useRecordOutcome.test.ts --reporter=verbose 2>&1 | tail -20`
Expected: 1 test PASS

**Step 5: Commit**

```bash
git add web/src/features/screening/hooks/useRecordOutcome.ts web/src/features/screening/hooks/useRecordOutcome.test.ts
git commit -m "feat(overview): add overview cache invalidation on outcome recording"
```

---

### Task 8: Integration -- Wire OverviewDashboard into RecruitmentPage + Filter Coordination

**Mode:** Test-first (filter state coordination between overview and candidate list)

**Files:**
- Modify: `web/src/features/candidates/CandidateList.tsx`
- Modify: `web/src/features/recruitments/pages/RecruitmentPage.tsx`
- Modify: `web/src/features/recruitments/pages/RecruitmentPage.test.tsx`

**Step 1: Add external filter props to CandidateList**

Modify `web/src/features/candidates/CandidateList.tsx`:

Add to `CandidateListProps`:
```typescript
interface CandidateListProps {
  recruitmentId: string
  isClosed: boolean
  workflowSteps?: WorkflowStepDto[]
  selectedId?: string | null
  onSelect?: (id: string) => void
  externalStepFilter?: string
  externalStaleOnly?: boolean
  onClearExternalFilters?: () => void
}
```

Destructure new props:
```typescript
export function CandidateList({
  recruitmentId,
  isClosed,
  workflowSteps = [],
  selectedId,
  onSelect,
  externalStepFilter,
  externalStaleOnly,
  onClearExternalFilters,
}: CandidateListProps) {
```

Merge external filters into the `useCandidates` call:
```typescript
  const effectiveStepFilter = externalStepFilter ?? stepFilter
  const effectiveStaleOnly = externalStaleOnly || undefined

  const { data, isPending } = useCandidates({
    recruitmentId,
    page,
    search,
    stepId: effectiveStepFilter,
    outcomeStatus: outcomeFilter,
    staleOnly: effectiveStaleOnly,
  })
```

Add external filter badge display (before existing filter badges):
```typescript
      {/* External filter badges (from overview) */}
      {(externalStepFilter || externalStaleOnly) && (
        <div className="mb-4 flex flex-wrap items-center gap-2" aria-live="polite">
          {externalStepFilter && (
            <Badge variant="secondary" className="gap-1">
              Step: {workflowSteps.find((s) => s.id === externalStepFilter)?.name ?? 'Unknown'}
              {externalStaleOnly && ' (stale only)'}
              <button
                onClick={onClearExternalFilters}
                className="ml-1"
                aria-label="Clear overview filter"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
        </div>
      )}
```

Update `hasActiveFilters` to include external:
```typescript
  const hasActiveFilters = !!search || !!stepFilter || !!outcomeFilter || !!externalStepFilter || !!externalStaleOnly
```

**Step 2: Wire OverviewDashboard into RecruitmentPage**

Modify `web/src/features/recruitments/pages/RecruitmentPage.tsx`:

Add imports:
```typescript
import { useState } from 'react'    // already imported
import { OverviewDashboard } from '@/features/overview/OverviewDashboard'
```

Add filter state after `closeDialogOpen`:
```typescript
  const [overviewStepFilter, setOverviewStepFilter] = useState<string | undefined>()
  const [overviewStaleOnly, setOverviewStaleOnly] = useState(false)

  function handleStepFilter(stepId: string) {
    setOverviewStaleOnly(false)
    setOverviewStepFilter(stepId)
  }

  function handleStaleFilter(stepId: string) {
    setOverviewStepFilter(stepId)
    setOverviewStaleOnly(true)
  }

  function handleClearOverviewFilters() {
    setOverviewStepFilter(undefined)
    setOverviewStaleOnly(false)
  }
```

Add `OverviewDashboard` before `CandidateList` in the JSX (after `MemberList`):
```tsx
      {!isClosed && (
        <OverviewDashboard
          recruitmentId={data.id}
          onStepFilter={handleStepFilter}
          onStaleFilter={handleStaleFilter}
        />
      )}

      <CandidateList
        recruitmentId={data.id}
        isClosed={isClosed}
        workflowSteps={data.steps}
        externalStepFilter={overviewStepFilter}
        externalStaleOnly={overviewStaleOnly}
        onClearExternalFilters={handleClearOverviewFilters}
      />
```

**Step 3: Write integration tests**

Add to `web/src/features/recruitments/pages/RecruitmentPage.test.tsx`:

```typescript
  it('should filter candidate list when step is clicked in overview', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByText('Screening')).toBeInTheDocument()
    })

    // Wait for overview to load
    await waitFor(() => {
      expect(screen.getByLabelText('Total Candidates: 130')).toBeInTheDocument()
    })

    // Click step in overview
    await user.click(screen.getByRole('button', { name: /filter by step: screening/i }))

    // External filter badge should appear
    await waitFor(() => {
      expect(screen.getByText(/Step: Screening/)).toBeInTheDocument()
    })
  })

  it('should filter to stale candidates when stale indicator is clicked', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByLabelText('Total Candidates: 130')).toBeInTheDocument()
    })

    await user.click(
      screen.getByRole('button', { name: /show stale candidates at step: screening/i }),
    )

    await waitFor(() => {
      expect(screen.getByText(/stale only/i)).toBeInTheDocument()
    })
  })

  it('should show active filter indicator with clear button', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByLabelText('Total Candidates: 130')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /filter by step: screening/i }))

    await waitFor(() => {
      expect(screen.getByLabelText('Clear overview filter')).toBeInTheDocument()
    })
  })

  it('should clear filter and show all candidates when clear button is clicked', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByLabelText('Total Candidates: 130')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: /filter by step: screening/i }))

    await waitFor(() => {
      expect(screen.getByLabelText('Clear overview filter')).toBeInTheDocument()
    })

    await user.click(screen.getByLabelText('Clear overview filter'))

    await waitFor(() => {
      expect(screen.queryByLabelText('Clear overview filter')).not.toBeInTheDocument()
    })
  })
```

Note: The existing `renderWithRoute` helper needs an `AuthProvider` added. Update it:

```typescript
import { AuthProvider } from '@/features/auth/AuthContext'
import userEvent from '@testing-library/user-event'

function renderWithRoute(recruitmentId: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  const router = createMemoryRouter(
    [{ path: '/recruitments/:recruitmentId', element: <RecruitmentPage /> }],
    { initialEntries: [`/recruitments/${recruitmentId}`] },
  )
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  )
}
```

**Step 4: Run all tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run --reporter=verbose 2>&1 | tail -30`
Expected: All tests pass

**Step 5: Commit**

```bash
git add web/src/features/candidates/CandidateList.tsx web/src/features/recruitments/pages/RecruitmentPage.tsx web/src/features/recruitments/pages/RecruitmentPage.test.tsx
git commit -m "feat(overview): integrate OverviewDashboard with RecruitmentPage and wire click-to-filter"
```

---

## Verification Checklist

After all tasks, run full suite and verify:

1. `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx vitest run --reporter=verbose` -- all tests pass
2. `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5/web && npx tsc --noEmit` -- no TypeScript errors (pre-existing errors in screening files are acceptable)
3. `cd /home/thomasg/Projects/Web/recruitment-tracker-epic5 && dotnet build api/api.sln --no-restore -warnaserror` -- backend still builds

## AC Coverage Map

| AC | Test(s) | Task |
|----|---------|------|
| AC1: Overview visible on page load | OverviewDashboard: "should render expanded by default" | T6 |
| AC2: KPI cards and pipeline breakdown | OverviewDashboard: "should display KPI cards with correct values"; StepSummaryCard: "should render step name and candidate count", "should show proportional width bar segment" | T3, T4, T6 |
| AC3: Collapse to inline summary | OverviewDashboard: "should display inline summary when collapsed", "should persist collapse state" | T6 |
| AC4: Expand from collapsed | OverviewDashboard: "should persist collapse state to localStorage on toggle" | T6 |
| AC5: Stale indicator per step | StepSummaryCard: "should show stale indicator with clock icon when staleCount > 0", "should not show stale indicator when staleCount is 0" | T4 |
| AC6: Click step to filter | StepSummaryCard: "should call onStepFilter when step name is clicked"; RecruitmentPage: "should filter candidate list when step is clicked" | T4, T8 |
| AC7: Click stale to filter | StepSummaryCard: "should call onStaleFilter when stale indicator is clicked"; RecruitmentPage: "should filter to stale candidates when stale indicator is clicked" | T4, T8 |
| AC8: Overview updates on outcome | useRecordOutcome: "should invalidate overview query when outcome is recorded" | T7 |
| AC9: Visual design and brand | KpiCard: "should apply warning styles"; all components use Tailwind brand classes | T3, T4, T6 |
