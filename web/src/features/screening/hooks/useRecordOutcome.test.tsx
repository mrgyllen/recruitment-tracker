import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { useRecordOutcome } from './useRecordOutcome'
import type { ReactNode } from 'react'

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return {
    Wrapper: ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    ),
    queryClient,
  }
}

describe('useRecordOutcome', () => {
  it('should invalidate overview query when outcome is recorded', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useRecordOutcome(), {
      wrapper: Wrapper,
    })

    await act(async () => {
      result.current.mutate({
        recruitmentId: 'rec-1',
        candidateId: 'cand-1',
        data: { workflowStepId: 'step-1', status: 'Pass', notes: null },
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(invalidateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: ['recruitment', 'rec-1', 'overview'],
      }),
    )
  })
})
