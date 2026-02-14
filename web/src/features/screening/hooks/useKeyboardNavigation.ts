import { useEffect, useCallback } from 'react'
import type { OutcomeStatus } from '@/lib/api/screening.types'

const OUTCOME_KEYS: Record<string, OutcomeStatus> = {
  '1': 'Pass',
  '2': 'Fail',
  '3': 'Hold',
}

const TEXT_INPUT_TAGS = new Set(['INPUT', 'TEXTAREA', 'SELECT'])

interface UseKeyboardNavigationOptions {
  outcomePanelRef: React.RefObject<HTMLDivElement | null>
  candidateListRef: React.RefObject<HTMLDivElement | null>
  onOutcomeSelect: (outcome: OutcomeStatus) => void
  selectCandidate: (id: string) => void
  candidates: Array<{ id: string }>
  selectedCandidateId: string | null
  enabled?: boolean
}

interface UseKeyboardNavigationReturn {
  focusOutcomePanel: () => void
}

export function useKeyboardNavigation({
  outcomePanelRef,
  candidateListRef,
  onOutcomeSelect,
  selectCandidate,
  candidates,
  selectedCandidateId,
  enabled = true,
}: UseKeyboardNavigationOptions): UseKeyboardNavigationReturn {
  useEffect(() => {
    if (!enabled) return
    const panel = outcomePanelRef.current
    if (!panel) return

    const handleKeyDown = (e: KeyboardEvent) => {
      const activeTag = (e.target as HTMLElement).tagName
      if (TEXT_INPUT_TAGS.has(activeTag)) return
      if ((e.target as HTMLElement).isContentEditable) return

      const outcome = OUTCOME_KEYS[e.key]
      if (outcome) {
        e.preventDefault()
        onOutcomeSelect(outcome)
      }
    }

    panel.addEventListener('keydown', handleKeyDown)
    return () => panel.removeEventListener('keydown', handleKeyDown)
  }, [outcomePanelRef, onOutcomeSelect, enabled])

  useEffect(() => {
    if (!enabled) return
    const list = candidateListRef.current
    if (!list) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return
      e.preventDefault()

      const currentIndex = candidates.findIndex((c) => c.id === selectedCandidateId)
      if (currentIndex === -1) return

      if (e.key === 'ArrowDown' && currentIndex < candidates.length - 1) {
        selectCandidate(candidates[currentIndex + 1].id)
      } else if (e.key === 'ArrowUp' && currentIndex > 0) {
        selectCandidate(candidates[currentIndex - 1].id)
      }
    }

    list.addEventListener('keydown', handleKeyDown)
    return () => list.removeEventListener('keydown', handleKeyDown)
  }, [candidateListRef, selectCandidate, candidates, selectedCandidateId, enabled])

  const focusOutcomePanel = useCallback(() => {
    requestAnimationFrame(() => {
      outcomePanelRef.current?.focus()
    })
  }, [outcomePanelRef])

  return { focusOutcomePanel }
}
