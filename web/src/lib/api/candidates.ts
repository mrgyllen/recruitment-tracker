import { apiDelete, apiGet, apiPost, apiPostFormData } from './httpClient'
import type {
  AssignDocumentRequest,
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

  getAll: (recruitmentId: string, page = 1, pageSize = 50) =>
    apiGet<PaginatedCandidateList>(
      `/recruitments/${recruitmentId}/candidates?page=${page}&pageSize=${pageSize}`,
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
