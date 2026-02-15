import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { useRecruitmentOverview } from './useRecruitmentOverview'
import type { ReactNode } from 'react'
import {
  mockOverviewData,
  mockRecruitmentId,
} from '@/mocks/recruitmentHandlers'
import { server } from '@/mocks/server'

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    )
  }
}

describe('useRecruitmentOverview', () => {
  it('should fetch overview data for recruitment', async () => {
    const { result } = renderHook(
      () => useRecruitmentOverview(mockRecruitmentId),
      { wrapper: createWrapper() },
    )

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.totalCandidates).toBe(
      mockOverviewData.totalCandidates,
    )
    expect(result.current.data?.steps).toHaveLength(3)
    expect(result.current.data?.pendingActionCount).toBe(
      mockOverviewData.pendingActionCount,
    )
  })

  it('should return loading state initially', () => {
    const { result } = renderHook(
      () => useRecruitmentOverview(mockRecruitmentId),
      { wrapper: createWrapper() },
    )

    expect(result.current.isPending).toBe(true)
    expect(result.current.data).toBeUndefined()
  })

  it('should return error state on API failure', async () => {
    server.use(
      http.get('/api/recruitments/:id/overview', () => {
        return HttpResponse.json(
          { title: 'Internal Server Error', status: 500 },
          { status: 500 },
        )
      }),
    )

    const { result } = renderHook(
      () => useRecruitmentOverview(mockRecruitmentId),
      { wrapper: createWrapper() },
    )

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})
