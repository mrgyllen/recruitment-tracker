import { useQuery } from '@tanstack/react-query'
import { recruitmentApi } from '@/lib/api/recruitments'

export function useRecruitments(page = 1, pageSize = 50) {
  return useQuery({
    queryKey: ['recruitments', page, pageSize],
    queryFn: () => recruitmentApi.getAll(page, pageSize),
  })
}
