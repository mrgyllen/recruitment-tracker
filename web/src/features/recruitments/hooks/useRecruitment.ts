import { useQuery } from '@tanstack/react-query'
import { recruitmentApi } from '@/lib/api/recruitments'

export function useRecruitment(id: string) {
  return useQuery({
    queryKey: ['recruitment', id],
    queryFn: () => recruitmentApi.getById(id),
    enabled: !!id,
  })
}
