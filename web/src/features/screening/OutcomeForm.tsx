import { useState } from 'react'
import { useRecordOutcome } from './hooks/useRecordOutcome'
import type { OutcomeHistoryDto, OutcomeResultDto, OutcomeStatus } from '@/lib/api/screening.types'
import { useAppToast } from '@/hooks/useAppToast'
import { cn } from '@/lib/utils'

interface OutcomeFormProps {
  recruitmentId: string
  candidateId: string
  currentStepId: string
  currentStepName: string
  existingOutcome: OutcomeHistoryDto | null
  isClosed: boolean
  onOutcomeRecorded?: (result: OutcomeResultDto) => void
  onOutcomeSelect?: (outcome: OutcomeStatus) => void
  externalOutcome?: OutcomeStatus | null
}

const outcomeOptions: { value: OutcomeStatus; label: string; hint: string; className: string; selectedClassName: string }[] = [
  { value: 'Pass', label: 'Pass', hint: '1', className: 'border-green-300 text-green-700 hover:bg-green-50', selectedClassName: 'bg-green-600 text-white border-green-600' },
  { value: 'Fail', label: 'Fail', hint: '2', className: 'border-red-300 text-red-700 hover:bg-red-50', selectedClassName: 'bg-red-600 text-white border-red-600' },
  { value: 'Hold', label: 'Hold', hint: '3', className: 'border-amber-300 text-amber-700 hover:bg-amber-50', selectedClassName: 'bg-amber-500 text-white border-amber-500' },
]

export function OutcomeForm({
  recruitmentId,
  candidateId,
  currentStepId,
  currentStepName,
  existingOutcome,
  isClosed,
  onOutcomeRecorded,
  onOutcomeSelect,
  externalOutcome,
}: OutcomeFormProps) {
  const [selectedOutcome, setSelectedOutcome] = useState<OutcomeStatus | null>(
    externalOutcome ?? existingOutcome?.outcome ?? null,
  )
  const [prevExternalOutcome, setPrevExternalOutcome] = useState(externalOutcome)

  if (externalOutcome && externalOutcome !== prevExternalOutcome) {
    setPrevExternalOutcome(externalOutcome)
    setSelectedOutcome(externalOutcome)
  }
  const [reason, setReason] = useState(existingOutcome?.reason ?? '')
  const toast = useAppToast()
  const recordOutcome = useRecordOutcome()

  const handleConfirm = () => {
    if (!selectedOutcome) return
    recordOutcome.mutate(
      {
        recruitmentId,
        candidateId,
        data: {
          workflowStepId: currentStepId,
          outcome: selectedOutcome,
          reason: reason || undefined,
        },
      },
      {
        onSuccess: (result) => {
          toast.success(`${selectedOutcome} recorded`)
          onOutcomeRecorded?.(result)
        },
      },
    )
  }

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-medium">
        Outcome for: {currentStepName}
      </h3>
      <div className="flex gap-2" role="group" aria-label="Outcome selection">
        {outcomeOptions.map((option) => (
          <button
            key={option.value}
            type="button"
            aria-pressed={selectedOutcome === option.value}
            disabled={isClosed}
            onClick={() => {
              setSelectedOutcome(option.value)
              onOutcomeSelect?.(option.value)
            }}
            className={cn(
              'rounded-md border px-4 py-2 text-sm font-medium transition-colors',
              selectedOutcome === option.value ? option.selectedClassName : option.className,
              isClosed && 'cursor-not-allowed opacity-50',
            )}
          >
            {option.label} <kbd className="ml-1 text-xs opacity-60">({option.hint})</kbd>
          </button>
        ))}
      </div>
      <div>
        <label htmlFor="outcome-reason" className="text-sm font-medium">
          Reason
        </label>
        <textarea
          id="outcome-reason"
          aria-label="Reason"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          disabled={isClosed}
          maxLength={500}
          placeholder="Optional reason for this decision..."
          className="mt-1 block w-full rounded-md border px-3 py-2 text-sm disabled:cursor-not-allowed disabled:opacity-50"
          rows={3}
        />
      </div>
      {recordOutcome.isError && (
        <div role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
          {recordOutcome.error instanceof Error ? recordOutcome.error.message : 'Failed to record outcome'}
        </div>
      )}
      <button
        type="button"
        disabled={!selectedOutcome || isClosed || recordOutcome.isPending}
        onClick={handleConfirm}
        className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground disabled:cursor-not-allowed disabled:opacity-50"
      >
        {recordOutcome.isPending ? 'Recording...' : 'Confirm'}
      </button>
    </div>
  )
}
