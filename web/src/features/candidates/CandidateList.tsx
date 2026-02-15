import { X } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { Virtuoso } from 'react-virtuoso'
import { CreateCandidateForm } from './CreateCandidateForm'
import { useRemoveCandidate } from './hooks/useCandidateMutations'
import { useCandidates } from './hooks/useCandidates'
import { ImportWizard } from './ImportFlow/ImportWizard'
import type { CandidateResponse } from '@/lib/api/candidates.types'
import type { WorkflowStepDto } from '@/lib/api/recruitments.types'
import { EmptyState } from '@/components/EmptyState'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { StatusBadge } from '@/components/StatusBadge'
import { toStatusVariant } from '@/components/StatusBadge.types'
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
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useAppToast } from '@/hooks/useAppToast'
import { useDebounce } from '@/hooks/useDebounce'
import { ApiError } from '@/lib/api/httpClient'
import { cn } from '@/lib/utils'

interface CandidateListProps {
  recruitmentId: string
  isClosed: boolean
  workflowSteps?: WorkflowStepDto[]
  selectedId?: string | null
  onSelect?: (id: string) => void
  externalStepFilter?: string
  externalStaleOnly?: boolean
  onClearExternalFilters?: () => void
}

const OUTCOME_OPTIONS = ['NotStarted', 'Pass', 'Fail', 'Hold'] as const


