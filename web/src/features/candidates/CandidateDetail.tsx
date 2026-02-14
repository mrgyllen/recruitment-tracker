import { useParams, Link } from 'react-router'
import { useCandidates } from './hooks/useCandidates'
import { DocumentUpload } from './DocumentUpload'
import { useRecruitment } from '@/features/recruitments/hooks/useRecruitment'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { Button } from '@/components/ui/button'
import { ArrowLeft } from 'lucide-react'

export function CandidateDetail() {
  const { recruitmentId, candidateId } = useParams<{
    recruitmentId: string
    candidateId: string
  }>()

  const { data: recruitment } = useRecruitment(recruitmentId ?? '')
  const { data, isPending } = useCandidates(recruitmentId ?? '')

  const isClosed = recruitment?.status === 'Closed'

  if (isPending) {
    return <SkeletonLoader variant="card" />
  }

  const candidate = data?.items.find((c) => c.id === candidateId)

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
      </div>

      <div>
        <h3 className="mb-2 text-sm font-medium">Document</h3>
        <DocumentUpload
          recruitmentId={recruitmentId}
          candidateId={candidateId}
          existingDocument={null}
          isClosed={isClosed}
        />
      </div>
    </div>
  )
}
