import { apiDelete, apiGet, apiPost, apiPostFormData } from './httpClient'
import type {
  AssignDocumentRequest,
  CandidateDetailResponse,
  CandidateDocumentDto,
  CreateCandidateRequest,
  PaginatedCandidateList,
} from './candidates.types'

export const candidateApi = {
  create: (recruitmentId: string, data: CreateCandidateRequest) =>
    apiPost<{ id: string }>(
      `/recruitments/${recruitmentId}/candidates`,
      data,
    ),

  remove: (recruitmentId: string, candidateId: string) =>
    apiDelete(`/recruitments/${recruitmentId}/candidates/${candidateId}`),

  getAll: (
    recruitmentId: string,
    page = 1,
    pageSize = 50,
    search?: string,
    stepId?: string,
    outcomeStatus?: string,
  ) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    })
    if (search) params.set('search', search)
    if (stepId) params.set('stepId', stepId)
    if (outcomeStatus) params.set('outcomeStatus', outcomeStatus)
    return apiGet<PaginatedCandidateList>(
      `/recruitments/${recruitmentId}/candidates?${params}`,
    )
  },

  getById: (recruitmentId: string, candidateId: string) =>
    apiGet<CandidateDetailResponse>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}`,
    ),

  uploadDocument: (recruitmentId: string, candidateId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiPostFormData<CandidateDocumentDto>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/document`,
      formData,
    )
  },

  assignDocument: (
    recruitmentId: string,
    candidateId: string,
    data: AssignDocumentRequest,
  ) =>
    apiPost<CandidateDocumentDto>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/document/assign`,
      data,
    ),
}
