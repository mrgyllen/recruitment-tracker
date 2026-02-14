import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod/v4'
import { useCreateCandidate } from './hooks/useCandidateMutations'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'

const createCandidateSchema = z.object({
  fullName: z.string().min(1, 'Full name is required').max(200),
  email: z
    .string()
    .min(1, 'Email is required')
    .email('A valid email is required')
    .max(254),
  phoneNumber: z.string().max(30).optional().or(z.literal('')),
  location: z.string().max(200).optional().or(z.literal('')),
  dateApplied: z.string().optional(),
})

type FormValues = z.infer<typeof createCandidateSchema>

interface CreateCandidateFormProps {
  recruitmentId: string
  open?: boolean
  onOpenChange?: (open: boolean) => void
}

export function CreateCandidateForm({
  recruitmentId,
  open: controlledOpen,
  onOpenChange: controlledOnOpenChange,
}: CreateCandidateFormProps) {
  const [internalOpen, setInternalOpen] = useState(false)
  const isControlled = controlledOpen !== undefined
  const open = isControlled ? controlledOpen : internalOpen
  const setOpen = isControlled ? controlledOnOpenChange! : setInternalOpen

  const toast = useAppToast()
  const createMutation = useCreateCandidate(recruitmentId)

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(createCandidateSchema),
    defaultValues: {
      fullName: '',
      email: '',
      phoneNumber: '',
      location: '',
      dateApplied: new Date().toISOString().split('T')[0],
    },
  })

  function onSubmit(data: FormValues) {
    createMutation.mutate(
      {
        fullName: data.fullName,
        email: data.email,
        phoneNumber: data.phoneNumber || null,
        location: data.location || null,
        dateApplied: data.dateApplied || null,
      },
      {
        onSuccess: () => {
          toast.success('Candidate added')
          reset()
          setOpen(false)
        },
        onError: (error) => {
          if (
            error instanceof ApiError &&
            error.problemDetails.title ===
              'A candidate with this email already exists in this recruitment'
          ) {
            setError('email', {
              message: error.problemDetails.title,
            })
          } else if (error instanceof ApiError) {
            toast.error(error.problemDetails.title)
          } else {
            toast.error('Failed to add candidate')
          }
        },
      },
    )
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      {!isControlled && (
        <DialogTrigger asChild>
          <Button>Add Candidate</Button>
        </DialogTrigger>
      )}
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add Candidate</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="candidate-fullName">Full Name *</Label>
            <Input
              id="candidate-fullName"
              {...register('fullName')}
              aria-invalid={!!errors.fullName}
              aria-describedby={
                errors.fullName ? 'candidate-fullName-error' : undefined
              }
            />
            {errors.fullName && (
              <p
                id="candidate-fullName-error"
                className="text-destructive text-sm"
              >
                {errors.fullName.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="candidate-email">Email *</Label>
            <Input
              id="candidate-email"
              type="email"
              {...register('email')}
              aria-invalid={!!errors.email}
              aria-describedby={
                errors.email ? 'candidate-email-error' : undefined
              }
            />
            {errors.email && (
              <p
                id="candidate-email-error"
                className="text-destructive text-sm"
              >
                {errors.email.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="candidate-phone">Phone (optional)</Label>
            <Input id="candidate-phone" {...register('phoneNumber')} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="candidate-location">Location (optional)</Label>
            <Input id="candidate-location" {...register('location')} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="candidate-dateApplied">
              Date Applied (optional)
            </Label>
            <Input
              id="candidate-dateApplied"
              type="date"
              {...register('dateApplied')}
            />
          </div>

          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Adding...' : 'Add Candidate'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
