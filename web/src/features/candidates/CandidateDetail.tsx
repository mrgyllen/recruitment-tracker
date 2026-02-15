import { ArrowLeft, Download } from 'lucide-react'
import { useParams, Link } from 'react-router'
import { DocumentUpload } from './DocumentUpload'
import { useCandidateById } from './hooks/useCandidateById'
import { useSasUrl } from './hooks/useSasUrl'
import { PdfViewer } from './PdfViewer'
import type { OutcomeHistoryEntry } from '@/lib/api/candidates.types'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { StatusBadge } from '@/components/StatusBadge'
import { toStatusVariant } from '@/components/StatusBadge.types'
import { Button } from '@/components/ui/button'
import { useRecruitment } from '@/features/recruitments/hooks/useRecruitment'


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

      {/* CV Viewer */}
      <div>
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-sm font-medium">CV</h3>
          {candidate.documents.length > 0 && (
            <Button variant="outline" size="sm" asChild>
              <a
                href={candidate.documents[0].sasUrl}
                target="_blank"
                rel="noopener noreferrer"
              >
                <Download className="mr-1 size-4" />
                Download
              </a>
            </Button>
          )}
        </div>

        {candidate.documents.length > 0 ? (
          <CvViewer
            sasUrl={candidate.documents[0].sasUrl}
            recruitmentId={recruitmentId}
            candidateId={candidateId}
          />
        ) : (
          <div className="rounded-md border p-6 text-center">
            <p className="text-muted-foreground mb-3 text-sm">
              No CV available
            </p>
            {!isClosed && (
              <DocumentUpload
                recruitmentId={recruitmentId}
                candidateId={candidateId}
                existingDocument={null}
                isClosed={false}
              />
            )}
          </div>
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

function CvViewer({
  sasUrl,
  recruitmentId,
  candidateId,
}: {
  sasUrl: string
  recruitmentId: string
  candidateId: string
}) {
  const { url, refresh } = useSasUrl({
    initialUrl: sasUrl,
    recruitmentId,
    candidateId,
  })

  return <PdfViewer url={url} onError={refresh} />
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
