import { Check, GripVertical, Plus, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import {
  useAddWorkflowStep,
  useRemoveWorkflowStep,
  useReorderWorkflowSteps,
} from './hooks/useRecruitmentMutations'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ApiError } from '@/lib/api/httpClient'

export interface WorkflowStep {
  id: string
  name: string
  order: number
}

interface WorkflowStepEditorBaseProps {
  steps: WorkflowStep[]
  disabled?: boolean
}

interface CreateModeProps extends WorkflowStepEditorBaseProps {
  mode?: 'create'
  onChange: (steps: WorkflowStep[]) => void
  recruitmentId?: never
}

interface EditModeProps extends WorkflowStepEditorBaseProps {
  mode: 'edit'
  recruitmentId: string
  onChange?: never
}

type WorkflowStepEditorProps = CreateModeProps | EditModeProps

let nextId = 1
function generateStepId(): string {
  return `step-${Date.now()}-${nextId++}`
}

export function WorkflowStepEditor(props: WorkflowStepEditorProps) {
  const { steps, disabled = false } = props

  if (props.mode === 'edit') {
    return (
      <EditModeEditor
        steps={steps}
        disabled={disabled}
        recruitmentId={props.recruitmentId}
      />
    )
  }

  return (
    <CreateModeEditor
      steps={steps}
      disabled={disabled}
      onChange={props.onChange}
    />
  )
}

function CreateModeEditor({
  steps,
  disabled,
  onChange,
}: {
  steps: WorkflowStep[]
  disabled: boolean
  onChange: (steps: WorkflowStep[]) => void
}) {
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
    <StepEditorLayout
      steps={steps}
      disabled={disabled}
      onAdd={handleAdd}
      onRemove={handleRemove}
      onMoveUp={handleMoveUp}
      onMoveDown={handleMoveDown}
      onNameChange={handleNameChange}
    />
  )
}

function EditModeEditor({
  steps,
  disabled,
  recruitmentId,
}: {
  steps: WorkflowStep[]
  disabled: boolean
  recruitmentId: string
}) {
  const addMutation = useAddWorkflowStep(recruitmentId)
  const removeMutation = useRemoveWorkflowStep(recruitmentId)
  const reorderMutation = useReorderWorkflowSteps(recruitmentId)
  const [showAddInput, setShowAddInput] = useState(false)
  const [newStepName, setNewStepName] = useState('')
  const [removeError, setRemoveError] = useState<string | null>(null)

  function handleAdd() {
    setShowAddInput(true)
    setNewStepName('')
  }

  function handleConfirmAdd() {
    if (!newStepName.trim()) return
    addMutation.mutate(
      { name: newStepName.trim(), order: steps.length + 1 },
      {
        onSuccess: () => {
          setShowAddInput(false)
          setNewStepName('')
        },
      },
    )
  }

  function handleCancelAdd() {
    setShowAddInput(false)
    setNewStepName('')
  }

  function handleRemove(id: string) {
    setRemoveError(null)
    removeMutation.mutate(id, {
      onError: (error) => {
        if (error instanceof ApiError && error.status === 409) {
          setRemoveError(error.problemDetails.title)
        }
      },
    })
  }

  function handleMoveUp(index: number) {
    if (index === 0) return
    const reordered = [...steps]
    ;[reordered[index - 1], reordered[index]] = [
      reordered[index],
      reordered[index - 1],
    ]
    reorderMutation.mutate({
      steps: reordered.map((s, i) => ({ stepId: s.id, order: i + 1 })),
    })
  }

  function handleMoveDown(index: number) {
    if (index === steps.length - 1) return
    const reordered = [...steps]
    ;[reordered[index], reordered[index + 1]] = [
      reordered[index + 1],
      reordered[index],
    ]
    reorderMutation.mutate({
      steps: reordered.map((s, i) => ({ stepId: s.id, order: i + 1 })),
    })
  }

  return (
    <div className="space-y-3">
      <StepEditorLayout
        steps={steps}
        disabled={disabled}
        onAdd={handleAdd}
        onRemove={handleRemove}
        onMoveUp={handleMoveUp}
        onMoveDown={handleMoveDown}
      />
      {removeError && (
        <p className="text-destructive text-sm">{removeError}</p>
      )}
      {showAddInput && (
        <div className="flex items-center gap-2">
          <Input
            value={newStepName}
            onChange={(e) => setNewStepName(e.target.value)}
            placeholder="New step name"
            disabled={addMutation.isPending}
            className="flex-1"
            autoFocus
          />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={handleConfirmAdd}
            disabled={addMutation.isPending || !newStepName.trim()}
            aria-label="Confirm add step"
          >
            <Check className="h-4 w-4" />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={handleCancelAdd}
            disabled={addMutation.isPending}
            aria-label="Cancel add step"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      )}
    </div>
  )
}

function StepEditorLayout({
  steps,
  disabled,
  onAdd,
  onRemove,
  onMoveUp,
  onMoveDown,
  onNameChange,
}: {
  steps: WorkflowStep[]
  disabled: boolean
  onAdd: () => void
  onRemove: (id: string) => void
  onMoveUp: (index: number) => void
  onMoveDown: (index: number) => void
  onNameChange?: (id: string, name: string) => void
}) {
  return (
    <>
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Workflow Steps</h3>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onAdd}
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
                onClick={() => onMoveUp(index)}
                disabled={disabled || index === 0}
                className="text-muted-foreground hover:text-foreground disabled:opacity-30"
                aria-label={`Move ${step.name || 'step'} up`}
              >
                <GripVertical className="h-3 w-3 rotate-90 scale-x-[-1]" />
              </button>
              <button
                type="button"
                onClick={() => onMoveDown(index)}
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
              onChange={
                onNameChange
                  ? (e) => onNameChange(step.id, e.target.value)
                  : undefined
              }
              readOnly={!onNameChange}
              placeholder="Step name"
              disabled={disabled}
              className="flex-1"
              aria-label={`Step ${step.order} name`}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={() => onRemove(step.id)}
              disabled={disabled}
              aria-label={`Remove ${step.name || 'step'}`}
            >
              <Trash2 className="h-4 w-4 text-destructive" />
            </Button>
          </li>
        ))}
      </ul>
    </>
  )
}
