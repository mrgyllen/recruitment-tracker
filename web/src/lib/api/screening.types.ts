export type OutcomeStatus = 'NotStarted' | 'Pass' | 'Fail' | 'Hold'

export interface RecordOutcomeRequest {
  workflowStepId: string
  outcome: OutcomeStatus
  reason?: string
}

export interface OutcomeResultDto {
  outcomeId: string
  candidateId: string
  workflowStepId: string
  outcome: OutcomeStatus
  reason: string | null
  recordedAt: string
  recordedBy: string
  newCurrentStepId: string | null
  isCompleted: boolean
}

export interface OutcomeHistoryDto {
  workflowStepId: string
  workflowStepName: string
  stepOrder: number
  outcome: OutcomeStatus
  reason: string | null
  recordedAt: string
  recordedByUserId: string
}
