import { StatusBadge } from '@/components/StatusBadge'
import type { StepOverview } from '@/lib/api/recruitments.types'

interface StepSummaryCardProps {
  step: StepOverview
  totalCandidates: number
  staleDays: number
  onStepFilter: (stepId: string) => void
  onStaleFilter: (stepId: string) => void
}

export function StepSummaryCard({
  step,
  totalCandidates,
  staleDays,
  onStepFilter,
  onStaleFilter,
}: StepSummaryCardProps) {
  const barWidth =
    totalCandidates > 0
      ? Math.max(2, (step.totalCandidates / totalCandidates) * 100)
      : 0

  return (
    <div className="rounded-md border border-stone-200 bg-amber-50 p-3">
      <div className="flex items-center justify-between">
        <button
          aria-label={`Filter by step: ${step.stepName}`}
          className="cursor-pointer rounded px-1 text-left hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
          onClick={() => onStepFilter(step.stepId)}
        >
          <span className="font-medium text-stone-900">{step.stepName}</span>
        </button>
        <span className="text-lg font-bold text-stone-900">
          {step.totalCandidates}
        </span>
      </div>

      <div className="mt-2 h-2 w-full rounded-full bg-stone-200">
        <div
          data-testid="step-bar"
          className="h-2 rounded-full bg-stone-500"
          style={{ width: `${barWidth}%` }}
        />
      </div>

      {step.staleCount > 0 && (
        <button
          aria-label={`Show stale candidates at step: ${step.stepName}`}
          className="mt-2 inline-flex cursor-pointer items-center gap-1 rounded px-1 hover:bg-amber-100 focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
          onClick={() => onStaleFilter(step.stepId)}
        >
          <StatusBadge status="stale" />
          <span className="text-xs text-amber-600">
            {step.staleCount} candidates &gt; {staleDays} days
          </span>
        </button>
      )}
    </div>
  )
}