export function CandidateList({
  recruitmentId,
  isClosed,
  workflowSteps = [],
  selectedId,
  onSelect,
  externalStepFilter,
  externalStaleOnly,
  onClearExternalFilters,
}: CandidateListProps) {
  const [searchInput, setSearchInput] = useState('')
  const [stepFilter, setStepFilter] = useState<string | undefined>()
  const [outcomeFilter, setOutcomeFilter] = useState<string | undefined>()
  const [page, setPage] = useState(1)

  const debouncedSearch = useDebounce(searchInput, 300)
  const search = debouncedSearch || undefined

  const effectiveStepFilter = externalStepFilter ?? stepFilter
  const effectiveStaleOnly = externalStaleOnly || undefined

  const { data, isPending } = useCandidates({
    recruitmentId,
    page,
    search,
    stepId: effectiveStepFilter,
    outcomeStatus: outcomeFilter,
    staleOnly: effectiveStaleOnly,
  })
  const removeMutation = useRemoveCandidate(recruitmentId)
  const toast = useAppToast()
  const [candidateToRemove, setCandidateToRemove] =
    useState<CandidateResponse | null>(null)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [importWizardOpen, setImportWizardOpen] = useState(false)

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

  function handleSearchChange(value: string) {
    setSearchInput(value)
    setPage(1)
  }

  function handleStepFilterChange(value: string) {
    setStepFilter(value === 'all' ? undefined : value)
    setPage(1)
  }

  function handleOutcomeFilterChange(value: string) {
    setOutcomeFilter(value === 'all' ? undefined : value)
    setPage(1)
  }

  function clearStepFilter() {
    setStepFilter(undefined)
    setPage(1)
  }

  function clearOutcomeFilter() {
    setOutcomeFilter(undefined)
    setPage(1)
  }

  const hasActiveFilters = !!search || !!stepFilter || !!outcomeFilter || !!externalStepFilter || !!externalStaleOnly

  if (isPending) {
    return (
      <section>
        <h2 className="mb-4 text-lg font-semibold">Candidates</h2>
        <SkeletonLoader variant="card" />
      </section>
    )
  }

  const candidates = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const pageSize = data?.pageSize ?? 50
  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <section>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold">Candidates</h2>
        {!isClosed && candidates.length > 0 && (
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              onClick={() => setImportWizardOpen(true)}
            >
              Import Candidates
            </Button>
            <CreateCandidateForm recruitmentId={recruitmentId} />
          </div>
        )}
      </div>

      {/* External filter badges (from overview) */}
      {(externalStepFilter || externalStaleOnly) && (
        <div
          className="mb-4 flex flex-wrap items-center gap-2"
          aria-live="polite"
        >
          <Badge variant="secondary" className="gap-1">
            Step:{' '}
            {workflowSteps.find((s) => s.id === externalStepFilter)?.name ??
              'Unknown'}
            {externalStaleOnly && ' (stale only)'}
            <button
              onClick={onClearExternalFilters}
              className="ml-1"
              aria-label="Clear overview filter"
            >
              <X className="h-3 w-3" />
            </button>
          </Badge>
        </div>
      )}

      {/* Search and filter controls */}
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Input
          placeholder="Search by name or email..."
          value={searchInput}
          onChange={(e) => handleSearchChange(e.target.value)}
          className="max-w-xs"
          aria-label="Search candidates"
        />
        <Select
          value={stepFilter ?? 'all'}
          onValueChange={handleStepFilterChange}
        >
          <SelectTrigger className="w-[180px]" aria-label="Filter by step">
            <SelectValue placeholder="All steps" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All steps</SelectItem>
            {workflowSteps.map((step) => (
              <SelectItem key={step.id} value={step.id}>
                {step.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={outcomeFilter ?? 'all'}
          onValueChange={handleOutcomeFilterChange}
        >
          <SelectTrigger
            className="w-[180px]"
            aria-label="Filter by outcome"
          >
            <SelectValue placeholder="All outcomes" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All outcomes</SelectItem>
            {OUTCOME_OPTIONS.map((status) => (
              <SelectItem key={status} value={status}>
                {status === 'NotStarted' ? 'Not Started' : status}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Active filter badges */}
      {hasActiveFilters && (
        <div className="mb-4 flex flex-wrap items-center gap-2">
          {stepFilter && (
            <Badge variant="secondary" className="gap-1">
              Step:{' '}
              {workflowSteps.find((s) => s.id === stepFilter)?.name ??
                'Unknown'}
              <button
                onClick={clearStepFilter}
                className="ml-1"
                aria-label="Clear step filter"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          {outcomeFilter && (
            <Badge variant="secondary" className="gap-1">
              Status:{' '}
              {outcomeFilter === 'NotStarted'
                ? 'Not Started'
                : outcomeFilter}
              <button
                onClick={clearOutcomeFilter}
                className="ml-1"
                aria-label="Clear outcome filter"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
        </div>
      )}

      {candidates.length === 0 && !hasActiveFilters ? (
        <>
          <EmptyState
            heading="No candidates yet"
            description="Add candidates to this recruitment to start tracking them through the workflow."
            actionLabel={isClosed ? undefined : 'Add Candidate'}
            onAction={isClosed ? undefined : () => setCreateDialogOpen(true)}
          />
          {!isClosed && (
            <>
              <Button
                variant="outline"
                className="mt-3"
                onClick={() => setImportWizardOpen(true)}
              >
                Import from Workday
              </Button>
              <CreateCandidateForm
                recruitmentId={recruitmentId}
                open={createDialogOpen}
                onOpenChange={setCreateDialogOpen}
              />
            </>
          )}
        </>
      ) : candidates.length === 0 && hasActiveFilters ? (
        <EmptyState
          heading="No matching candidates"
          description="Try adjusting your search or filters."
        />
      ) : (
        <>
          <div className="rounded-md border">
            {totalCount > 50 ? (
              <Virtuoso
                style={{ height: '600px' }}
                totalCount={candidates.length}
                itemContent={(index) => {
                  const candidate = candidates[index]
                  return (
                    <CandidateRow
                      key={candidate.id}
                      candidate={candidate}
                      recruitmentId={recruitmentId}
                      isClosed={isClosed}
                      onRemove={setCandidateToRemove}
                      isSelected={candidate.id === selectedId}
                      onSelect={onSelect}
                    />
                  )
                }}
              />
            ) : (
              <div className="divide-y">
                {candidates.map((candidate) => (
                  <CandidateRow
                    key={candidate.id}
                    candidate={candidate}
                    recruitmentId={recruitmentId}
                    isClosed={isClosed}
                    onRemove={setCandidateToRemove}
                    isSelected={candidate.id === selectedId}
                    onSelect={onSelect}
                  />
                ))}
              </div>
            )}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="mt-4 flex items-center justify-between">
              <p className="text-muted-foreground text-sm">
                Showing {(page - 1) * pageSize + 1}--
                {Math.min(page * pageSize, totalCount)} of {totalCount}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  Previous
                </Button>
                <span className="text-muted-foreground text-sm">
                  Page {page} of {totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
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

      <ImportWizard
        recruitmentId={recruitmentId}
        open={importWizardOpen}
        onOpenChange={setImportWizardOpen}
      />
    </section>
  )
}

function CandidateRow({
  candidate,
  recruitmentId,
  isClosed,
  onRemove,
  isSelected,
  onSelect,
}: {
  candidate: CandidateResponse
  recruitmentId: string
  isClosed: boolean
  onRemove: (c: CandidateResponse) => void
  isSelected?: boolean
  onSelect?: (id: string) => void
}) {
  return (
    <div
      className={cn(
        'flex items-center justify-between px-4 py-3',
        isSelected && 'bg-blue-50',
        onSelect && 'cursor-pointer',
      )}
      onClick={onSelect ? () => onSelect(candidate.id) : undefined}
    >
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          {onSelect ? (
            <span className="font-medium">{candidate.fullName}</span>
          ) : (
            <Link
              to={`/recruitments/${recruitmentId}/candidates/${candidate.id}`}
              className="font-medium hover:underline"
            >
              {candidate.fullName}
            </Link>
          )}
          {candidate.currentOutcomeStatus && (
            <StatusBadge
              status={toStatusVariant(candidate.currentOutcomeStatus)}
            />
          )}
        </div>
        <p className="text-muted-foreground text-sm">
          {candidate.email}
          {candidate.location && ` -- ${candidate.location}`}
        </p>
        <p className="text-muted-foreground text-xs">
          {candidate.currentWorkflowStepName && (
            <span className="mr-2">
              Step: {candidate.currentWorkflowStepName}
            </span>
          )}
          Applied: {new Date(candidate.dateApplied).toLocaleDateString()}
        </p>
      </div>
      {!isClosed && (
        <Button
          variant="ghost"
          size="sm"
          className="text-destructive hover:text-destructive"
          onClick={() => onRemove(candidate)}
        >
          Remove
        </Button>
      )}
    </div>
  )
}
