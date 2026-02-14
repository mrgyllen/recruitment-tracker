import { apiGet, apiPost } from './httpClient'
import type {
  RecordOutcomeRequest,
  OutcomeResultDto,
  OutcomeHistoryDto,
} from './screening.types'

export const screeningApi = {
  recordOutcome: (
    recruitmentId: string,
    candidateId: string,
    data: RecordOutcomeRequest,
  ) =>
    apiPost<OutcomeResultDto>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/screening/outcome`,
      data,
    ),

  getOutcomeHistory: (recruitmentId: string, candidateId: string) =>
    apiGet<OutcomeHistoryDto[]>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/screening/history`,
    ),
}
