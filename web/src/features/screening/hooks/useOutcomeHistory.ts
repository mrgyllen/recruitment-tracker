import { useQuery } from '@tanstack/react-query'
import { screeningApi } from '@/lib/api/screening'

export function useOutcomeHistory(recruitmentId: string, candidateId: string) {
  return useQuery({
    queryKey: ['screening', 'history', candidateId],
    queryFn: () => screeningApi.getOutcomeHistory(recruitmentId, candidateId),
    enabled: !!recruitmentId && !!candidateId,
  })
}
