import { describe, expect, it, vi } from 'vitest'
import { MatchReviewStep, type FlaggedRow } from './MatchReviewStep'
import { render, screen } from '@/test-utils'
import userEvent from '@testing-library/user-event'
import { server } from '@/mocks/server'
import { http, HttpResponse } from 'msw'

const flaggedRows: FlaggedRow[] = [
  {
    rowNumber: 4,
    candidateEmail: 'flagged@example.com',
    action: 'Flagged',
    errorMessage: null,
    resolution: null,
    originalIndex: 3,
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
    const resolvedRows: FlaggedRow[] = [
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

  it('should send originalIndex as matchIndex, not filtered array index', async () => {
    const capturedRequests: { matchIndex: number; action: string }[] = []
    server.use(
      http.post('/api/import-sessions/:id/resolve-match', async ({ request }) => {
        const body = (await request.json()) as { matchIndex: number; action: string }
        capturedRequests.push(body)
        return HttpResponse.json({
          matchIndex: body.matchIndex,
          action: 'Confirmed',
          candidateEmail: 'flagged@example.com',
        })
      }),
    )

    const rows: FlaggedRow[] = [
      {
        rowNumber: 4,
        candidateEmail: 'flagged@example.com',
        action: 'Flagged',
        errorMessage: null,
        resolution: null,
        originalIndex: 5,
      },
    ]

    render(
      <MatchReviewStep
        importSessionId="session-1"
        flaggedRows={rows}
        onDone={() => {}}
      />,
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: /confirm match/i }))

    await vi.waitFor(() => {
      expect(capturedRequests).toHaveLength(1)
    })
    expect(capturedRequests[0].matchIndex).toBe(5)
  })
})
