import { useParams, Link } from 'react-router'
import { useCandidateById } from './hooks/useCandidateById'
import { DocumentUpload } from './DocumentUpload'
import { useRecruitment } from '@/features/recruitments/hooks/useRecruitment'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { StatusBadge } from '@/components/StatusBadge'
import type { StatusVariant } from '@/components/StatusBadge.types'
import { Button } from '@/components/ui/button'
import { ArrowLeft, Download } from 'lucide-react'
import type { OutcomeHistoryEntry } from '@/lib/api/candidates.types'

function toStatusVariant(status: string): StatusVariant {
  switch (status) {
    case 'Pass':
      return 'pass'
    case 'Fail':
      return 'fail'
    case 'Hold':
      return 'hold'
    case 'NotStarted':
    default:
      return 'not-started'
  }
}

export function CandidateDetail() {
  const { recruitmentId, candidateId } = useParams<{
    recruitmentId: string
    candidateId: string
  }>()

  const { data: recruitment } = useRecruitment(recruitmentId ?? '')
  const { data: candidate, isPending } = useCandidateById(
    recruitmentId ?? '',
    candidateId ?? '',
  )

  const isClosed = recruitment?.status === 'Closed'

  if (isPending) {
    return <SkeletonLoader variant="card" />
  }

  if (!candidate || !recruitmentId || !candidateId) {
    return <p className="text-muted-foreground">Candidate not found.</p>
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" asChild>
          <Link to={`/recruitments/${recruitmentId}`}>
            <ArrowLeft className="mr-1 size-4" />
            Back
          </Link>
        </Button>
      </div>

      {/* Current step */}
      {candidate.currentWorkflowStepName && (
        <div className="flex items-center gap-2 rounded-md border p-3">
          <span className="text-muted-foreground text-sm">Current step:</span>
          <span className="font-medium">
            {candidate.currentWorkflowStepName}
          </span>
          {candidate.currentOutcomeStatus && (
            <StatusBadge
              status={toStatusVariant(candidate.currentOutcomeStatus)}
            />
          )}
        </div>
      )}

      {/* Profile info */}
      <div>
        <h2 className="text-lg font-semibold">{candidate.fullName}</h2>
        <p className="text-muted-foreground text-sm">{candidate.email}</p>
        {candidate.phoneNumber && (
          <p className="text-muted-foreground text-sm">
            {candidate.phoneNumber}
          </p>
        )}
        {candidate.location && (
          <p className="text-muted-foreground text-sm">
            {candidate.location}
          </p>
        )}
        <p className="text-muted-foreground text-sm">
          Applied: {new Date(candidate.dateApplied).toLocaleDateString()}
        </p>
      </div>

      {/* Documents */}
      <div>
        <h3 className="mb-2 text-sm font-medium">Documents</h3>
        {candidate.documents.length > 0 ? (
          <div className="space-y-2">
            {candidate.documents.map((doc) => (
              <div
                key={doc.id}
                className="flex items-center justify-between rounded-md border p-3"
              >
                <div>
                  <p className="text-sm font-medium">{doc.documentType}</p>
                  <p className="text-muted-foreground text-xs">
                    Uploaded:{' '}
                    {new Date(doc.uploadedAt).toLocaleDateString()}
                  </p>
                </div>
                <Button variant="outline" size="sm" asChild>
                  <a
                    href={doc.sasUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    <Download className="mr-1 size-4" />
                    Download
                  </a>
                </Button>
              </div>
            ))}
          </div>
        ) : (
          <DocumentUpload
            recruitmentId={recruitmentId}
            candidateId={candidateId}
            existingDocument={null}
            isClosed={isClosed}
          />
        )}
      </div>

      {/* Outcome history */}
      <div>
        <h3 className="mb-2 text-sm font-medium">Outcome History</h3>
        {candidate.outcomeHistory.length > 0 ? (
          <div className="space-y-2">
            {candidate.outcomeHistory.map((outcome, index) => (
              <OutcomeRow key={index} outcome={outcome} />
            ))}
          </div>
        ) : (
          <p className="text-muted-foreground text-sm">
            No outcomes recorded yet.
          </p>
        )}
      </div>
    </div>
  )
}

function OutcomeRow({ outcome }: { outcome: OutcomeHistoryEntry }) {
  return (
    <div className="flex items-center justify-between rounded-md border p-3">
      <div className="flex items-center gap-3">
        <div>
          <p className="text-sm font-medium">{outcome.workflowStepName}</p>
          <p className="text-muted-foreground text-xs">
            {new Date(outcome.recordedAt).toLocaleDateString()}
          </p>
        </div>
        <StatusBadge status={toStatusVariant(outcome.status)} />
      </div>
    </div>
  )
}
