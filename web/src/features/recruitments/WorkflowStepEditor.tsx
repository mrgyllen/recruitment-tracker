import { GripVertical, Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export interface WorkflowStep {
  id: string
  name: string
  order: number
}

interface WorkflowStepEditorProps {
  steps: WorkflowStep[]
  onChange: (steps: WorkflowStep[]) => void
  disabled?: boolean
}

let nextId = 1
function generateStepId(): string {
  return `step-${Date.now()}-${nextId++}`
}

export function WorkflowStepEditor({
  steps,
  onChange,
  disabled = false,
}: WorkflowStepEditorProps) {
  function handleNameChange(id: string, name: string) {
    onChange(steps.map((s) => (s.id === id ? { ...s, name } : s)))
  }

  function handleRemove(id: string) {
    const updated = steps
      .filter((s) => s.id !== id)
      .map((s, i) => ({ ...s, order: i + 1 }))
    onChange(updated)
  }

  function handleAdd() {
    const newStep: WorkflowStep = {
      id: generateStepId(),
      name: '',
      order: steps.length + 1,
    }
    onChange([...steps, newStep])
  }

  function handleMoveUp(index: number) {
    if (index === 0) return
    const updated = [...steps]
    ;[updated[index - 1], updated[index]] = [updated[index], updated[index - 1]]
    onChange(updated.map((s, i) => ({ ...s, order: i + 1 })))
  }

  function handleMoveDown(index: number) {
    if (index === steps.length - 1) return
    const updated = [...steps]
    ;[updated[index], updated[index + 1]] = [updated[index + 1], updated[index]]
    onChange(updated.map((s, i) => ({ ...s, order: i + 1 })))
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Workflow Steps</h3>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleAdd}
          disabled={disabled}
        >
          <Plus className="mr-1 h-4 w-4" />
          Add Step
        </Button>
      </div>
      {steps.length === 0 && (
        <p className="text-muted-foreground text-sm">
          No workflow steps defined. Add steps to define the hiring process.
        </p>
      )}
      <ul className="space-y-2" role="list" aria-label="Workflow steps">
        {steps.map((step, index) => (
          <li
            key={step.id}
            className="flex items-center gap-2 rounded-md border p-2"
          >
            <div className="flex flex-col">
              <button
                type="button"
                onClick={() => handleMoveUp(index)}
                disabled={disabled || index === 0}
                className="text-muted-foreground hover:text-foreground disabled:opacity-30"
                aria-label={`Move ${step.name || 'step'} up`}
              >
                <GripVertical className="h-3 w-3 rotate-90 scale-x-[-1]" />
              </button>
              <button
                type="button"
                onClick={() => handleMoveDown(index)}
                disabled={disabled || index === steps.length - 1}
                className="text-muted-foreground hover:text-foreground disabled:opacity-30"
                aria-label={`Move ${step.name || 'step'} down`}
              >
                <GripVertical className="h-3 w-3 rotate-90" />
              </button>
            </div>
            <span className="text-muted-foreground w-6 text-center text-sm">
              {step.order}
            </span>
            <Input
              value={step.name}
              onChange={(e) => handleNameChange(step.id, e.target.value)}
              placeholder="Step name"
              disabled={disabled}
              className="flex-1"
              aria-label={`Step ${step.order} name`}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={() => handleRemove(step.id)}
              disabled={disabled}
              aria-label={`Remove ${step.name || 'step'}`}
            >
              <Trash2 className="h-4 w-4 text-destructive" />
            </Button>
          </li>
        ))}
      </ul>
    </div>
  )
}
