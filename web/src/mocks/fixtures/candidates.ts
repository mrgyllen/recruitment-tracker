import type {
  CandidateDocumentDto,
  CandidateResponse,
} from '@/lib/api/candidates.types'

export const mockCandidateId1 = 'cand-1111-1111-1111-111111111111'
export const mockCandidateId2 = 'cand-2222-2222-2222-222222222222'
export const mockDocumentId = 'doc-1111-1111-1111-111111111111'

export const mockCandidateDocument: CandidateDocumentDto = {
  id: mockDocumentId,
  candidateId: mockCandidateId1,
  documentType: 'CV',
  blobStorageUrl: 'recruitment-1/cvs/doc-1.pdf',
  uploadedAt: '2026-02-14T12:00:00Z',
}

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
    document: mockCandidateDocument,
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
    document: null,
  },
]
