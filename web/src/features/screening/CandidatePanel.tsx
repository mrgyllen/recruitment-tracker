import { CandidateList } from '@/features/candidates/CandidateList'
import type { WorkflowStepDto } from '@/lib/api/recruitments.types'

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
}: CandidatePanelProps) {
  return (
    <div className="flex h-full flex-col">
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
