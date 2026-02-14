export interface CandidateResponse {
  id: string
  recruitmentId: string
  fullName: string
  email: string
  phoneNumber: string | null
  location: string | null
  dateApplied: string
  createdAt: string
  document: CandidateDocumentDto | null
  documentSasUrl: string | null
  currentWorkflowStepId: string | null
  currentWorkflowStepName: string | null
  currentOutcomeStatus: string | null
}

export interface CreateCandidateRequest {
  fullName: string
  email: string
  phoneNumber?: string | null
  location?: string | null
  dateApplied?: string | null
}

export interface PaginatedCandidateList {
  items: CandidateResponse[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CandidateDocumentDto {
  id: string
  candidateId: string
  documentType: string
  blobStorageUrl: string
  uploadedAt: string
}

export interface AssignDocumentRequest {
  documentBlobUrl: string
  documentName: string
  importSessionId?: string
}

export interface CandidateDetailResponse {
  id: string
  recruitmentId: string
  fullName: string
  email: string
  phoneNumber: string | null
  location: string | null
  dateApplied: string
  createdAt: string
  currentWorkflowStepId: string | null
  currentWorkflowStepName: string | null
  currentOutcomeStatus: string | null
  documents: DocumentDetailDto[]
  outcomeHistory: OutcomeHistoryEntry[]
}

export interface DocumentDetailDto {
  id: string
  documentType: string
  sasUrl: string
  uploadedAt: string
}

export interface OutcomeHistoryEntry {
  workflowStepId: string
  workflowStepName: string
  stepOrder: number
  status: string
  recordedAt: string
  recordedByUserId: string
}
