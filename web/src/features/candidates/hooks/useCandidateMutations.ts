import { useMutation, useQueryClient } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'
import type { CreateCandidateRequest } from '@/lib/api/candidates.types'

export function useCreateCandidate(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateCandidateRequest) =>
      candidateApi.create(recruitmentId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['candidates', recruitmentId],
      })
    },
  })
}

export function useRemoveCandidate(recruitmentId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (candidateId: string) =>
      candidateApi.remove(recruitmentId, candidateId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['candidates', recruitmentId],
      })
    },
  })
}
