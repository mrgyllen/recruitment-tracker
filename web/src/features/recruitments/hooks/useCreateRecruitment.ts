import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { CreateRecruitmentRequest } from '@/lib/api/recruitments.types'
import { recruitmentApi } from '@/lib/api/recruitments'

export function useCreateRecruitment() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateRecruitmentRequest) => recruitmentApi.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['recruitments'] })
    },
  })
}
