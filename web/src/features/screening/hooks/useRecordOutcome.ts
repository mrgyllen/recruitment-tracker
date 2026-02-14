import { useMutation, useQueryClient } from '@tanstack/react-query'
import { screeningApi } from '@/lib/api/screening'
import type { RecordOutcomeRequest } from '@/lib/api/screening.types'

export function useRecordOutcome() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      recruitmentId,
      candidateId,
      data,
    }: {
      recruitmentId: string
      candidateId: string
      data: RecordOutcomeRequest
    }) => screeningApi.recordOutcome(recruitmentId, candidateId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: ['screening', 'history', variables.candidateId],
      })
      queryClient.invalidateQueries({
        queryKey: ['candidates', variables.recruitmentId],
      })
    },
  })
}
