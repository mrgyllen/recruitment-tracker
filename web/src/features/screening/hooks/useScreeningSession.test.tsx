import { renderHook, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from '@/components/ui/sonner'
import { useScreeningSession } from './useScreeningSession'
import type { CandidateResponse } from '@/lib/api/candidates.types'
import type { OutcomeResultDto } from '@/lib/api/screening.types'

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

const recruitmentId = '550e8400-e29b-41d4-a716-446655440000'

function makeCandidate(
  id: string,
  name: string,
  outcomeStatus: string | null = null,
): CandidateResponse {
  return {
    id,
    recruitmentId,
    fullName: name,
    email: `${name.toLowerCase().replace(' ', '.')}@example.com`,
    phoneNumber: null,
    location: null,
    dateApplied: '2026-02-10T00:00:00Z',
    createdAt: '2026-02-10T12:00:00Z',
    document: null,
    documentSasUrl: null,
    currentWorkflowStepId: 'step-1',
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: outcomeStatus,
  }
}

function makeOutcomeResult(candidateId: string, outcome = 'Pass'): OutcomeResultDto {
  return {
    outcomeId: `out-${candidateId}`,
    candidateId,
    workflowStepId: 'step-1',
    outcome: outcome as OutcomeResultDto['outcome'],
    reason: null,
    recordedAt: '2026-02-14T14:00:00Z',
    recordedBy: 'user-1',
    newCurrentStepId: 'step-2',
    isCompleted: false,
  }
}

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return {
    queryClient,
    wrapper: ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>
        {children}
        <Toaster />
      </QueryClientProvider>
    ),
  }
}

describe('useScreeningSession', () => {
  it('should initialize with no selected candidate', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice'),
      makeCandidate('cand-2', 'Bob'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    expect(result.current.selectedCandidateId).toBeNull()
    expect(result.current.selectedCandidate).toBeNull()
    expect(result.current.sessionScreenedCount).toBe(0)
  })

  it('should select candidate and update selectedCandidateId', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice'),
      makeCandidate('cand-2', 'Bob'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    act(() => {
      result.current.selectCandidate('cand-2')
    })

    expect(result.current.selectedCandidateId).toBe('cand-2')
    expect(result.current.selectedCandidate?.fullName).toBe('Bob')
  })

  it('should compute total screened count from candidates array', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice', 'Pass'),
      makeCandidate('cand-2', 'Bob', null),
      makeCandidate('cand-3', 'Carol', 'Fail'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    expect(result.current.totalScreenedCount).toBe(2)
    expect(result.current.isAllScreened).toBe(false)
  })

  it('should detect when all candidates are screened', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice', 'Pass'),
      makeCandidate('cand-2', 'Bob', 'Fail'),
      makeCandidate('cand-3', 'Carol', 'Hold'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    expect(result.current.isAllScreened).toBe(true)
  })

  it('should exclude NotStarted from screened count', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice', 'NotStarted'),
      makeCandidate('cand-2', 'Bob', 'Pass'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    expect(result.current.totalScreenedCount).toBe(1)
    expect(result.current.isAllScreened).toBe(false)
  })

  it('should increment session screened count on outcome', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice'),
      makeCandidate('cand-2', 'Bob'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    act(() => {
      result.current.selectCandidate('cand-1')
    })

    act(() => {
      result.current.handleOutcomeRecorded(makeOutcomeResult('cand-1'))
    })

    expect(result.current.sessionScreenedCount).toBe(1)
  })

  it('should auto-advance to next unscreened candidate after 300ms delay', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice'),
      makeCandidate('cand-2', 'Bob'),
      makeCandidate('cand-3', 'Carol'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    act(() => {
      result.current.selectCandidate('cand-1')
    })

    act(() => {
      result.current.handleOutcomeRecorded(makeOutcomeResult('cand-1'))
    })

    // Before auto-advance delay
    expect(result.current.selectedCandidateId).toBe('cand-1')

    // After 300ms auto-advance
    act(() => {
      vi.advanceTimersByTime(300)
    })

    expect(result.current.selectedCandidateId).toBe('cand-2')
  })

  it('should undo outcome and restore session count', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice'),
      makeCandidate('cand-2', 'Bob'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    act(() => {
      result.current.selectCandidate('cand-1')
    })

    act(() => {
      result.current.handleOutcomeRecorded(makeOutcomeResult('cand-1'))
    })

    expect(result.current.sessionScreenedCount).toBe(1)

    act(() => {
      result.current.undoOutcome()
    })

    expect(result.current.sessionScreenedCount).toBe(0)
    expect(result.current.selectedCandidateId).toBe('cand-1')
  })

  it('should override auto-advance when user manually selects candidate', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice'),
      makeCandidate('cand-2', 'Bob'),
      makeCandidate('cand-3', 'Carol'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    act(() => {
      result.current.selectCandidate('cand-1')
    })

    act(() => {
      result.current.handleOutcomeRecorded(makeOutcomeResult('cand-1'))
    })

    // User manually selects cand-3 before auto-advance fires
    act(() => {
      result.current.selectCandidate('cand-3')
    })

    // Auto-advance timer fires but should be cancelled
    act(() => {
      vi.advanceTimersByTime(300)
    })

    expect(result.current.selectedCandidateId).toBe('cand-3')
  })

  it('should wrap to top of list when no unscreened below', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice', null),
      makeCandidate('cand-2', 'Bob', 'Pass'),
      makeCandidate('cand-3', 'Carol', 'Fail'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    // Select cand-2 (already screened) and record outcome
    act(() => {
      result.current.selectCandidate('cand-2')
    })

    act(() => {
      result.current.handleOutcomeRecorded(makeOutcomeResult('cand-2'))
    })

    act(() => {
      vi.advanceTimersByTime(300)
    })

    // Should wrap to cand-1 (the only unscreened)
    expect(result.current.selectedCandidateId).toBe('cand-1')
  })

  it('should stay on current candidate when all are screened', () => {
    const { wrapper } = createWrapper()
    const candidates = [
      makeCandidate('cand-1', 'Alice', 'Pass'),
      makeCandidate('cand-2', 'Bob', 'Fail'),
    ]
    const { result } = renderHook(
      () => useScreeningSession(recruitmentId, candidates),
      { wrapper },
    )

    act(() => {
      result.current.selectCandidate('cand-1')
    })

    act(() => {
      result.current.handleOutcomeRecorded(makeOutcomeResult('cand-1'))
    })

    act(() => {
      vi.advanceTimersByTime(300)
    })

    // All are already screened, should stay on cand-1
    expect(result.current.selectedCandidateId).toBe('cand-1')
  })
})
