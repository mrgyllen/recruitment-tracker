import { http, HttpResponse } from 'msw'
import type {
  PaginatedRecruitmentList,
  RecruitmentDetail,
  WorkflowStepDto,
} from '@/lib/api/recruitments.types'

export const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'
export const mockRecruitmentId2 = '660e8400-e29b-41d4-a716-446655440001'
export const forbiddenRecruitmentId = '770e8400-e29b-41d4-a716-446655440002'
const mockUserId = 'dev-user-a'

const mockRecruitment: RecruitmentDetail = {
  id: mockRecruitmentId,
  title: 'Senior .NET Developer',
  description: 'Hiring for senior backend role',
  jobRequisitionId: null,
  status: 'Active',
  createdAt: new Date().toISOString(),
  closedAt: null,
  createdByUserId: mockUserId,
  steps: [
    { id: 'step-1', name: 'Screening', order: 1 },
    { id: 'step-2', name: 'Technical Test', order: 2 },
    { id: 'step-3', name: 'Technical Interview', order: 3 },
  ],
  members: [{ id: 'member-1', userId: mockUserId, role: 'Recruiting Leader' }],
}

const mockRecruitment2: RecruitmentDetail = {
  id: mockRecruitmentId2,
  title: 'Frontend Engineer',
  description: 'React/TypeScript role',
  jobRequisitionId: 'REQ-2026-002',
  status: 'Closed',
  createdAt: new Date().toISOString(),
  closedAt: new Date().toISOString(),
  createdByUserId: mockUserId,
  steps: [
    { id: 'step-4', name: 'Screening', order: 1 },
    { id: 'step-5', name: 'Interview', order: 2 },
  ],
  members: [{ id: 'member-2', userId: mockUserId, role: 'Recruiting Leader' }],
}

const recruitmentsById: Record<string, RecruitmentDetail> = {
  [mockRecruitmentId]: mockRecruitment,
  [mockRecruitmentId2]: mockRecruitment2,
}

export const recruitmentHandlers = [
  http.post('/api/recruitments', async () => {
    return HttpResponse.json(
      { id: mockRecruitmentId },
      {
        status: 201,
        headers: {
          Location: `/api/recruitments/${mockRecruitmentId}`,
        },
      },
    )
  }),

  http.get('/api/recruitments/:id', ({ params }) => {
    const { id } = params

    if (id === forbiddenRecruitmentId) {
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.3',
          title: 'Forbidden',
          status: 403,
        },
        { status: 403 },
      )
    }

    const recruitment = recruitmentsById[id as string]
    if (recruitment) {
      return HttpResponse.json(recruitment)
    }

    return HttpResponse.json(
      {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4',
        title: 'Not Found',
        status: 404,
      },
      { status: 404 },
    )
  }),

  http.get('/api/recruitments', () => {
    const response: PaginatedRecruitmentList = {
      items: [
        {
          id: mockRecruitmentId,
          title: 'Senior .NET Developer',
          description: 'Hiring for senior backend role',
          status: 'Active',
          createdAt: new Date().toISOString(),
          closedAt: null,
          stepCount: 3,
          memberCount: 1,
        },
        {
          id: mockRecruitmentId2,
          title: 'Frontend Engineer',
          description: 'React/TypeScript role',
          status: 'Closed',
          createdAt: new Date().toISOString(),
          closedAt: new Date().toISOString(),
          stepCount: 2,
          memberCount: 1,
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    }
    return HttpResponse.json(response)
  }),

  http.put('/api/recruitments/:id', async ({ params, request }) => {
    const { id } = params
    const recruitment = recruitmentsById[id as string]
    if (!recruitment) {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }
    if (recruitment.status === 'Closed') {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1', title: 'Recruitment is closed', status: 400 },
        { status: 400 },
      )
    }
    const body = (await request.json()) as Record<string, unknown>
    recruitment.title = body.title as string
    recruitment.description = (body.description as string) ?? null
    recruitment.jobRequisitionId = (body.jobRequisitionId as string) ?? null
    return new HttpResponse(null, { status: 204 })
  }),

  http.post('/api/recruitments/:id/steps', async ({ params, request }) => {
    const { id } = params
    const recruitment = recruitmentsById[id as string]
    if (!recruitment) {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }
    if (recruitment.status === 'Closed') {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1', title: 'Recruitment is closed', status: 400 },
        { status: 400 },
      )
    }
    const body = (await request.json()) as { name: string; order: number }
    const newStep: WorkflowStepDto = {
      id: `step-new-${Date.now()}`,
      name: body.name,
      order: body.order,
    }
    recruitment.steps.push(newStep)
    return HttpResponse.json(newStep)
  }),

  http.delete('/api/recruitments/:id/steps/:stepId', ({ params }) => {
    const { id, stepId } = params
    const recruitment = recruitmentsById[id as string]
    if (!recruitment) {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }
    if (stepId === 'step-has-outcomes') {
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
          title: 'Cannot remove -- outcomes recorded at this step',
          status: 409,
        },
        { status: 409 },
      )
    }
    recruitment.steps = recruitment.steps.filter((s) => s.id !== stepId)
    return new HttpResponse(null, { status: 204 })
  }),

  http.post('/api/recruitments/:id/close', ({ params }) => {
    const { id } = params
    const recruitment = recruitmentsById[id as string]
    if (!recruitment) {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }
    if (recruitment.status === 'Closed') {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1', title: 'Recruitment is closed', status: 400 },
        { status: 400 },
      )
    }
    recruitment.status = 'Closed'
    recruitment.closedAt = new Date().toISOString()
    return new HttpResponse(null, { status: 204 })
  }),

  http.put('/api/recruitments/:id/steps/reorder', async ({ params, request }) => {
    const { id } = params
    const recruitment = recruitmentsById[id as string]
    if (!recruitment) {
      return HttpResponse.json(
        { type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }
    const body = (await request.json()) as { steps: { stepId: string; order: number }[] }
    for (const item of body.steps) {
      const step = recruitment.steps.find((s) => s.id === item.stepId)
      if (step) step.order = item.order
    }
    recruitment.steps.sort((a, b) => a.order - b.order)
    return new HttpResponse(null, { status: 204 })
  }),
]
