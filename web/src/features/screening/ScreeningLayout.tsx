import { useRef, useState, useEffect, useCallback } from 'react'
import { useParams } from 'react-router'
import { useRecruitment } from '@/features/recruitments/hooks/useRecruitment'
import { useCandidates } from '@/features/candidates/hooks/useCandidates'
import { usePdfPrefetch } from '@/features/candidates/hooks/usePdfPrefetch'
import { PdfViewer } from '@/features/candidates/PdfViewer'
import { useResizablePanel } from './hooks/useResizablePanel'
import { useScreeningSession } from './hooks/useScreeningSession'
import { useKeyboardNavigation } from './hooks/useKeyboardNavigation'
import { CandidatePanel } from './CandidatePanel'
import { OutcomeForm } from './OutcomeForm'
import { EmptyState } from '@/components/EmptyState'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import type { OutcomeStatus, OutcomeResultDto } from '@/lib/api/screening.types'

export function ScreeningLayout() {
  const { recruitmentId } = useParams<{ recruitmentId: string }>()
  const { data: recruitment, isLoading: recruitmentLoading } = useRecruitment(recruitmentId!)
  const { data: candidateData, isLoading: candidatesLoading } = useCandidates({
    recruitmentId: recruitmentId!,
    pageSize: 50,
  })

  const candidates = candidateData?.items ?? []

  const panel = useResizablePanel({
    storageKey: recruitmentId!,
    defaultRatio: 0.25,
    minLeftPx: 250,
    minCenterPx: 300,
  })

  const outcomePanelRef = useRef<HTMLDivElement>(null!)
  const candidateListRef = useRef<HTMLDivElement>(null!)
  const [keyboardOutcome, setKeyboardOutcome] = useState<OutcomeStatus | null>(null)
  const [candidateAnnouncement, setCandidateAnnouncement] = useState('')
  const [outcomeAnnouncement, setOutcomeAnnouncement] = useState('')

  const session = useScreeningSession(recruitmentId!, candidates, {
    onAutoAdvance: () => {
      requestAnimationFrame(() => {
        outcomePanelRef.current?.focus()
      })
    },
  })

  const { focusOutcomePanel } = useKeyboardNavigation({
    outcomePanelRef,
    candidateListRef,
    onOutcomeSelect: setKeyboardOutcome,
    selectCandidate: session.selectCandidate,
    candidates,
    selectedCandidateId: session.selectedCandidateId,
    enabled: !!session.selectedCandidateId,
  })

  const prefetch = usePdfPrefetch({
    candidates,
    currentCandidateId: session.selectedCandidateId,
  })

  const isClosed = recruitment?.status === 'Closed'
  const selectedCandidate = session.selectedCandidate
  const documentUrl = selectedCandidate
    ? prefetch.getPrefetchedUrl(selectedCandidate.id) ?? selectedCandidate.documentSasUrl
    : null

  // ARIA announcement on candidate switch
  useEffect(() => {
    if (selectedCandidate) {
      setCandidateAnnouncement(
        `Now reviewing ${selectedCandidate.fullName} at ${selectedCandidate.currentWorkflowStepName ?? 'unknown step'}`,
      )
    }
  }, [selectedCandidate])

  // Reset keyboard outcome when candidate changes
  useEffect(() => {
    setKeyboardOutcome(null)
  }, [session.selectedCandidateId])

  const handleOutcomeWithAnnouncement = useCallback(
    (result: OutcomeResultDto) => {
      const candidate = candidates.find((c) => c.id === result.candidateId)
      if (candidate) {
        setOutcomeAnnouncement(`${result.outcome} recorded for ${candidate.fullName}`)
      }
      setKeyboardOutcome(null)
      session.handleOutcomeRecorded(result)
    },
    [candidates, session.handleOutcomeRecorded],
  )

  if (recruitmentLoading || candidatesLoading) {
    return <SkeletonLoader variant="card" />
  }

  return (
    <div
      ref={panel.containerRef}
      className="flex h-[calc(100vh-4rem)]"
      style={{ userSelect: panel.isDragging ? 'none' : 'auto' }}
    >
      {/* Left Panel: Candidate List */}
      <div
        style={{ width: panel.leftWidth, flexShrink: 0 }}
        className="overflow-hidden border-r focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
        role="region"
        aria-label="Candidate list"
      >
        <CandidatePanel
          recruitmentId={recruitmentId!}
          selectedCandidateId={session.selectedCandidateId}
          onCandidateSelect={session.selectCandidate}
          sessionScreenedCount={session.sessionScreenedCount}
          totalScreenedCount={session.totalScreenedCount}
          totalCandidateCount={candidates.length}
          isAllScreened={session.isAllScreened}
          isClosed={isClosed ?? false}
          workflowSteps={recruitment?.steps ?? []}
          candidateListRef={candidateListRef}
        />
      </div>

      {/* Resizable Divider */}
      <div
        {...panel.dividerProps}
        className="flex-shrink-0 bg-gray-200 transition-colors hover:bg-blue-400"
        role="separator"
        aria-orientation="vertical"
        aria-label="Resize candidate list"
      />

      {/* Center Panel: PDF Viewer */}
      <div
        style={{ width: panel.centerWidth, flexShrink: 0 }}
        className="overflow-hidden"
        role="region"
        aria-label="CV viewer"
      >
        {selectedCandidate ? (
          <PdfViewer url={documentUrl} />
        ) : (
          <EmptyState
            heading="Select a candidate"
            description="Choose a candidate from the list to review their CV."
          />
        )}
      </div>

      {/* Right Panel: Outcome Controls */}
      <div
        ref={outcomePanelRef}
        tabIndex={0}
        className="w-[300px] flex-shrink-0 overflow-y-auto border-l focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
        role="region"
        aria-label="Outcome controls"
      >
        {selectedCandidate ? (
          <div className="flex h-full flex-col">
            <div className="border-b p-4">
              <h2 className="truncate text-lg font-semibold">{selectedCandidate.fullName}</h2>
              <p className="text-sm text-gray-500">
                {selectedCandidate.currentWorkflowStepName ?? 'No step assigned'}
              </p>
            </div>
            <div className="flex-1 p-4">
              <OutcomeForm
                key={selectedCandidate.id}
                recruitmentId={recruitmentId!}
                candidateId={selectedCandidate.id}
                currentStepId={selectedCandidate.currentWorkflowStepId ?? ''}
                currentStepName={selectedCandidate.currentWorkflowStepName ?? 'Unknown'}
                existingOutcome={null}
                isClosed={isClosed ?? false}
                onOutcomeRecorded={handleOutcomeWithAnnouncement}
                externalOutcome={keyboardOutcome}
              />
            </div>
          </div>
        ) : (
          <EmptyState
            heading="Select a candidate"
            description="Choose a candidate from the list to record an outcome."
          />
        )}
      </div>

      {/* ARIA Live Regions */}
      <div aria-live="polite" aria-atomic="true" className="sr-only">
        {candidateAnnouncement}
      </div>
      <div aria-live="assertive" aria-atomic="true" className="sr-only">
        {outcomeAnnouncement}
      </div>
    </div>
  )
}
