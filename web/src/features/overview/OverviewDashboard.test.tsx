import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { MemoryRouter } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { OverviewDashboard } from './OverviewDashboard'
import { AuthProvider } from '@/features/auth/AuthContext'
import {
  mockOverviewData,
  mockRecruitmentId,
} from '@/mocks/recruitmentHandlers'
import { server } from '@/mocks/server'

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

    expect(
      screen.getByText(String(mockOverviewData.totalCandidates)),
    ).toBeInTheDocument()
  })

  it('should render collapsed when localStorage indicates collapsed', async () => {
    localStorage.setItem(
      `overview-collapsed:${mockRecruitmentId}`,
      'true',
    )

    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText(/130 candidates/)).toBeInTheDocument()
    })

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
    expect(
      localStorage.getItem(`overview-collapsed:${mockRecruitmentId}`),
    ).toBe('true')

    // Expand
    await user.click(screen.getByLabelText('Expand overview'))
    expect(
      localStorage.getItem(`overview-collapsed:${mockRecruitmentId}`),
    ).toBe('false')
  })

  it('should display KPI cards with correct values when expanded', async () => {
    renderDashboard()

    await waitFor(() => {
      expect(screen.getByText('Total Candidates')).toBeInTheDocument()
    })

    expect(
      screen.getByLabelText('Total Candidates: 130'),
    ).toBeInTheDocument()
    expect(
      screen.getByLabelText('Pending Actions: 47'),
    ).toBeInTheDocument()
    expect(
      screen.getByLabelText('Stale Candidates: 3'),
    ).toBeInTheDocument()
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
      expect(
        screen.getByText('No candidates imported yet.'),
      ).toBeInTheDocument()
    })
  })
})
