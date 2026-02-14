import { useMemo } from 'react'
import type { CandidateResponse } from '@/lib/api/candidates.types'

interface UsePdfPrefetchParams {
  candidates: CandidateResponse[]
  currentCandidateId: string | null
  prefetchCount?: number
}

interface UsePdfPrefetchResult {
  getPrefetchedUrl: (candidateId: string) => string | null
}

export function usePdfPrefetch({
  candidates,
  currentCandidateId,
  prefetchCount = 3,
}: UsePdfPrefetchParams): UsePdfPrefetchResult {
  const prefetchedUrls = useMemo(() => {
    const urlMap = new Map<string, string | null>()

    if (!currentCandidateId || candidates.length === 0) return urlMap

    const currentIndex = candidates.findIndex(
      (c) => c.id === currentCandidateId,
    )
    if (currentIndex === -1) return urlMap

    for (
      let i = currentIndex + 1;
      i < Math.min(currentIndex + 1 + prefetchCount, candidates.length);
      i++
    ) {
      const candidate = candidates[i]
      urlMap.set(candidate.id, candidate.documentSasUrl)
    }

    return urlMap
  }, [candidates, currentCandidateId, prefetchCount])

  const getPrefetchedUrl = (candidateId: string): string | null => {
    return prefetchedUrls.get(candidateId) ?? null
  }

  return { getPrefetchedUrl }
}
