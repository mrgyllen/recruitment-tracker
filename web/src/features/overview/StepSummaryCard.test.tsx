import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { StepSummaryCard } from './StepSummaryCard'
import type { StepOverview } from '@/lib/api/recruitments.types'
import { render, screen } from '@/test-utils'

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

    expect(
      screen.queryByText(/candidates > 5 days/),
    ).not.toBeInTheDocument()
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

    await user.click(
      screen.getByRole('button', { name: /filter by step: screening/i }),
    )
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
      screen.getByRole('button', {
        name: /show stale candidates at step: screening/i,
      }),
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
      screen.getByRole('button', {
        name: /show stale candidates at step: screening/i,
      }),
    ).toBeInTheDocument()
  })
})
