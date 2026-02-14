import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod/v4'
import { useUpdateRecruitment } from './hooks/useRecruitmentMutations'
import type { RecruitmentDetail } from '@/lib/api/recruitments.types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'

const editRecruitmentSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(2000).optional(),
  jobRequisitionId: z.string().max(100).optional(),
})

type FormValues = z.infer<typeof editRecruitmentSchema>

interface EditRecruitmentFormProps {
  recruitment: RecruitmentDetail
}

export function EditRecruitmentForm({ recruitment }: EditRecruitmentFormProps) {
  const toast = useAppToast()
  const updateMutation = useUpdateRecruitment(recruitment.id)
  const isClosed = recruitment.status === 'Closed'

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(editRecruitmentSchema),
    defaultValues: {
      title: recruitment.title,
      description: recruitment.description ?? '',
      jobRequisitionId: recruitment.jobRequisitionId ?? '',
    },
  })

  useEffect(() => {
    reset({
      title: recruitment.title,
      description: recruitment.description ?? '',
      jobRequisitionId: recruitment.jobRequisitionId ?? '',
    })
  }, [recruitment, reset])

  function onSubmit(data: FormValues) {
    updateMutation.mutate(
      {
        title: data.title,
        description: data.description || null,
        jobRequisitionId: data.jobRequisitionId || null,
      },
      {
        onSuccess: () => {
          toast.success('Recruitment updated')
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            toast.error(error.problemDetails.title)
          } else {
            toast.error('Failed to update recruitment')
          }
        },
      },
    )
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="edit-title">Title *</Label>
        <Input
          id="edit-title"
          {...register('title')}
          disabled={isClosed}
          aria-invalid={!!errors.title}
          aria-describedby={errors.title ? 'edit-title-error' : undefined}
        />
        {errors.title && (
          <p id="edit-title-error" className="text-destructive text-sm">
            {errors.title.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="edit-description">Description (optional)</Label>
        <Textarea
          id="edit-description"
          {...register('description')}
          disabled={isClosed}
          rows={3}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="edit-jobRequisitionId">
          Job Requisition Reference (optional)
        </Label>
        <Input
          id="edit-jobRequisitionId"
          {...register('jobRequisitionId')}
          disabled={isClosed}
        />
      </div>

      {!isClosed && (
        <div className="flex justify-end">
          <Button
            type="submit"
            disabled={updateMutation.isPending || !isDirty}
          >
            {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
          </Button>
        </div>
      )}
    </form>
  )
}
