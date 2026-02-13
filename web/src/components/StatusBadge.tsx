import { Check, Clock, Pause, X } from 'lucide-react'
import type { StatusBadgeProps, StatusVariant } from './StatusBadge.types'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

const variantConfig: Record<
  StatusVariant,
  {
    label: string
    text: string
    icon?: React.ComponentType<{ className?: string }>
    className: string
  }
> = {
  pass: {
    label: 'Pass outcome',
    text: 'Pass',
    icon: Check,
    className: 'bg-status-pass-bg text-status-pass border-transparent',
  },
  fail: {
    label: 'Fail outcome',
    text: 'Fail',
    icon: X,
    className: 'bg-status-fail-bg text-status-fail border-transparent',
  },
  hold: {
    label: 'Hold outcome',
    text: 'Hold',
    icon: Pause,
    className: 'bg-status-hold-bg text-status-hold border-transparent',
  },
  stale: {
    label: 'Stale outcome',
    text: 'Stale',
    icon: Clock,
    className:
      'border-status-hold text-status-hold bg-transparent border',
  },
  'not-started': {
    label: 'Not Started outcome',
    text: 'Not Started',
    className: 'bg-border-subtle text-text-secondary border-transparent',
  },
}

export function StatusBadge({
  status,
  'aria-label': ariaLabel,
}: StatusBadgeProps) {
  const config = variantConfig[status]
  const Icon = config.icon

  return (
    <Badge
      aria-label={ariaLabel ?? config.label}
      className={cn(
        'inline-flex items-center gap-1 font-medium',
        config.className,
      )}
    >
      {Icon && <Icon className="h-3.5 w-3.5" />}
      {config.text}
    </Badge>
  )
}
