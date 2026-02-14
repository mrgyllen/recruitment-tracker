import type {
  CandidateDetailResponse,
  CandidateDocumentDto,
  CandidateResponse,
  OutcomeHistoryEntry,
} from '@/lib/api/candidates.types'

export const mockCandidateId1 = 'cand-1111-1111-1111-111111111111'
export const mockCandidateId2 = 'cand-2222-2222-2222-222222222222'
export const mockCandidateId3 = 'cand-3333-3333-3333-333333333333'
export const mockCandidateId4 = 'cand-4444-4444-4444-444444444444'
export const mockDocumentId = 'doc-1111-1111-1111-111111111111'
export const mockStepId1 = 'step-1111-1111-1111-111111111111'
export const mockStepId2 = 'step-2222-2222-2222-222222222222'

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
    documentSasUrl:
      'https://storage.blob.core.windows.net/documents/recruitment-1/cvs/doc-1.pdf?sv=2024&sig=mock',
    currentWorkflowStepId: mockStepId2,
    currentWorkflowStepName: 'Interview',
    currentOutcomeStatus: 'NotStarted',
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
    documentSasUrl: null,
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: 'NotStarted',
  },
]

export const mockOutcomeHistory: OutcomeHistoryEntry[] = [
  {
    workflowStepId: mockStepId1,
    workflowStepName: 'Screening',
    stepOrder: 1,
    status: 'Pass',
    recordedAt: '2026-02-11T10:00:00Z',
    recordedByUserId: 'user-1111-1111-1111-111111111111',
  },
]

export const mockCandidateDetailNoDoc: CandidateDetailResponse = {
  id: mockCandidateId2,
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  fullName: 'Bob Smith',
  email: 'bob@example.com',
  phoneNumber: null,
  location: 'San Francisco, CA',
  dateApplied: '2026-02-12T00:00:00Z',
  createdAt: '2026-02-12T09:00:00Z',
  currentWorkflowStepId: mockStepId1,
  currentWorkflowStepName: 'Screening',
  currentOutcomeStatus: 'NotStarted',
  documents: [],
  outcomeHistory: [],
}

export const mockCandidateDetail: CandidateDetailResponse = {
  id: mockCandidateId1,
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  fullName: 'Alice Johnson',
  email: 'alice@example.com',
  phoneNumber: '+1-555-0101',
  location: 'New York, NY',
  dateApplied: '2026-02-10T00:00:00Z',
  createdAt: '2026-02-10T12:00:00Z',
  currentWorkflowStepId: mockStepId2,
  currentWorkflowStepName: 'Interview',
  currentOutcomeStatus: 'NotStarted',
  documents: [
    {
      id: mockDocumentId,
      documentType: 'CV',
      sasUrl:
        'https://storage.blob.core.windows.net/documents/recruitment-1/cvs/doc-1.pdf?sv=2024&sig=mock',
      uploadedAt: '2026-02-14T12:00:00Z',
    },
  ],
  outcomeHistory: mockOutcomeHistory,
}

/** Screening session fixture: mix of screened and unscreened candidates */
export const mockScreeningCandidates: CandidateResponse[] = [
  {
    id: mockCandidateId1,
    recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
    fullName: 'Alice Johnson',
    email: 'alice@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-02-10T00:00:00Z',
    createdAt: '2026-02-10T12:00:00Z',
    document: mockCandidateDocument,
    documentSasUrl:
      'https://storage.blob.core.windows.net/docs/alice-cv.pdf?sig=mock',
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: 'Pass',
  },
  {
    id: mockCandidateId2,
    recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
    fullName: 'Bob Smith',
    email: 'bob@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-02-11T00:00:00Z',
    createdAt: '2026-02-11T12:00:00Z',
    document: null,
    documentSasUrl: null,
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: null,
  },
  {
    id: mockCandidateId3,
    recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
    fullName: 'Carol White',
    email: 'carol@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-02-12T00:00:00Z',
    createdAt: '2026-02-12T12:00:00Z',
    document: null,
    documentSasUrl:
      'https://storage.blob.core.windows.net/docs/carol-cv.pdf?sig=mock',
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: null,
  },
  {
    id: mockCandidateId4,
    recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
    fullName: 'Dave Brown',
    email: 'dave@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-02-13T00:00:00Z',
    createdAt: '2026-02-13T12:00:00Z',
    document: null,
    documentSasUrl: null,
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: 'Fail',
  },
]
