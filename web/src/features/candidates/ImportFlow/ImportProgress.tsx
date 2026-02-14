import { Loader2 } from 'lucide-react'
import { Progress } from '@/components/ui/progress'

interface ImportProgressProps {
  sourceFileName: string
}

export function ImportProgress({ sourceFileName }: ImportProgressProps) {
  return (
    <div className="flex flex-col items-center gap-6 p-8">
      <Loader2 className="text-primary size-10 animate-spin" />
      <div className="text-center">
        <p className="font-medium">Importing candidates...</p>
        <p className="text-muted-foreground mt-1 text-sm">
          Processing {sourceFileName}
        </p>
      </div>
      <Progress className="w-full" />
    </div>
  )
}
