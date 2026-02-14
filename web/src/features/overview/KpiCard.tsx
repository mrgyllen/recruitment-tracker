import { cn } from '@/lib/utils'

interface KpiCardProps {
  label: string
  value: number
  variant?: 'default' | 'warning'
}

export function KpiCard({ label, value, variant = 'default' }: KpiCardProps) {
  return (
    <div
      className="rounded-md border border-stone-200 bg-amber-50 p-4"
      aria-label={`${label}: ${value}`}
    >
      <p
        className={cn(
          'text-2xl font-bold',
          variant === 'warning' ? 'text-amber-600' : 'text-stone-900',
        )}
      >
        {value}
      </p>
      <p className="text-sm text-stone-600">{label}</p>
    </div>
  )
}
