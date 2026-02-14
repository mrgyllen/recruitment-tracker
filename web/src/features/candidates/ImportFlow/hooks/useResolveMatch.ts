import { useMutation, useQueryClient } from '@tanstack/react-query'
import { importApi } from '@/lib/api/import'
import type { ResolveMatchRequest } from '@/lib/api/import.types'

export function useResolveMatch(importSessionId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ResolveMatchRequest) =>
      importApi.resolveMatch(importSessionId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['import-session', importSessionId],
      })
    },
  })
}
