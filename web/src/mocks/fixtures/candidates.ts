import type { CandidateResponse } from '@/lib/api/candidates.types'

export const mockCandidateId1 = 'cand-1111-1111-1111-111111111111'
export const mockCandidateId2 = 'cand-2222-2222-2222-222222222222'

export const mockCandidates: CandidateResponse[] = [
  {
    id: mockCandidateId1,
    recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
    fullName: 'Alice Johnson',
    email: 'alice@example.com',
    phoneNumber: '+1-555-0101',
    location: 'New York, NY',
    dateApplied: '2026-02-10T00:00:00Z',
    createdAt: '2026-02-10T12:00:00Z',
  },
  {
    id: mockCandidateId2,
    recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
    fullName: 'Bob Smith',
    email: 'bob@example.com',
    phoneNumber: null,
    location: 'San Francisco, CA',
    dateApplied: '2026-02-12T00:00:00Z',
    createdAt: '2026-02-12T09:00:00Z',
  },
]
