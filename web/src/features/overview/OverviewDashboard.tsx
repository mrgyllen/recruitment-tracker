import { ChevronDown, ChevronUp } from 'lucide-react'
import { useState } from 'react'
import { useRecruitmentOverview } from './hooks/useRecruitmentOverview'
import { KpiCard } from './KpiCard'
import { PendingActionsPanel } from './PendingActionsPanel'
import { StepSummaryCard } from './StepSummaryCard'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'

interface OverviewDashboardProps {
  recruitmentId: string
  onStepFilter: (stepId: string) => void
  onStaleFilter: (stepId: string) => void
}

export function OverviewDashboard({
  recruitmentId,
  onStepFilter,
  onStaleFilter,
}: OverviewDashboardProps) {
  const storageKey = `overview-collapsed:${recruitmentId}`
  const [prevStorageKey, setPrevStorageKey] = useState(storageKey)
  const [isOpen, setIsOpen] = useState(() => {
    const stored = localStorage.getItem(storageKey)
    return stored !== 'true'
  })

  if (prevStorageKey !== storageKey) {
    setPrevStorageKey(storageKey)
    const stored = localStorage.getItem(storageKey)
    setIsOpen(stored !== 'true')
  }

  const { data, isPending } = useRecruitmentOverview(recruitmentId)

  function handleOpenChange(open: boolean) {
    setIsOpen(open)
    localStorage.setItem(storageKey, String(!open))
  }

  if (isPending) {
    return (
      <section aria-label="Overview">
        <div className="grid grid-cols-3 gap-4">
          <SkeletonLoader variant="card" />
          <SkeletonLoader variant="card" />
          <SkeletonLoader variant="card" />
        </div>
      </section>
    )
  }

  if (!data) return null

  const screenedCount = data.totalCandidates - data.pendingActionCount

  return (
    <section aria-label="Overview">
      <Collapsible open={isOpen} onOpenChange={handleOpenChange}>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold">Overview</h2>
          <CollapsibleTrigger asChild>
            <button
              className="rounded p-1 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2"
              aria-label={isOpen ? 'Collapse overview' : 'Expand overview'}
            >
              {isOpen ? (
                <ChevronUp className="h-5 w-5" />
              ) : (
                <ChevronDown className="h-5 w-5" />
              )}
            </button>
          </CollapsibleTrigger>
        </div>

        {!isOpen && (
          <p className="text-sm text-stone-600">
            {data.totalCandidates} candidates - {screenedCount} screened -{' '}
            {data.totalStale} stale
          </p>
        )}

        <CollapsibleContent>
          {data.totalCandidates === 0 ? (
            <p className="text-sm text-stone-500">
              No candidates imported yet.
            </p>
          ) : (
            <>
              <div className="mb-4 grid grid-cols-3 gap-4">
                <KpiCard
                  label="Total Candidates"
                  value={data.totalCandidates}
                />
                <PendingActionsPanel count={data.pendingActionCount} />
                <KpiCard
                  label="Stale Candidates"
                  value={data.totalStale}
                  variant="warning"
                />
              </div>

              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.steps.map((step) => (
                  <StepSummaryCard
                    key={step.stepId}
                    step={step}
                    totalCandidates={data.totalCandidates}
                    staleDays={data.staleDays}
                    onStepFilter={onStepFilter}
                    onStaleFilter={onStaleFilter}
                  />
                ))}
              </div>
            </>
          )}
        </CollapsibleContent>
      </Collapsible>
    </section>
  )
}
