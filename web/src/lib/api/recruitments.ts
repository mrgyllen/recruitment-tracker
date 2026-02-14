import { apiDelete, apiGet, apiPost, apiPut } from './httpClient'
import type {
  AddWorkflowStepRequest,
  CreateRecruitmentRequest,
  CreateRecruitmentResponse,
  PaginatedRecruitmentList,
  RecruitmentDetail,
  RecruitmentOverview,
  ReorderStepsRequest,
  UpdateRecruitmentRequest,
  WorkflowStepDto,
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

  update: (id: string, data: UpdateRecruitmentRequest) =>
    apiPut<void>(`/recruitments/${id}`, data),

  addStep: (recruitmentId: string, data: AddWorkflowStepRequest) =>
    apiPost<WorkflowStepDto>(`/recruitments/${recruitmentId}/steps`, data),

  removeStep: (recruitmentId: string, stepId: string) =>
    apiDelete(`/recruitments/${recruitmentId}/steps/${stepId}`),

  reorderSteps: (recruitmentId: string, data: ReorderStepsRequest) =>
    apiPut<void>(`/recruitments/${recruitmentId}/steps/reorder`, data),

  close: (id: string) => apiPost<void>(`/recruitments/${id}/close`),

  getOverview: (id: string) =>
    apiGet<RecruitmentOverview>(`/recruitments/${id}/overview`),
}
