import { Link, useParams } from 'react-router'
import { useRecruitment } from '../hooks/useRecruitment'
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

  return (
    <div className="mx-auto max-w-4xl p-6">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{data.title}</h1>
          {data.description && (
            <p className="text-muted-foreground mt-1">{data.description}</p>
          )}
        </div>
        <Badge variant={data.status === 'Active' ? 'default' : 'secondary'}>
          {data.status}
        </Badge>
      </div>

      {data.steps.length > 0 && (
        <div>
          <h2 className="mb-3 text-lg font-medium">Workflow Steps</h2>
          <ol className="space-y-2">
            {data.steps.map((step) => (
              <li
                key={step.id}
                className="flex items-center gap-3 rounded-md border p-3"
              >
                <span className="text-muted-foreground text-sm font-medium">
                  {step.order}
                </span>
                <span>{step.name}</span>
              </li>
            ))}
          </ol>
        </div>
      )}
    </div>
  )
}
