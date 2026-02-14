import { useMutation, useQueryClient } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

export function useDocumentUpload(
  recruitmentId: string,
  candidateId: string,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file: File) =>
      candidateApi.uploadDocument(recruitmentId, candidateId, file),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['candidates', recruitmentId],
      })
    },
  })
}
