import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

type SkeletonVariant = 'card' | 'list-row' | 'text-block'

interface SkeletonLoaderProps {
  variant: SkeletonVariant
  className?: string
}

export function SkeletonLoader({ variant, className }: SkeletonLoaderProps) {
  switch (variant) {
    case 'card':
      return (
        <div
          data-testid="skeleton-card"
          className={cn(
            'animate-pulse space-y-3 rounded-md border border-border-default p-4',
            className,
          )}
        >
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-20 w-full" />
        </div>
      )
    case 'list-row':
      return (
        <div
          data-testid="skeleton-list-row"
          className={cn(
            'animate-pulse flex items-center gap-3 py-3',
            className,
          )}
        >
          <Skeleton className="h-8 w-8 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-1/3" />
            <Skeleton className="h-3 w-1/4" />
          </div>
        </div>
      )
    case 'text-block':
      return (
        <div
          data-testid="skeleton-text-block"
          className={cn('animate-pulse space-y-2', className)}
        >
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-5/6" />
          <Skeleton className="h-4 w-4/6" />
        </div>
      )
  }
}
