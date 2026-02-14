import { http, HttpResponse } from 'msw'
import type { RecordOutcomeRequest } from '@/lib/api/screening.types'
import { mockOutcomeResult, mockOutcomeHistoryList } from './fixtures/screening'

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
