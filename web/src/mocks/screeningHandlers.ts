import { http, HttpResponse } from 'msw'
import { mockOutcomeResult, mockOutcomeHistoryList } from './fixtures/screening'
import type { RecordOutcomeRequest } from '@/lib/api/screening.types'

export const screeningHandlers = [
  http.post(
    '/api/recruitments/:recruitmentId/candidates/:candidateId/screening/outcome',
    async ({ request }) => {
      const body = (await request.json()) as RecordOutcomeRequest
      return HttpResponse.json({
        ...mockOutcomeResult,
        outcome: body.outcome,
        reason: body.reason ?? null,
        workflowStepId: body.workflowStepId,
      })
    },
  ),

  http.get(
    '/api/recruitments/:recruitmentId/candidates/:candidateId/screening/history',
    () => {
      return HttpResponse.json(mockOutcomeHistoryList)
    },
  ),
]
