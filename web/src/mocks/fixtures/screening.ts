import type {
  OutcomeResultDto,
  OutcomeHistoryDto,
} from '@/lib/api/screening.types'
import { mockCandidateId1, mockStepId1, mockStepId2 } from './candidates'

export const mockOutcomeResult: OutcomeResultDto = {
  outcomeId: 'outcome-1111-1111-1111-111111111111',
  candidateId: mockCandidateId1,
  workflowStepId: mockStepId1,
  outcome: 'Pass',
  reason: 'Strong technical skills',
  recordedAt: '2026-02-14T14:00:00Z',
  recordedBy: 'user-1111-1111-1111-111111111111',
  newCurrentStepId: mockStepId2,
  isCompleted: false,
}

export const mockOutcomeHistoryList: OutcomeHistoryDto[] = [
  {
    workflowStepId: mockStepId1,
    workflowStepName: 'Screening',
    stepOrder: 1,
    outcome: 'Pass',
    reason: 'Strong technical skills',
    recordedAt: '2026-02-14T14:00:00Z',
    recordedByUserId: 'user-1111-1111-1111-111111111111',
  },
]
