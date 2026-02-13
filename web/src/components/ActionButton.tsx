import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type ActionButtonVariant = 'primary' | 'secondary' | 'destructive'

interface ActionButtonProps
  extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'children'> {
  variant: ActionButtonVariant
  loading?: boolean
  loadingText?: string
  children: React.ReactNode
}

const variantStyles: Record<ActionButtonVariant, string> = {
  primary: 'bg-interactive text-white hover:bg-interactive-hover',
  secondary:
    'border border-brand-brown text-brand-brown bg-transparent hover:bg-border-subtle',
  destructive:
    'border border-status-fail text-status-fail bg-transparent hover:bg-status-fail-bg',
}

export function ActionButton({
  variant,
  loading = false,
  loadingText,
  children,
  className,
  disabled,
  ...props
}: ActionButtonProps) {
  return (
    <Button
      className={cn(variantStyles[variant], className)}
      disabled={disabled || loading}
      {...props}
    >
      {loading ? (
        <>
          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          {loadingText ?? children}
        </>
      ) : (
        children
      )}
    </Button>
  )
}
