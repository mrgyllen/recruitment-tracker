import { useQuery } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

export function useCandidateById(
  recruitmentId: string,
  candidateId: string,
) {
  return useQuery({
    queryKey: ['candidate', recruitmentId, candidateId],
    queryFn: () => candidateApi.getById(recruitmentId, candidateId),
    enabled: !!recruitmentId && !!candidateId,
  })
}
