import { useQuery } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

interface UseCandidatesParams {
  recruitmentId: string
  page?: number
  pageSize?: number
  search?: string
  stepId?: string
  outcomeStatus?: string
  staleOnly?: boolean
}

export function useCandidates({
  recruitmentId,
  page = 1,
  pageSize = 50,
  search,
  stepId,
  outcomeStatus,
  staleOnly,
}: UseCandidatesParams) {
  return useQuery({
    queryKey: [
      'candidates',
      recruitmentId,
      { page, pageSize, search, stepId, outcomeStatus, staleOnly },
    ],
    queryFn: () =>
      candidateApi.getAll(
        recruitmentId,
        page,
        pageSize,
        search,
        stepId,
        outcomeStatus,
        staleOnly,
      ),
    enabled: !!recruitmentId,
  })
}
