import { describe, expect, it } from 'vitest'
import { MatchReviewStep } from './MatchReviewStep'
import { render, screen } from '@/test-utils'
import type { ImportRowResult } from '@/lib/api/import.types'

const flaggedRows: ImportRowResult[] = [
  {
    rowNumber: 4,
    candidateEmail: 'flagged@example.com',
    action: 'Flagged',
    errorMessage: null,
    resolution: null,
  },
]

describe('MatchReviewStep', () => {
  it('should render flagged matches', () => {
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={flaggedRows}
        onDone={() => {}}
      />,
    )
    expect(
      screen.getByText(/row 4.*flagged@example\.com/i),
    ).toBeInTheDocument()
    expect(screen.getByText(/matched by name \+ phone/i)).toBeInTheDocument()
  })

  it('should show confirm and reject buttons', () => {
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={flaggedRows}
        onDone={() => {}}
      />,
    )
    expect(
      screen.getByRole('button', { name: /confirm match/i }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /reject/i }),
    ).toBeInTheDocument()
  })

  it('should show unresolved count', () => {
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={flaggedRows}
        onDone={() => {}}
      />,
    )
    expect(screen.getByText(/1 match needs review/i)).toBeInTheDocument()
  })

  it('should show resolved status for already resolved rows', () => {
    const resolvedRows: ImportRowResult[] = [
      { ...flaggedRows[0], resolution: 'Confirmed' },
    ]
    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={resolvedRows}
        onDone={() => {}}
      />,
    )
    expect(screen.getByText('Confirmed')).toBeInTheDocument()
    expect(screen.getByText('All matches reviewed')).toBeInTheDocument()
  })
})
