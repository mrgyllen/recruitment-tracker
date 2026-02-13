import { apiGet, apiPost } from './httpClient'
import type {
  CreateRecruitmentRequest,
  CreateRecruitmentResponse,
  PaginatedRecruitmentList,
  RecruitmentDetail,
} from './recruitments.types'

export const recruitmentApi = {
  create: (data: CreateRecruitmentRequest) =>
    apiPost<CreateRecruitmentResponse>('/recruitments', data),

  getById: (id: string) =>
    apiGet<RecruitmentDetail>(`/recruitments/${id}`),

  getAll: (page = 1, pageSize = 50) =>
    apiGet<PaginatedRecruitmentList>(
      `/recruitments?page=${page}&pageSize=${pageSize}`,
    ),
}
