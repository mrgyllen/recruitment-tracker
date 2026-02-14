import { describe, expect, it, vi } from 'vitest'
import userEvent from '@testing-library/user-event'
import { ImportSummary } from './ImportSummary'
import { render, screen } from '@/test-utils'
import type { ImportRowResult } from '@/lib/api/import.types'

const defaultRows: ImportRowResult[] = [
  { rowNumber: 1, candidateEmail: 'a@test.com', action: 'Created', errorMessage: null, resolution: null },
  { rowNumber: 2, candidateEmail: 'b@test.com', action: 'Updated', errorMessage: null, resolution: null },
  { rowNumber: 3, candidateEmail: null, action: 'Errored', errorMessage: 'Invalid email', resolution: null },
  { rowNumber: 4, candidateEmail: 'c@test.com', action: 'Flagged', errorMessage: null, resolution: null },
]

describe('ImportSummary', () => {
  const defaultProps = {
    createdCount: 1,
    updatedCount: 1,
    erroredCount: 1,
    flaggedCount: 1,
    rowResults: defaultRows,
    onDone: vi.fn(),
  }

  it('should render summary counts', () => {
    render(<ImportSummary {...defaultProps} />)
    expect(screen.getByText('Created')).toBeInTheDocument()
    expect(screen.getByText('Updated')).toBeInTheDocument()
    expect(screen.getByText('Errored')).toBeInTheDocument()
    expect(screen.getByText('Flagged')).toBeInTheDocument()
  })

  it('should show flagged match notice with review button', () => {
    const onReviewMatches = vi.fn()
    render(
      <ImportSummary {...defaultProps} onReviewMatches={onReviewMatches} />,
    )
    expect(
      screen.getByText(/1 match by name\+phone only/),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /review matches/i }),
    ).toBeInTheDocument()
  })

  it('should expand errored rows to show detail', async () => {
    const user = userEvent.setup()
    render(<ImportSummary {...defaultProps} />)

    await user.click(screen.getByText(/errored \(1\)/i))

    expect(screen.getByText('Row 3')).toBeInTheDocument()
    expect(screen.getByText('Invalid email')).toBeInTheDocument()
  })

  it('should call onDone when Done is clicked', async () => {
    const onDone = vi.fn()
    const user = userEvent.setup()
    render(<ImportSummary {...defaultProps} onDone={onDone} />)

    await user.click(screen.getByRole('button', { name: /done/i }))

    expect(onDone).toHaveBeenCalled()
  })

  it('should show failure reason when provided', () => {
    render(
      <ImportSummary {...defaultProps} failureReason="File is corrupted" />,
    )
    expect(screen.getByText('File is corrupted')).toBeInTheDocument()
  })
})
