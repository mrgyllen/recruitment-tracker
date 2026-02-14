import { useCallback, useEffect, useState } from 'react'
import { candidateApi } from '@/lib/api/candidates'

interface UseSasUrlParams {
  initialUrl: string | null
  recruitmentId: string
  candidateId: string
}

interface UseSasUrlResult {
  url: string | null
  isRefreshing: boolean
  refresh: () => Promise<void>
}

export function useSasUrl({
  initialUrl,
  recruitmentId,
  candidateId,
}: UseSasUrlParams): UseSasUrlResult {
  const [url, setUrl] = useState<string | null>(initialUrl)
  const [isRefreshing, setIsRefreshing] = useState(false)

  useEffect(() => {
    setUrl(initialUrl)
  }, [initialUrl])

  const refresh = useCallback(async () => {
    setIsRefreshing(true)
    try {
      const detail = await candidateApi.getById(recruitmentId, candidateId)
      const freshUrl = detail.documents[0]?.sasUrl ?? null
      setUrl(freshUrl)
    } catch {
      // Keep existing URL on refresh failure
    } finally {
      setIsRefreshing(false)
    }
  }, [recruitmentId, candidateId])

  return { url, isRefreshing, refresh }
}
