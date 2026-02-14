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

export interface UpdateRecruitmentRequest {
  title: string
  description?: string | null
  jobRequisitionId?: string | null
}

export interface AddWorkflowStepRequest {
  name: string
  order: number
}

export interface ReorderStepsRequest {
  steps: { stepId: string; order: number }[]
}

export interface OutcomeBreakdown {
  notStarted: number
  pass: number
  fail: number
  hold: number
}

export interface StepOverview {
  stepId: string
  stepName: string
  stepOrder: number
  totalCandidates: number
  pendingCount: number
  staleCount: number
  outcomeBreakdown: OutcomeBreakdown
}

export interface RecruitmentOverview {
  recruitmentId: string
  totalCandidates: number
  pendingActionCount: number
  totalStale: number
  staleDays: number
  steps: StepOverview[]
}
