import { http, HttpResponse } from 'msw'
import type { ImportSessionResponse } from '@/lib/api/import.types'

export const mockImportSessionId = 'import-session-001'

export const mockCompletedSession: ImportSessionResponse = {
  id: mockImportSessionId,
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  status: 'Completed',
  sourceFileName: 'workday-export.xlsx',
  createdAt: new Date().toISOString(),
  completedAt: new Date().toISOString(),
  totalRows: 10,
  createdCount: 7,
  updatedCount: 2,
  erroredCount: 0,
  flaggedCount: 1,
  failureReason: null,
  rowResults: [
    {
      rowNumber: 1,
      candidateEmail: 'anna@example.com',
      action: 'Created',
      errorMessage: null,
      resolution: null,
    },
    {
      rowNumber: 2,
      candidateEmail: 'bob@example.com',
      action: 'Updated',
      errorMessage: null,
      resolution: null,
    },
    {
      rowNumber: 3,
      candidateEmail: 'flagged@example.com',
      action: 'Flagged',
      errorMessage: null,
      resolution: null,
    },
  ],
}

export const mockProcessingSession: ImportSessionResponse = {
  ...mockCompletedSession,
  status: 'Processing',
  completedAt: null,
  totalRows: 0,
  createdCount: 0,
  updatedCount: 0,
  erroredCount: 0,
  flaggedCount: 0,
  rowResults: [],
}

export const mockFailedSession: ImportSessionResponse = {
  ...mockCompletedSession,
  status: 'Failed',
  failureReason: 'Missing required column: Email',
  totalRows: 0,
  createdCount: 0,
  updatedCount: 0,
  erroredCount: 0,
  flaggedCount: 0,
  rowResults: [],
}

export const importHandlers = [
  http.post('/api/recruitments/:id/import', () => {
    return HttpResponse.json(
      {
        importSessionId: mockImportSessionId,
        statusUrl: `/api/import-sessions/${mockImportSessionId}`,
      },
      { status: 202 },
    )
  }),

  http.get('/api/import-sessions/:id', () => {
    return HttpResponse.json(mockCompletedSession)
  }),

  http.post('/api/import-sessions/:id/resolve-match', async ({ request }) => {
    const body = (await request.json()) as {
      matchIndex: number
      action: string
    }
    return HttpResponse.json({
      matchIndex: body.matchIndex,
      action: body.action === 'Confirm' ? 'Confirmed' : 'Rejected',
      candidateEmail: 'flagged@example.com',
    })
  }),
]
