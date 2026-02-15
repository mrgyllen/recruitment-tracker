import type { OutcomeHistoryDto } from '@/lib/api/screening.types'
import { StatusBadge } from '@/components/StatusBadge'
import { toStatusVariant } from '@/components/StatusBadge.types'

interface OutcomeHistoryProps {
  history: OutcomeHistoryDto[]
}

export function OutcomeHistory({ history }: OutcomeHistoryProps) {
  if (history.length === 0) {
    return (
      <p className="text-muted-foreground text-sm">No outcomes recorded yet.</p>
    )
  }

  const sorted = [...history].sort((a, b) => a.stepOrder - b.stepOrder)

  return (
    <ul className="space-y-3" role="list">
      {sorted.map((entry) => (
        <li key={entry.workflowStepId} className="rounded-md border p-3">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">{entry.workflowStepName}</span>
            <StatusBadge status={toStatusVariant(entry.outcome)} />
          </div>
          {entry.reason && (
            <p className="text-muted-foreground mt-1 text-sm">{entry.reason}</p>
          )}
          <p className="text-muted-foreground mt-1 text-xs">
            {new Intl.DateTimeFormat(undefined, {
              dateStyle: 'medium',
              timeStyle: 'short',
            }).format(new Date(entry.recordedAt))}
          </p>
        </li>
      ))}
    </ul>
  )
}
