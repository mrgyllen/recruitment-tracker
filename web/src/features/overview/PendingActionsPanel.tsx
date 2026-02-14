import { KpiCard } from './KpiCard'

interface PendingActionsPanelProps {
  count: number
}

export function PendingActionsPanel({ count }: PendingActionsPanelProps) {
  return <KpiCard label="Pending Actions" value={count} />
}
