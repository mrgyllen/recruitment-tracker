export type StatusVariant = 'pass' | 'fail' | 'hold' | 'stale' | 'not-started'

export interface StatusBadgeProps {
  status: StatusVariant
  'aria-label'?: string
}
