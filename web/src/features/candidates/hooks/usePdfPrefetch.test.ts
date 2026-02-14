import { describe, expect, it } from 'vitest'
import { renderHook } from '@testing-library/react'
import { usePdfPrefetch } from './usePdfPrefetch'
import type { CandidateResponse } from '@/lib/api/candidates.types'
import { mockStepId1, mockStepId2 } from '@/mocks/fixtures/candidates'

const mockCandidateList: CandidateResponse[] = [
  {
    id: 'cand-1',
    recruitmentId: 'rec-1',
    fullName: 'Alice',
    email: 'a@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-01-01',
    createdAt: '2026-01-01',
    document: null,
    documentSasUrl:
      'https://storage.blob.core.windows.net/alice.pdf?sig=mock',
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: 'NotStarted',
  },
  {
    id: 'cand-2',
    recruitmentId: 'rec-1',
    fullName: 'Bob',
    email: 'b@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-01-02',
    createdAt: '2026-01-02',
    document: null,
    documentSasUrl: 'https://storage.blob.core.windows.net/bob.pdf?sig=mock',
    currentWorkflowStepId: mockStepId2,
    currentWorkflowStepName: 'Interview',
    currentOutcomeStatus: 'NotStarted',
  },
  {
    id: 'cand-3',
    recruitmentId: 'rec-1',
    fullName: 'Charlie',
    email: 'c@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-01-03',
    createdAt: '2026-01-03',
    document: null,
    documentSasUrl: null,
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: 'Pass',
  },
  {
    id: 'cand-4',
    recruitmentId: 'rec-1',
    fullName: 'Diana',
    email: 'd@example.com',
    phoneNumber: null,
    location: null,
    dateApplied: '2026-01-04',
    createdAt: '2026-01-04',
    document: null,
    documentSasUrl:
      'https://storage.blob.core.windows.net/diana.pdf?sig=mock',
    currentWorkflowStepId: mockStepId1,
    currentWorkflowStepName: 'Screening',
    currentOutcomeStatus: 'NotStarted',
  },
]

describe('usePdfPrefetch', () => {
  it('should return prefetched SAS URLs for adjacent candidates', () => {
    const { result } = renderHook(() =>
      usePdfPrefetch({
        candidates: mockCandidateList,
        currentCandidateId: 'cand-1',
        prefetchCount: 2,
      }),
    )

    expect(result.current.getPrefetchedUrl('cand-2')).toBe(
      'https://storage.blob.core.windows.net/bob.pdf?sig=mock',
    )
  })

  it('should return null for candidates without SAS URLs', () => {
    const { result } = renderHook(() =>
      usePdfPrefetch({
        candidates: mockCandidateList,
        currentCandidateId: 'cand-1',
        prefetchCount: 3,
      }),
    )

    expect(result.current.getPrefetchedUrl('cand-3')).toBeNull()
  })

  it('should return null for candidates outside the prefetch window', () => {
    const { result } = renderHook(() =>
      usePdfPrefetch({
        candidates: mockCandidateList,
        currentCandidateId: 'cand-1',
        prefetchCount: 1,
      }),
    )

    // cand-3 is at index 2, only 1 ahead is prefetched (cand-2)
    expect(result.current.getPrefetchedUrl('cand-3')).toBeNull()
    // cand-4 is even further
    expect(result.current.getPrefetchedUrl('cand-4')).toBeNull()
  })

  it('should return null for candidates before the current one', () => {
    const { result } = renderHook(() =>
      usePdfPrefetch({
        candidates: mockCandidateList,
        currentCandidateId: 'cand-2',
        prefetchCount: 3,
      }),
    )

    expect(result.current.getPrefetchedUrl('cand-1')).toBeNull()
  })

  it('should return null when no current candidate is set', () => {
    const { result } = renderHook(() =>
      usePdfPrefetch({
        candidates: mockCandidateList,
        currentCandidateId: null,
        prefetchCount: 3,
      }),
    )

    expect(result.current.getPrefetchedUrl('cand-1')).toBeNull()
  })

  it('should default prefetchCount to 3', () => {
    const { result } = renderHook(() =>
      usePdfPrefetch({
        candidates: mockCandidateList,
        currentCandidateId: 'cand-1',
      }),
    )

    // With default prefetchCount=3, cand-2, cand-3, cand-4 are all in window
    expect(result.current.getPrefetchedUrl('cand-2')).toBe(
      'https://storage.blob.core.windows.net/bob.pdf?sig=mock',
    )
    expect(result.current.getPrefetchedUrl('cand-4')).toBe(
      'https://storage.blob.core.windows.net/diana.pdf?sig=mock',
    )
  })
})
