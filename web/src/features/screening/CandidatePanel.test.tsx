import { render, screen } from '@/test-utils'
import { CandidatePanel } from './CandidatePanel'

const defaultProps = {
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  selectedCandidateId: null,
  onCandidateSelect: vi.fn(),
  sessionScreenedCount: 0,
  totalScreenedCount: 47,
  totalCandidateCount: 130,
  isAllScreened: false,
  isClosed: false,
  workflowSteps: [],
}

describe('CandidatePanel', () => {
  it('should display total screening progress', () => {
    render(<CandidatePanel {...defaultProps} />)
    expect(screen.getByText('47 of 130 screened')).toBeInTheDocument()
  })

  it('should display session screening progress', () => {
    render(<CandidatePanel {...defaultProps} sessionScreenedCount={12} />)
    expect(screen.getByText('12 this session')).toBeInTheDocument()
  })

  it('should show completion banner when all candidates screened', () => {
    render(
      <CandidatePanel
        {...defaultProps}
        totalScreenedCount={130}
        isAllScreened={true}
      />,
    )
    expect(screen.getByText('All candidates screened!')).toBeInTheDocument()
  })

  it('should not show completion banner when not all screened', () => {
    render(<CandidatePanel {...defaultProps} />)
    expect(screen.queryByText('All candidates screened!')).not.toBeInTheDocument()
  })
})
