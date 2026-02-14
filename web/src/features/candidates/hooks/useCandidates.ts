import { useQuery } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

interface UseCandidatesParams {
  recruitmentId: string
  page?: number
  pageSize?: number
  search?: string
  stepId?: string
  outcomeStatus?: string
}

export function useCandidates({
  recruitmentId,
  page = 1,
  pageSize = 50,
  search,
  stepId,
  outcomeStatus,
}: UseCandidatesParams) {
  return useQuery({
    queryKey: [
      'candidates',
      recruitmentId,
      { page, search, stepId, outcomeStatus },
    ],
    queryFn: () =>
      candidateApi.getAll(
        recruitmentId,
        page,
        pageSize,
        search,
        stepId,
        outcomeStatus,
      ),
    enabled: !!recruitmentId,
  })
}
