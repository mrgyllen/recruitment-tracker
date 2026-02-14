import { useState, useCallback, useRef } from 'react'
import { toast } from 'sonner'
import type { CandidateResponse } from '@/lib/api/candidates.types'
import type { OutcomeResultDto } from '@/lib/api/screening.types'

const AUTO_ADVANCE_DELAY_MS = 300

interface PendingOutcome {
  candidateId: string
  toastId: string | number
}

interface UseScreeningSessionOptions {
  onAutoAdvance?: () => void
}

export function useScreeningSession(
  _recruitmentId: string,
  candidates: CandidateResponse[],
  options?: UseScreeningSessionOptions,
) {
  const [selectedCandidateId, setSelectedCandidateId] = useState<string | null>(null)
  const [sessionScreenedCount, setSessionScreenedCount] = useState(0)
  const pendingRef = useRef<PendingOutcome | null>(null)
  const autoAdvanceRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const recentlyScreenedRef = useRef<Set<string>>(new Set())
  const onAutoAdvanceRef = useRef(options?.onAutoAdvance)
  onAutoAdvanceRef.current = options?.onAutoAdvance

  const selectedCandidate = candidates.find((c) => c.id === selectedCandidateId) ?? null

  const totalScreenedCount = candidates.filter(
    (c) => c.currentOutcomeStatus && c.currentOutcomeStatus !== 'NotStarted',
  ).length

  const isAllScreened = totalScreenedCount === candidates.length && candidates.length > 0

  const selectCandidate = useCallback((id: string) => {
    if (autoAdvanceRef.current) {
      clearTimeout(autoAdvanceRef.current)
      autoAdvanceRef.current = null
    }
    setSelectedCandidateId(id)
  }, [])

  const findNextUnscreened = useCallback(
    (currentId: string): string | null => {
      const currentIndex = candidates.findIndex((c) => c.id === currentId)
      if (currentIndex === -1) return null

      const isUnscreened = (c: CandidateResponse) =>
        (!c.currentOutcomeStatus || c.currentOutcomeStatus === 'NotStarted') &&
        !recentlyScreenedRef.current.has(c.id)

      for (let i = currentIndex + 1; i < candidates.length; i++) {
        if (isUnscreened(candidates[i])) return candidates[i].id
      }
      for (let i = 0; i < currentIndex; i++) {
        if (isUnscreened(candidates[i])) return candidates[i].id
      }
      return null
    },
    [candidates],
  )

  const undoOutcome = useCallback(() => {
    const pending = pendingRef.current
    if (!pending) return

    if (autoAdvanceRef.current) {
      clearTimeout(autoAdvanceRef.current)
      autoAdvanceRef.current = null
    }

    recentlyScreenedRef.current.delete(pending.candidateId)
    toast.dismiss(pending.toastId)
    pendingRef.current = null
    setSelectedCandidateId(pending.candidateId)
    setSessionScreenedCount((prev) => Math.max(0, prev - 1))
  }, [])

  const handleOutcomeRecorded = useCallback(
    (result: OutcomeResultDto) => {
      const candidate = candidates.find((c) => c.id === result.candidateId)
      if (!candidate) return

      recentlyScreenedRef.current.add(result.candidateId)
      setSessionScreenedCount((prev) => prev + 1)

      const toastId = toast(`${result.outcome} recorded for ${candidate.fullName}`, {
        action: {
          label: 'Undo',
          onClick: () => undoOutcome(),
        },
        duration: 5000,
      })

      pendingRef.current = {
        candidateId: result.candidateId,
        toastId: toastId as string | number,
      }

      autoAdvanceRef.current = setTimeout(() => {
        const nextId = findNextUnscreened(result.candidateId)
        if (nextId) {
          setSelectedCandidateId(nextId)
        }
        autoAdvanceRef.current = null
        onAutoAdvanceRef.current?.()
      }, AUTO_ADVANCE_DELAY_MS)
    },
    [candidates, findNextUnscreened, undoOutcome],
  )

  return {
    selectedCandidateId,
    selectedCandidate,
    sessionScreenedCount,
    totalScreenedCount,
    isAllScreened,
    selectCandidate,
    handleOutcomeRecorded,
    undoOutcome,
  }
}
