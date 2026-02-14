import { AlertTriangle } from 'lucide-react'
import { useResolveMatch } from './hooks/useResolveMatch'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import type { ImportRowResult } from '@/lib/api/import.types'

interface MatchReviewStepProps {
  importSessionId: string
  flaggedRows: ImportRowResult[]
  onDone: () => void
}

export function MatchReviewStep({
  importSessionId,
  flaggedRows,
  onDone,
}: MatchReviewStepProps) {
  const resolveMatch = useResolveMatch(importSessionId)

  const unresolvedCount = flaggedRows.filter((r) => !r.resolution).length

  return (
    <div className="space-y-4 p-4">
      <Alert>
        <AlertTriangle className="size-4" />
        <AlertDescription>
          {unresolvedCount > 0
            ? `${unresolvedCount} match${unresolvedCount !== 1 ? 'es' : ''} need${unresolvedCount === 1 ? 's' : ''} review`
            : 'All matches reviewed'}
        </AlertDescription>
      </Alert>

      <div className="divide-y rounded-md border">
        {flaggedRows.map((row, index) => (
          <div key={row.rowNumber} className="space-y-2 p-3">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">
                  Row {row.rowNumber}: {row.candidateEmail ?? 'Unknown'}
                </p>
                <p className="text-muted-foreground text-xs">
                  Matched by name + phone
                </p>
              </div>
              {row.resolution ? (
                <span
                  className={
                    row.resolution === 'Confirmed'
                      ? 'text-sm text-green-600'
                      : 'text-sm text-red-600'
                  }
                >
                  {row.resolution}
                </span>
              ) : (
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={resolveMatch.isPending}
                    onClick={() =>
                      resolveMatch.mutate({
                        matchIndex: index,
                        action: 'Confirm',
                      })
                    }
                  >
                    Confirm Match
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled={resolveMatch.isPending}
                    onClick={() =>
                      resolveMatch.mutate({
                        matchIndex: index,
                        action: 'Reject',
                      })
                    }
                  >
                    Reject
                  </Button>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      <Button className="w-full" onClick={onDone}>
        Done
      </Button>
    </div>
  )
}
