import { Link, useNavigate } from 'react-router'
import { useRecruitments } from './hooks/useRecruitments'
import { EmptyState } from '@/components/EmptyState'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { Badge } from '@/components/ui/badge'

export function RecruitmentList() {
  const navigate = useNavigate()
  const { data, isPending } = useRecruitments()

  if (isPending) {
    return (
      <div className="mx-auto max-w-4xl space-y-3 p-6">
        {Array.from({ length: 3 }).map((_, i) => (
          <SkeletonLoader key={i} variant="card" />
        ))}
      </div>
    )
  }

  if (!data || data.items.length === 0) {
    return (
      <div className="flex h-full items-center justify-center">
        <EmptyState
          heading="Create your first recruitment"
          description="Track candidates from screening to offer. Your team sees the same status without meetings."
          actionLabel="Create Recruitment"
          onAction={() => void navigate('/recruitments/new')}
        />
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-4xl p-6">
      <ul className="space-y-3" role="list" aria-label="Recruitments">
        {data.items.map((item) => (
          <li key={item.id}>
            <Link
              to={`/recruitments/${item.id}`}
              className="block rounded-lg border p-4 transition-colors hover:bg-muted/50"
            >
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="font-medium">{item.title}</h2>
                  {item.description && (
                    <p className="text-muted-foreground text-sm">
                      {item.description}
                    </p>
                  )}
                </div>
                <Badge
                  variant={item.status === 'Active' ? 'default' : 'secondary'}
                >
                  {item.status}
                </Badge>
              </div>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}
