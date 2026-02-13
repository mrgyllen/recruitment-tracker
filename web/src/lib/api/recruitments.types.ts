export interface WorkflowStepDto {
  id: string
  name: string
  order: number
}

export interface MemberDto {
  id: string
  userId: string
  role: string
}

export interface RecruitmentDetail {
  id: string
  title: string
  description: string | null
  jobRequisitionId: string | null
  status: string
  createdAt: string
  closedAt: string | null
  createdByUserId: string
  steps: WorkflowStepDto[]
  members: MemberDto[]
}

export interface RecruitmentListItem {
  id: string
  title: string
  description: string | null
  status: string
  createdAt: string
  closedAt: string | null
  stepCount: number
  memberCount: number
}

export interface PaginatedRecruitmentList {
  items: RecruitmentListItem[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CreateRecruitmentRequest {
  title: string
  description?: string | null
  jobRequisitionId?: string | null
  steps: CreateWorkflowStepRequest[]
}

export interface CreateWorkflowStepRequest {
  name: string
  order: number
}

export interface CreateRecruitmentResponse {
  id: string
}
