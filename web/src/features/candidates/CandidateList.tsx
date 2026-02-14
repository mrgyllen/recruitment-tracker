import { useState } from 'react'
import { CreateCandidateForm } from './CreateCandidateForm'
import { useCandidates } from './hooks/useCandidates'
import { useRemoveCandidate } from './hooks/useCandidateMutations'
import { EmptyState } from '@/components/EmptyState'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'
import type { CandidateResponse } from '@/lib/api/candidates.types'

interface CandidateListProps {
  recruitmentId: string
  isClosed: boolean
}

export function CandidateList({ recruitmentId, isClosed }: CandidateListProps) {
  const { data, isPending } = useCandidates(recruitmentId)
  const removeMutation = useRemoveCandidate(recruitmentId)
  const toast = useAppToast()
  const [candidateToRemove, setCandidateToRemove] =
    useState<CandidateResponse | null>(null)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)

  function handleConfirmRemove() {
    if (!candidateToRemove) return
    removeMutation.mutate(candidateToRemove.id, {
      onSuccess: () => {
        toast.success('Candidate removed')
        setCandidateToRemove(null)
      },
      onError: (error) => {
        if (error instanceof ApiError) {
          toast.error(error.problemDetails.title)
        } else {
          toast.error('Failed to remove candidate')
        }
        setCandidateToRemove(null)
      },
    })
  }

  if (isPending) {
    return (
      <section>
        <h2 className="mb-4 text-lg font-semibold">Candidates</h2>
        <SkeletonLoader variant="card" />
      </section>
    )
  }

  const candidates = data?.items ?? []

  return (
    <section>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold">Candidates</h2>
        {!isClosed && candidates.length > 0 && (
          <CreateCandidateForm recruitmentId={recruitmentId} />
        )}
      </div>

      {candidates.length === 0 ? (
        <>
          <EmptyState
            heading="No candidates yet"
            description="Add candidates to this recruitment to start tracking them through the workflow."
            actionLabel={isClosed ? undefined : 'Add Candidate'}
            onAction={isClosed ? undefined : () => setCreateDialogOpen(true)}
          />
          {!isClosed && (
            <CreateCandidateForm
              recruitmentId={recruitmentId}
              open={createDialogOpen}
              onOpenChange={setCreateDialogOpen}
            />
          )}
        </>
      ) : (
        <div className="divide-y rounded-md border">
          {candidates.map((candidate) => (
            <div
              key={candidate.id}
              className="flex items-center justify-between px-4 py-3"
            >
              <div className="min-w-0 flex-1">
                <p className="font-medium">{candidate.fullName}</p>
                <p className="text-muted-foreground text-sm">
                  {candidate.email}
                  {candidate.location && ` -- ${candidate.location}`}
                </p>
                <p className="text-muted-foreground text-xs">
                  Applied:{' '}
                  {new Date(candidate.dateApplied).toLocaleDateString()}
                </p>
              </div>
              {!isClosed && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-destructive hover:text-destructive"
                  onClick={() => setCandidateToRemove(candidate)}
                >
                  Remove
                </Button>
              )}
            </div>
          ))}
        </div>
      )}

      <AlertDialog
        open={!!candidateToRemove}
        onOpenChange={(open) => {
          if (!open) setCandidateToRemove(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Candidate</AlertDialogTitle>
            <AlertDialogDescription>
              Remove {candidateToRemove?.fullName} from this recruitment? This
              cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmRemove}
              disabled={removeMutation.isPending}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {removeMutation.isPending ? 'Removing...' : 'Remove'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  )
}
