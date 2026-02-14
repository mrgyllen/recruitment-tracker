import { useState } from 'react'
import { ChevronDown } from 'lucide-react'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { cn } from '@/lib/utils'

export function WorkdayGuide() {
  const [isOpen, setIsOpen] = useState(false)

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <CollapsibleTrigger className="flex w-full items-center gap-2 rounded-md border p-3 text-sm font-medium hover:bg-muted/50">
        <ChevronDown
          className={cn('size-4 transition-transform', isOpen && 'rotate-180')}
        />
        Workday export instructions
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-2 space-y-2 rounded-md border bg-muted/30 p-4 text-sm">
        <ol className="list-decimal space-y-1.5 pl-4">
          <li>
            In Workday, navigate to your recruitment and open the candidate
            list.
          </li>
          <li>
            Select <strong>all candidates</strong> (always export the full
            list to avoid duplicates on re-import).
          </li>
          <li>
            Click <strong>Export to Excel</strong> and choose the XLSX format.
          </li>
          <li>
            Ensure the export includes: Full Name, Email, Phone, Location,
            and Date Applied columns.
          </li>
          <li>Upload the exported file here.</li>
        </ol>
        <p className="text-muted-foreground">
          Tip: Always export all candidates, not just new ones. The system
          handles deduplication automatically.
        </p>
      </CollapsibleContent>
    </Collapsible>
  )
}
