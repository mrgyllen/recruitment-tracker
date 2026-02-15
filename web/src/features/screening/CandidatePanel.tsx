import type { WorkflowStepDto } from '@/lib/api/recruitments.types'
import { CandidateList } from '@/features/candidates/CandidateList'

interface CandidatePanelProps {
  recruitmentId: string
  selectedCandidateId: string | null
  onCandidateSelect: (id: string) => void
  sessionScreenedCount: number
  totalScreenedCount: number
  totalCandidateCount: number
  isAllScreened: boolean
  isClosed: boolean
  workflowSteps: WorkflowStepDto[]
  candidateListRef?: React.RefObject<HTMLDivElement | null>
}

export function CandidatePanel({
  recruitmentId,
  selectedCandidateId,
  onCandidateSelect,
  sessionScreenedCount,
  totalScreenedCount,
  totalCandidateCount,
  isAllScreened,
  isClosed,
  workflowSteps,
  candidateListRef,
}: CandidatePanelProps) {
  return (
    <div ref={candidateListRef} tabIndex={0} className="flex h-full flex-col focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2">
      <div className="border-b bg-gray-50 p-3">
        <div className="flex justify-between text-sm">
          <span className="font-medium">
            {totalScreenedCount} of {totalCandidateCount} screened
          </span>
          <span className="text-gray-500">{sessionScreenedCount} this session</span>
        </div>
        {isAllScreened && (
          <div className="mt-2 rounded-md bg-green-50 px-3 py-1.5 text-center text-sm font-medium text-green-700">
            All candidates screened!
          </div>
        )}
      </div>
      <div className="flex-1 overflow-hidden">
        <CandidateList
          recruitmentId={recruitmentId}
          isClosed={isClosed}
          workflowSteps={workflowSteps}
          selectedId={selectedCandidateId}
          onSelect={onCandidateSelect}
        />
      </div>
    </div>
  )
}
