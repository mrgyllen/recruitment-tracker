import { useQuery } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

export function useCandidates(recruitmentId: string) {
  return useQuery({
    queryKey: ['candidates', recruitmentId],
    queryFn: () => candidateApi.getAll(recruitmentId),
    enabled: !!recruitmentId,
  })
}
