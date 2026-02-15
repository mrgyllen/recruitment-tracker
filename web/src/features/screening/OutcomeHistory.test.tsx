import { OutcomeHistory } from './OutcomeHistory'
import type { OutcomeHistoryDto } from '@/lib/api/screening.types'
import { render, screen } from '@/test-utils'

const mockHistory: OutcomeHistoryDto[] = [
  {
    workflowStepId: 'step-1',
    workflowStepName: 'Screening',
    stepOrder: 1,
    outcome: 'Pass',
    reason: 'Strong skills',
    recordedAt: '2026-02-14T14:00:00Z',
    recordedByUserId: 'user-1',
  },
  {
    workflowStepId: 'step-2',
    workflowStepName: 'Interview',
    stepOrder: 2,
    outcome: 'Fail',
    reason: null,
    recordedAt: '2026-02-14T15:00:00Z',
    recordedByUserId: 'user-2',
  },
]

describe('OutcomeHistory', () => {
  it('should display outcome history ordered by step', () => {
    render(<OutcomeHistory history={mockHistory} />)
    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(2)
    expect(items[0]).toHaveTextContent('Screening')
    expect(items[1]).toHaveTextContent('Interview')
  })

  it('should show reason text when provided', () => {
    render(<OutcomeHistory history={mockHistory} />)
    expect(screen.getByText('Strong skills')).toBeInTheDocument()
  })

  it('should display empty state when no outcomes recorded', () => {
    render(<OutcomeHistory history={[]} />)
    expect(screen.getByText(/no outcomes recorded/i)).toBeInTheDocument()
  })

  it('should show status badge with correct variant for each outcome', () => {
    render(<OutcomeHistory history={mockHistory} />)
    expect(screen.getByText('Pass')).toBeInTheDocument()
    expect(screen.getByText('Fail')).toBeInTheDocument()
  })

  it('should display recorded date', () => {
    render(<OutcomeHistory history={mockHistory} />)
    const items = screen.getAllByRole('listitem')
    expect(items[0]).toHaveTextContent(/2026/)
  })
})
