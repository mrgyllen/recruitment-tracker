import { useNavigate } from 'react-router'
import { useRecruitments } from '../hooks/useRecruitments'
import { EmptyState } from '@/components/EmptyState'

export function HomePage() {
  const navigate = useNavigate()
  const { data, isLoading } = useRecruitments()

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <p className="text-muted-foreground">Loading...</p>
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
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Recruitments</h1>
        <button
          onClick={() => void navigate('/recruitments/new')}
          className="bg-primary text-primary-foreground rounded-md px-4 py-2 text-sm font-medium"
        >
          Create Recruitment
        </button>
      </div>
      <ul className="space-y-3">
        {data.items.map((item) => (
          <li
            key={item.id}
            className="rounded-lg border p-4"
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
              <span className="text-muted-foreground text-sm">
                {item.status}
              </span>
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}
