import { http, HttpResponse } from 'msw'
import type {
  PaginatedRecruitmentList,
  RecruitmentDetail,
} from '@/lib/api/recruitments.types'

const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'
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
    if (id === mockRecruitmentId) {
      return HttpResponse.json(mockRecruitment)
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
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    }
    return HttpResponse.json(response)
  }),
]
