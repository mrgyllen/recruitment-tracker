import { ActionButton } from './ActionButton'

interface EmptyStateProps {
  heading: string
  description: string
  actionLabel?: string
  onAction?: () => void
  headingLevel?: 'h2' | 'h3'
  icon?: React.ReactNode
}

export function EmptyState({
  heading,
  description,
  actionLabel,
  onAction,
  headingLevel = 'h2',
  icon,
}: EmptyStateProps) {
  const Heading = headingLevel

  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      {icon && <div className="mb-4 text-text-tertiary">{icon}</div>}
      <Heading className="mb-2 text-lg font-semibold text-brand-brown">
        {heading}
      </Heading>
      <p className="mb-6 max-w-md text-text-secondary">{description}</p>
      {actionLabel && onAction && (
        <ActionButton variant="primary" onClick={onAction}>
          {actionLabel}
        </ActionButton>
      )}
    </div>
  )
}
