import { EmptyState } from '@/components/EmptyState'
import { useAppToast } from '@/hooks/useAppToast'

export function HomePage() {
  const toast = useAppToast()

  return (
    <div className="flex h-full items-center justify-center">
      <EmptyState
        heading="Create your first recruitment"
        description="Track candidates from screening to offer. Your team sees the same status without meetings."
        actionLabel="Create Recruitment"
        onAction={() => toast.info('Coming in Epic 2')}
      />
    </div>
  )
}
