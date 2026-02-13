import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { z } from 'zod/v4'
import { useCreateRecruitment } from './hooks/useCreateRecruitment'
import { DEFAULT_WORKFLOW_STEPS } from './workflowDefaults'
import { WorkflowStepEditor, type WorkflowStep } from './WorkflowStepEditor'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'

const createRecruitmentSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(2000).optional(),
  jobRequisitionId: z.string().max(100).optional(),
})

type FormValues = z.infer<typeof createRecruitmentSchema>

export function CreateRecruitmentForm() {
  const navigate = useNavigate()
  const toast = useAppToast()
  const createMutation = useCreateRecruitment()
  const [steps, setSteps] = useState<WorkflowStep[]>(
    () => [...DEFAULT_WORKFLOW_STEPS],
  )

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(createRecruitmentSchema),
    defaultValues: {
      title: '',
      description: '',
      jobRequisitionId: '',
    },
  })

  function validateSteps(): string | null {
    const names = steps.map((s) => s.name.trim().toLowerCase())
    const hasDuplicates = new Set(names).size !== names.length
    if (hasDuplicates) return 'Workflow step names must be unique.'

    const hasEmpty = steps.some((s) => s.name.trim() === '')
    if (hasEmpty) return 'All workflow steps must have a name.'

    return null
  }

  async function onSubmit(data: FormValues) {
    const stepError = validateSteps()
    if (stepError) {
      toast.error(stepError)
      return
    }

    createMutation.mutate(
      {
        title: data.title,
        description: data.description || null,
        jobRequisitionId: data.jobRequisitionId || null,
        steps: steps.map((s) => ({ name: s.name.trim(), order: s.order })),
      },
      {
        onSuccess: (result) => {
          toast.success('Recruitment created successfully')
          void navigate(`/recruitments/${result.id}`)
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            toast.error(error.problemDetails.title)
          } else {
            toast.error('Failed to create recruitment')
          }
        },
      },
    )
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="mx-auto max-w-2xl space-y-6 p-6"
    >
      <h1 className="text-2xl font-semibold">Create Recruitment</h1>

      <div className="space-y-2">
        <Label htmlFor="title">Title *</Label>
        <Input
          id="title"
          {...register('title')}
          placeholder="e.g., Senior .NET Developer"
          aria-invalid={!!errors.title}
          aria-describedby={errors.title ? 'title-error' : undefined}
        />
        {errors.title && (
          <p id="title-error" className="text-destructive text-sm">
            {errors.title.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          {...register('description')}
          placeholder="Brief description of this recruitment"
          rows={3}
        />
        {errors.description && (
          <p className="text-destructive text-sm">
            {errors.description.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="jobRequisitionId">Job Requisition Reference</Label>
        <Input
          id="jobRequisitionId"
          {...register('jobRequisitionId')}
          placeholder="Optional reference ID"
        />
      </div>

      <WorkflowStepEditor
        steps={steps}
        onChange={setSteps}
        disabled={createMutation.isPending}
      />

      <div className="flex justify-end gap-3">
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/')}
          disabled={createMutation.isPending}
        >
          Cancel
        </Button>
        <Button type="submit" disabled={createMutation.isPending}>
          {createMutation.isPending ? 'Creating...' : 'Create Recruitment'}
        </Button>
      </div>
    </form>
  )
}
