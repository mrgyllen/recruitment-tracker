import { Link, useParams } from 'react-router'
import { EditRecruitmentForm } from '../EditRecruitmentForm'
import { useRecruitment } from '../hooks/useRecruitment'
import { WorkflowStepEditor } from '../WorkflowStepEditor'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api/httpClient'

export function RecruitmentPage() {
  const { recruitmentId } = useParams<{ recruitmentId: string }>()
  const { data, isPending, error } = useRecruitment(recruitmentId ?? '')

  if (isPending) {
    return (
      <div className="mx-auto max-w-4xl space-y-4 p-6">
        <SkeletonLoader variant="text-block" />
        <SkeletonLoader variant="card" />
      </div>
    )
  }

  if (error) {
    const status = error instanceof ApiError ? error.status : 0

    if (status === 403) {
      return (
        <div className="flex h-full flex-col items-center justify-center gap-4">
          <h2 className="text-lg font-semibold">Access Denied</h2>
          <p className="text-muted-foreground">
            You don&apos;t have access to this recruitment.
          </p>
          <Button asChild variant="outline">
            <Link to="/">Back to Home</Link>
          </Button>
        </div>
      )
    }

    if (status === 404) {
      return (
        <div className="flex h-full flex-col items-center justify-center gap-4">
          <h2 className="text-lg font-semibold">Not Found</h2>
          <p className="text-muted-foreground">
            Recruitment not found.
          </p>
          <Button asChild variant="outline">
            <Link to="/">Back to Home</Link>
          </Button>
        </div>
      )
    }

    return (
      <div className="flex h-full flex-col items-center justify-center gap-4">
        <h2 className="text-lg font-semibold">Something went wrong</h2>
        <p className="text-muted-foreground">
          An unexpected error occurred.
        </p>
        <Button asChild variant="outline">
          <Link to="/">Back to Home</Link>
        </Button>
      </div>
    )
  }

  if (!data) return null

  const isClosed = data.status === 'Closed'

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{data.title}</h1>
        <Badge variant={data.status === 'Active' ? 'default' : 'secondary'}>
          {data.status}
        </Badge>
      </div>

      <EditRecruitmentForm recruitment={data} />

      <WorkflowStepEditor
        mode="edit"
        steps={data.steps}
        recruitmentId={data.id}
        disabled={isClosed}
      />
    </div>
  )
}
