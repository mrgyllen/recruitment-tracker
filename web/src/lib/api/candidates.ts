import { apiDelete, apiGet, apiPost } from './httpClient'
import type {
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
}
