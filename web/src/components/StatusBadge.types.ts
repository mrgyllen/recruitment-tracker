export type StatusVariant = 'pass' | 'fail' | 'hold' | 'stale' | 'not-started'

export interface StatusBadgeProps {
  status: StatusVariant
  'aria-label'?: string
}

export function toStatusVariant(status: string | null): StatusVariant {
  switch (status) {
    case 'Pass':
      return 'pass'
    case 'Fail':
      return 'fail'
    case 'Hold':
      return 'hold'
    case 'NotStarted':
      return 'not-started'
    default:
      return 'not-started'
  }
}
