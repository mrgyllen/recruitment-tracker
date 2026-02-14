import { describe, expect, it, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement, type ReactNode } from 'react'
import { useSasUrl } from './useSasUrl'

vi.mock('@/lib/api/candidates', () => ({
  candidateApi: {
    getById: vi.fn(),
  },
}))

import { candidateApi } from '@/lib/api/candidates'

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return ({ children }: { children: ReactNode }) =>
    createElement(QueryClientProvider, { client: queryClient }, children)
}

describe('useSasUrl', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should return the initial SAS URL when provided', () => {
    const { result } = renderHook(
      () =>
        useSasUrl({
          initialUrl:
            'https://storage.blob.core.windows.net/doc.pdf?sig=valid',
          recruitmentId: 'rec-1',
          candidateId: 'cand-1',
        }),
      { wrapper: createWrapper() },
    )

    expect(result.current.url).toBe(
      'https://storage.blob.core.windows.net/doc.pdf?sig=valid',
    )
    expect(result.current.isRefreshing).toBe(false)
  })

  it('should return null when initialUrl is null', () => {
    const { result } = renderHook(
      () =>
        useSasUrl({
          initialUrl: null,
          recruitmentId: 'rec-1',
          candidateId: 'cand-1',
        }),
      { wrapper: createWrapper() },
    )

    expect(result.current.url).toBeNull()
  })

  it('should refresh URL when refresh is called', async () => {
    const mockGetById = vi.mocked(candidateApi.getById)
    mockGetById.mockResolvedValue({
      id: 'cand-1',
      recruitmentId: 'rec-1',
      fullName: 'Test',
      email: 'test@example.com',
      phoneNumber: null,
      location: null,
      dateApplied: '2026-01-01',
      createdAt: '2026-01-01',
      currentWorkflowStepId: null,
      currentWorkflowStepName: null,
      currentOutcomeStatus: null,
      documents: [
        {
          id: 'doc-1',
          documentType: 'CV',
          sasUrl:
            'https://storage.blob.core.windows.net/doc.pdf?sig=refreshed',
          uploadedAt: '2026-01-01',
        },
      ],
      outcomeHistory: [],
    })

    const { result } = renderHook(
      () =>
        useSasUrl({
          initialUrl:
            'https://storage.blob.core.windows.net/doc.pdf?sig=expired',
          recruitmentId: 'rec-1',
          candidateId: 'cand-1',
        }),
      { wrapper: createWrapper() },
    )

    await act(() => result.current.refresh())

    await waitFor(() => {
      expect(result.current.url).toBe(
        'https://storage.blob.core.windows.net/doc.pdf?sig=refreshed',
      )
    })

    expect(mockGetById).toHaveBeenCalledWith('rec-1', 'cand-1')
  })

  it('should keep existing URL when refresh fails', async () => {
    const mockGetById = vi.mocked(candidateApi.getById)
    mockGetById.mockRejectedValue(new Error('Network error'))

    const { result } = renderHook(
      () =>
        useSasUrl({
          initialUrl:
            'https://storage.blob.core.windows.net/doc.pdf?sig=original',
          recruitmentId: 'rec-1',
          candidateId: 'cand-1',
        }),
      { wrapper: createWrapper() },
    )

    await act(() => result.current.refresh())

    await waitFor(() => {
      expect(result.current.isRefreshing).toBe(false)
    })

    expect(result.current.url).toBe(
      'https://storage.blob.core.windows.net/doc.pdf?sig=original',
    )
  })
})
