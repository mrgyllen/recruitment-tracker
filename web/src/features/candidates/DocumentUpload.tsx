import { useRef, useState } from 'react'
import { useDocumentUpload } from './hooks/useDocumentUpload'
import type { CandidateDocumentDto } from '@/lib/api/candidates.types'
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
import { Button } from '@/components/ui/button'
import { useAppToast } from '@/hooks/useAppToast'
import { ApiError } from '@/lib/api/httpClient'

const MAX_FILE_SIZE = 10 * 1024 * 1024 // 10 MB

interface DocumentUploadProps {
  recruitmentId: string
  candidateId: string
  existingDocument: CandidateDocumentDto | null
  isClosed: boolean
}

export function DocumentUpload({
  recruitmentId,
  candidateId,
  existingDocument,
  isClosed,
}: DocumentUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [pendingFile, setPendingFile] = useState<File | null>(null)
  const [showConfirm, setShowConfirm] = useState(false)
  const toast = useAppToast()
  const uploadMutation = useDocumentUpload(recruitmentId, candidateId)

  if (isClosed) {
    return existingDocument ? (
      <div className="text-muted-foreground text-sm">
        CV uploaded on{' '}
        {new Date(existingDocument.uploadedAt).toLocaleDateString()}
      </div>
    ) : (
      <div className="text-muted-foreground text-sm">No CV uploaded</div>
    )
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    setValidationError(null)
    const file = e.target.files?.[0]
    if (!file) return

    if (!file.name.endsWith('.pdf')) {
      setValidationError('Only PDF files are accepted.')
      return
    }
    if (file.size > MAX_FILE_SIZE) {
      setValidationError('File must not exceed 10 MB.')
      return
    }

    if (existingDocument) {
      setPendingFile(file)
      setShowConfirm(true)
    } else {
      doUpload(file)
    }
  }

  function doUpload(file: File) {
    uploadMutation.mutate(file, {
      onSuccess: () => {
        toast.success('CV uploaded successfully')
        if (inputRef.current) inputRef.current.value = ''
      },
      onError: (error) => {
        if (error instanceof ApiError) {
          toast.error(error.problemDetails.title)
        } else {
          toast.error('Failed to upload CV')
        }
      },
    })
  }

  function handleConfirmReplace() {
    if (pendingFile) {
      doUpload(pendingFile)
      setPendingFile(null)
    }
    setShowConfirm(false)
  }

  return (
    <div className="space-y-2">
      {existingDocument ? (
        <p className="text-sm">
          CV uploaded on{' '}
          {new Date(existingDocument.uploadedAt).toLocaleDateString()}
        </p>
      ) : (
        <p className="text-muted-foreground text-sm">No CV uploaded</p>
      )}

      <input
        ref={inputRef}
        data-testid="file-input"
        type="file"
        accept=".pdf"
        className="hidden"
        onChange={handleFileChange}
      />

      <Button
        variant="outline"
        size="sm"
        disabled={uploadMutation.isPending}
        onClick={() => inputRef.current?.click()}
      >
        {uploadMutation.isPending
          ? 'Uploading...'
          : existingDocument
            ? 'Replace CV'
            : 'Upload CV'}
      </Button>

      {validationError && (
        <p className="text-destructive text-sm">{validationError}</p>
      )}

      <AlertDialog open={showConfirm} onOpenChange={setShowConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Replace CV</AlertDialogTitle>
            <AlertDialogDescription>
              This will replace the existing CV. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel
              onClick={() => {
                setPendingFile(null)
                if (inputRef.current) inputRef.current.value = ''
              }}
            >
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirmReplace}>
              Replace
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
