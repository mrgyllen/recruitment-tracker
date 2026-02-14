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
