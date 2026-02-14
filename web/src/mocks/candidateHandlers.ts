import { http, HttpResponse } from 'msw'
import type { PaginatedCandidateList } from '@/lib/api/candidates.types'
import { mockCandidates } from './fixtures/candidates'

const duplicateEmail = 'duplicate@example.com'

export const candidateHandlers = [
  http.get('/api/recruitments/:recruitmentId/candidates', () => {
    const response: PaginatedCandidateList = {
      items: mockCandidates,
      totalCount: mockCandidates.length,
      page: 1,
      pageSize: 50,
    }
    return HttpResponse.json(response)
  }),

  http.post('/api/recruitments/:recruitmentId/candidates', async ({ params, request }) => {
    const body = (await request.json()) as { email?: string }

    if (body.email === duplicateEmail) {
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
          title: 'A candidate with this email already exists in this recruitment',
          status: 400,
          detail: `Candidate with email '${duplicateEmail}' already exists.`,
        },
        { status: 400 },
      )
    }

    const { recruitmentId } = params
    const newId = `cand-new-${Date.now()}`
    return HttpResponse.json(
      { id: newId },
      {
        status: 201,
        headers: {
          Location: `/api/recruitments/${recruitmentId as string}/candidates/${newId}`,
        },
      },
    )
  }),

  http.delete('/api/recruitments/:recruitmentId/candidates/:candidateId', ({ params }) => {
    const { candidateId } = params
    const exists = mockCandidates.some((c) => c.id === candidateId)
    if (!exists) {
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4',
          title: 'The specified resource was not found.',
          status: 404,
        },
        { status: 404 },
      )
    }
    return new HttpResponse(null, { status: 204 })
  }),
]
