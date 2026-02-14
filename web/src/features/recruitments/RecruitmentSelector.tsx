import { ChevronDown } from 'lucide-react'
import { useNavigate, useParams } from 'react-router'
import { useRecruitments } from './hooks/useRecruitments'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

export function RecruitmentSelector() {
  const { recruitmentId } = useParams<{ recruitmentId: string }>()
  const navigate = useNavigate()
  const { data } = useRecruitments()

  if (!recruitmentId || !data) return null

  const current = data.items.find((r) => r.id === recruitmentId)
  if (!current) return null

  const hasMultiple = data.items.length > 1

  return (
    <nav aria-label="Breadcrumb" className="flex items-center gap-1 text-sm">
      <span className="text-muted-foreground">/</span>
      {hasMultiple ? (
        <DropdownMenu>
          <DropdownMenuTrigger className="flex items-center gap-1 font-medium hover:underline focus:outline-none">
            {current.title}
            <ChevronDown className="h-3.5 w-3.5" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            {data.items.map((item) => (
              <DropdownMenuItem
                key={item.id}
                onClick={() => void navigate(`/recruitments/${item.id}`)}
                className="flex items-center justify-between gap-4"
              >
                <span>{item.title}</span>
                <Badge
                  variant={
                    item.status === 'Active' ? 'default' : 'secondary'
                  }
                  className="text-xs"
                >
                  {item.status}
                </Badge>
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      ) : (
        <span className="font-medium">{current.title}</span>
      )}
    </nav>
  )
}
