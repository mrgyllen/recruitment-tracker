import { useState } from 'react'
import {
  AlertTriangle,
  CheckCircle,
  ChevronDown,
  RefreshCw,
  XCircle,
} from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { cn } from '@/lib/utils'
import type { ImportRowResult } from '@/lib/api/import.types'

interface ImportSummaryProps {
  createdCount: number
  updatedCount: number
  erroredCount: number
  flaggedCount: number
  rowResults: ImportRowResult[]
  failureReason?: string | null
  onReviewMatches?: () => void
  onDone: () => void
}

export function ImportSummary({
  createdCount,
  updatedCount,
  erroredCount,
  flaggedCount,
  rowResults,
  failureReason,
  onReviewMatches,
  onDone,
}: ImportSummaryProps) {
  return (
    <div className="space-y-4 p-4">
      {failureReason && (
        <Alert variant="destructive">
          <XCircle className="size-4" />
          <AlertDescription>{failureReason}</AlertDescription>
        </Alert>
      )}

      <div className="grid grid-cols-2 gap-3">
        <SummaryCard
          label="Created"
          count={createdCount}
          icon={<CheckCircle className="size-4 text-green-600" />}
        />
        <SummaryCard
          label="Updated"
          count={updatedCount}
          icon={<RefreshCw className="size-4 text-blue-600" />}
        />
        <SummaryCard
          label="Errored"
          count={erroredCount}
          icon={<XCircle className="size-4 text-red-600" />}
        />
        <SummaryCard
          label="Flagged"
          count={flaggedCount}
          icon={<AlertTriangle className="size-4 text-amber-600" />}
        />
      </div>

      {flaggedCount > 0 && onReviewMatches && (
        <Alert>
          <AlertTriangle className="size-4" />
          <AlertDescription className="flex items-center justify-between">
            <span>
              {flaggedCount} match{flaggedCount !== 1 ? 'es' : ''} by name+phone
              only -- review recommended
            </span>
            <Button variant="outline" size="sm" onClick={onReviewMatches}>
              Review Matches
            </Button>
          </AlertDescription>
        </Alert>
      )}

      <RowDetailSection
        label="Errored"
        rows={rowResults.filter((r) => r.action === 'Errored')}
      />

      <RowDetailSection
        label="Created"
        rows={rowResults.filter((r) => r.action === 'Created')}
      />

      <RowDetailSection
        label="Updated"
        rows={rowResults.filter((r) => r.action === 'Updated')}
      />

      <Button className="w-full" onClick={onDone}>
        Done
      </Button>
    </div>
  )
}

function SummaryCard({
  label,
  count,
  icon,
}: {
  label: string
  count: number
  icon: React.ReactNode
}) {
  return (
    <div className="flex items-center gap-3 rounded-md border p-3">
      {icon}
      <div>
        <p className="text-2xl font-semibold">{count}</p>
        <p className="text-muted-foreground text-xs">{label}</p>
      </div>
    </div>
  )
}

function RowDetailSection({
  label,
  rows,
}: {
  label: string
  rows: ImportRowResult[]
}) {
  const [isOpen, setIsOpen] = useState(false)

  if (rows.length === 0) return null

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <CollapsibleTrigger className="flex w-full items-center gap-2 text-sm font-medium">
        <ChevronDown
          className={cn('size-4 transition-transform', isOpen && 'rotate-180')}
        />
        {label} ({rows.length})
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-1">
        <div className="divide-y rounded-md border text-sm">
          {rows.map((row) => (
            <div key={row.rowNumber} className="px-3 py-2">
              <span className="text-muted-foreground">Row {row.rowNumber}</span>
              {row.candidateEmail && (
                <span className="ml-2">{row.candidateEmail}</span>
              )}
              {row.errorMessage && (
                <span className="text-destructive ml-2">
                  {row.errorMessage}
                </span>
              )}
            </div>
          ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}
