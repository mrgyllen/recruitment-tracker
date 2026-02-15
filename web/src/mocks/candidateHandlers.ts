import { http, HttpResponse } from 'msw'
import {
  mockCandidateDetail,
  mockCandidateDocument,
  mockCandidates,
} from './fixtures/candidates'
import type {
  CandidateDocumentDto,
  PaginatedCandidateList,
} from '@/lib/api/candidates.types'

const duplicateEmail = 'duplicate@example.com'

export const candidateHandlers = [
  http.get('/api/recruitments/:recruitmentId/candidates', ({ request }) => {
    const url = new URL(request.url)
    const search = url.searchParams.get('search')?.toLowerCase()
    const stepId = url.searchParams.get('stepId')
    const outcomeStatus = url.searchParams.get('outcomeStatus')

    let filtered = [...mockCandidates]

    if (search) {
      filtered = filtered.filter(
        (c) =>
          c.fullName.toLowerCase().includes(search) ||
          c.email.toLowerCase().includes(search),
      )
    }

    if (stepId) {
      filtered = filtered.filter(
        (c) => c.currentWorkflowStepId === stepId,
      )
    }

    if (outcomeStatus) {
      filtered = filtered.filter(
        (c) => c.currentOutcomeStatus === outcomeStatus,
      )
    }

    const response: PaginatedCandidateList = {
      items: filtered,
      totalCount: filtered.length,
      page: 1,
      pageSize: 50,
    }
    return HttpResponse.json(response)
  }),

  http.get(
    '/api/recruitments/:recruitmentId/candidates/:candidateId',
    ({ params }) => {
      const { candidateId } = params
      if (candidateId === mockCandidateDetail.id) {
        return HttpResponse.json(mockCandidateDetail)
      }
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4',
          title: 'The specified resource was not found.',
          status: 404,
        },
        { status: 404 },
      )
    },
  ),

  http.post(
    '/api/recruitments/:recruitmentId/candidates',
    async ({ params, request }) => {
      const body = (await request.json()) as { email?: string }

      if (body.email === duplicateEmail) {
        return HttpResponse.json(
          {
            type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
            title:
              'A candidate with this email already exists in this recruitment',
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
    },
  ),

  http.delete(
    '/api/recruitments/:recruitmentId/candidates/:candidateId',
    ({ params }) => {
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
    },
  ),

  http.post(
    '/api/recruitments/:recruitmentId/candidates/:candidateId/document',
    ({ params }) => {
      const response: CandidateDocumentDto = {
        ...mockCandidateDocument,
        candidateId: params.candidateId as string,
      }
      return HttpResponse.json(response, { status: 200 })
    },
  ),

  http.post(
    '/api/recruitments/:recruitmentId/candidates/:candidateId/document/assign',
    ({ params }) => {
      const response: CandidateDocumentDto = {
        ...mockCandidateDocument,
        candidateId: params.candidateId as string,
      }
      return HttpResponse.json(response, { status: 200 })
    },
  ),
]
