import { http, HttpResponse } from 'msw'
import type {
  MembersListResponse,
  TeamMemberDto,
} from '@/lib/api/team.types'

export const mockCreatorUserId = 'dev-user-a'

const mockMembers: Record<string, TeamMemberDto[]> = {
  '550e8400-e29b-41d4-a716-446655440000': [
    {
      id: 'member-1',
      userId: mockCreatorUserId,
      displayName: 'Dev User A',
      role: 'Recruiting Leader',
      isCreator: true,
      invitedAt: new Date().toISOString(),
    },
    {
      id: 'member-extra',
      userId: '22222222-2222-2222-2222-222222222222',
      displayName: 'Dev User B',
      role: 'SME/Collaborator',
      isCreator: false,
      invitedAt: new Date().toISOString(),
    },
  ],
}

const directoryUsers = [
  { id: '22222222-2222-2222-2222-222222222222', displayName: 'Dev User B', email: 'userb@dev.local' },
  { id: '33333333-3333-3333-3333-333333333333', displayName: 'Dev Admin', email: 'admin@dev.local' },
  { id: '44444444-4444-4444-4444-444444444444', displayName: 'Erik Leader', email: 'erik@dev.local' },
  { id: '55555555-5555-5555-5555-555555555555', displayName: 'Sara Specialist', email: 'sara@dev.local' },
]

export const teamHandlers = [
  http.get('/api/recruitments/:recruitmentId/members', ({ params }) => {
    const { recruitmentId } = params
    const members = mockMembers[recruitmentId as string] ?? []
    const response: MembersListResponse = {
      members,
      totalCount: members.length,
    }
    return HttpResponse.json(response)
  }),

  http.get('/api/recruitments/:recruitmentId/directory-search', ({ request }) => {
    const url = new URL(request.url)
    const q = url.searchParams.get('q') ?? ''
    if (q.length < 2) {
      return HttpResponse.json(
        { type: 'validation', title: 'Validation Failed', status: 400, errors: { SearchTerm: ['Minimum 2 characters'] } },
        { status: 400 },
      )
    }
    const results = directoryUsers.filter(
      u => u.displayName.toLowerCase().includes(q.toLowerCase())
        || u.email.toLowerCase().includes(q.toLowerCase()),
    )
    return HttpResponse.json(results)
  }),

  http.post('/api/recruitments/:recruitmentId/members', async ({ params, request }) => {
    const { recruitmentId } = params
    const body = (await request.json()) as { userId: string; displayName: string }

    const members = mockMembers[recruitmentId as string] ?? []
    if (members.some(m => m.userId === body.userId)) {
      return HttpResponse.json(
        { type: 'validation', title: 'Bad Request', status: 400, detail: `User ${body.userId} is already a member.` },
        { status: 400 },
      )
    }

    const newMember: TeamMemberDto = {
      id: crypto.randomUUID(),
      userId: body.userId,
      displayName: body.displayName,
      role: 'SME/Collaborator',
      isCreator: false,
      invitedAt: new Date().toISOString(),
    }
    if (!mockMembers[recruitmentId as string]) {
      mockMembers[recruitmentId as string] = []
    }
    mockMembers[recruitmentId as string].push(newMember)

    return HttpResponse.json(
      { id: newMember.id },
      { status: 201, headers: { Location: `/api/recruitments/${recruitmentId as string}/members/${newMember.id}` } },
    )
  }),

  http.delete('/api/recruitments/:recruitmentId/members/:memberId', ({ params }) => {
    const { recruitmentId, memberId } = params
    const members = mockMembers[recruitmentId as string] ?? []
    const member = members.find(m => m.id === memberId)

    if (!member) {
      return HttpResponse.json(
        { type: 'not-found', title: 'Not Found', status: 404 },
        { status: 404 },
      )
    }

    if (member.isCreator) {
      return HttpResponse.json(
        { type: 'validation', title: 'Bad Request', status: 400, detail: 'Cannot remove the creator of the recruitment.' },
        { status: 400 },
      )
    }

    mockMembers[recruitmentId as string] = members.filter(m => m.id !== memberId)
    return new HttpResponse(null, { status: 204 })
  }),
]
