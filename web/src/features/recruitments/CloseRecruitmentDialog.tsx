import { useCloseRecruitment } from './hooks/useRecruitmentMutations'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'

interface CloseRecruitmentDialogProps {
  recruitmentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CloseRecruitmentDialog({
  recruitmentId,
  open,
  onOpenChange,
}: CloseRecruitmentDialogProps) {
  const closeMutation = useCloseRecruitment(recruitmentId)
  const toast = useAppToast()

  function handleConfirm() {
    closeMutation.mutate(undefined, {
      onSuccess: () => {
        toast.success('Recruitment closed')
        onOpenChange(false)
      },
      onError: (error) => {
        if (error instanceof ApiError) {
          toast.error(error.problemDetails.title)
        } else {
          toast.error('Failed to close recruitment')
        }
      },
    })
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Close Recruitment</AlertDialogTitle>
          <AlertDialogDescription>
            This will lock the recruitment from further changes. No edits can be
            made to candidates, workflow steps, or team members after closing.
            Data will be retained for the configured retention period before
            anonymization.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={closeMutation.isPending}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {closeMutation.isPending ? 'Closing...' : 'Close Recruitment'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
