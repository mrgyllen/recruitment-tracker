import { useQuery } from '@tanstack/react-query'
import { recruitmentApi } from '@/lib/api/recruitments'

export function useRecruitmentOverview(recruitmentId: string) {
  return useQuery({
    queryKey: ['recruitment', recruitmentId, 'overview'],
    queryFn: () => recruitmentApi.getOverview(recruitmentId),
    enabled: !!recruitmentId,
  })
}
