export interface CandidateResponse {
  id: string
  recruitmentId: string
  fullName: string
  email: string
  phoneNumber: string | null
  location: string | null
  dateApplied: string
  createdAt: string
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
