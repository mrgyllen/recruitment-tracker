import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { OutcomeForm } from './OutcomeForm'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

const defaultProps = {
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  candidateId: 'cand-1111-1111-1111-111111111111',
  currentStepId: 'step-1111-1111-1111-111111111111',
  currentStepName: 'Screening',
  existingOutcome: null,
  isClosed: false,
  onOutcomeRecorded: vi.fn(),
}

describe('OutcomeForm', () => {
  it('should display Pass, Fail, Hold buttons', () => {
    render(<OutcomeForm {...defaultProps} />)
    expect(screen.getByRole('button', { name: /pass/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /fail/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /hold/i })).toBeInTheDocument()
  })

  it('should show reason textarea always visible', () => {
    render(<OutcomeForm {...defaultProps} />)
    expect(screen.getByRole('textbox', { name: /reason/i })).toBeInTheDocument()
  })

  it('should disable confirm button when no outcome selected', () => {
    render(<OutcomeForm {...defaultProps} />)
    expect(screen.getByRole('button', { name: /confirm/i })).toBeDisabled()
  })

  it('should enable confirm button when outcome selected', async () => {
    const user = userEvent.setup()
    render(<OutcomeForm {...defaultProps} />)
    await user.click(screen.getByRole('button', { name: /pass/i }))
    expect(screen.getByRole('button', { name: /confirm/i })).toBeEnabled()
  })

  it('should call onOutcomeRecorded after successful submission', async () => {
    const user = userEvent.setup()
    const onOutcomeRecorded = vi.fn()
    render(<OutcomeForm {...defaultProps} onOutcomeRecorded={onOutcomeRecorded} />)

    await user.click(screen.getByRole('button', { name: /pass/i }))
    await user.type(screen.getByRole('textbox', { name: /reason/i }), 'Strong candidate')
    await user.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => {
      expect(onOutcomeRecorded).toHaveBeenCalled()
    })
  })

  it('should pre-fill form with existing outcome', () => {
    render(
      <OutcomeForm
        {...defaultProps}
        existingOutcome={{
          workflowStepId: 'step-1111-1111-1111-111111111111',
          workflowStepName: 'Screening',
          stepOrder: 1,
          outcome: 'Fail',
          reason: 'Lacking experience',
          recordedAt: '2026-02-14T14:00:00Z',
          recordedByUserId: 'user-1',
        }}
      />,
    )
    expect(screen.getByRole('button', { name: /fail/i })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('textbox', { name: /reason/i })).toHaveValue('Lacking experience')
  })

  it('should disable all controls when recruitment is closed', () => {
    render(<OutcomeForm {...defaultProps} isClosed={true} />)
    expect(screen.getByRole('button', { name: /pass/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /fail/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /hold/i })).toBeDisabled()
    expect(screen.getByRole('textbox', { name: /reason/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /confirm/i })).toBeDisabled()
  })

  it('should display shortcut hints on outcome buttons', () => {
    render(<OutcomeForm {...defaultProps} />)
    expect(screen.getByRole('button', { name: /pass \(1\)/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /fail \(2\)/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /hold \(3\)/i })).toBeInTheDocument()
  })

  it('should apply externalOutcome prop to select outcome', () => {
    render(<OutcomeForm {...defaultProps} externalOutcome="Hold" />)
    expect(screen.getByRole('button', { name: /hold/i })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: /confirm/i })).toBeEnabled()
  })

  it('should call onOutcomeSelect when outcome button is clicked', async () => {
    const onOutcomeSelect = vi.fn()
    const user = userEvent.setup()
    render(<OutcomeForm {...defaultProps} onOutcomeSelect={onOutcomeSelect} />)
    await user.click(screen.getByRole('button', { name: /pass/i }))
    expect(onOutcomeSelect).toHaveBeenCalledWith('Pass')
  })

  it('should show error on API failure', async () => {
    server.use(
      http.post(
        '/api/recruitments/:recruitmentId/candidates/:candidateId/screening/outcome',
        () => HttpResponse.json(
          { title: 'Invalid workflow transition', status: 400 },
          { status: 400 },
        ),
      ),
    )

    const user = userEvent.setup()
    render(<OutcomeForm {...defaultProps} />)
    await user.click(screen.getByRole('button', { name: /pass/i }))
    await user.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })
  })
})
