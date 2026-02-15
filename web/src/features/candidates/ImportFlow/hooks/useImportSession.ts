import { useQuery } from '@tanstack/react-query'
import type { ImportSessionResponse } from '@/lib/api/import.types'
import { importApi } from '@/lib/api/import'

const POLL_INTERVAL_MS = 2000

export function useImportSession(importSessionId: string | null) {
  return useQuery<ImportSessionResponse>({
    queryKey: ['import-session', importSessionId],
    queryFn: () => importApi.getSession(importSessionId!),
    enabled: !!importSessionId,
    refetchInterval: (query) => {
      const data = query.state.data
      if (!data) return POLL_INTERVAL_MS
      if (data.status === 'Completed' || data.status === 'Failed') {
        return false
      }
      return POLL_INTERVAL_MS
    },
  })
}
