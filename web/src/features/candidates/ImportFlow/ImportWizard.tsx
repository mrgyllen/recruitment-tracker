import { useEffect, useState } from 'react'
import { FileUploadStep } from './FileUploadStep'
import { ImportProgress } from './ImportProgress'
import { ImportSummary } from './ImportSummary'
import { MatchReviewStep, type FlaggedRow } from './MatchReviewStep'
import { useImportSession } from './hooks/useImportSession'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { useAppToast } from '@/hooks/useAppToast'
import { importApi } from '@/lib/api/import'
import { ApiError } from '@/lib/api/httpClient'
import { useQueryClient } from '@tanstack/react-query'

type WizardStep = 'upload' | 'processing' | 'summary' | 'matchReview'

interface ImportWizardProps {
  recruitmentId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ImportWizard({
  recruitmentId,
  open,
  onOpenChange,
}: ImportWizardProps) {
  const [step, setStep] = useState<WizardStep>('upload')
  const [importSessionId, setImportSessionId] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [sourceFileName, setSourceFileName] = useState('')

  const toast = useAppToast()
  const queryClient = useQueryClient()
  const { data: session } = useImportSession(
    step !== 'upload' ? importSessionId : null,
  )

  useEffect(() => {
    if (step !== 'processing' || !session) return
    if (session.status === 'Completed') {
      setStep('summary')
    } else if (session.status === 'Failed') {
      setUploadError(session.failureReason ?? 'Import failed')
      setStep('upload')
    }
  }, [step, session])

  async function handleStartImport(file: File) {
    setIsUploading(true)
    setUploadError(null)
    setSourceFileName(file.name)
    try {
      const response = await importApi.startImport(recruitmentId, file)
      setImportSessionId(response.importSessionId)
      setStep('processing')
    } catch (error) {
      if (error instanceof ApiError) {
        setUploadError(error.problemDetails.title)
      } else {
        setUploadError('Upload failed')
      }
    } finally {
      setIsUploading(false)
    }
  }

  function handleClose() {
    if (step === 'summary' && session?.status === 'Completed') {
      const count = session.createdCount + session.updatedCount
      if (count > 0) {
        toast.success(`${count} candidates imported`)
      }
    }
    void queryClient.invalidateQueries({ queryKey: ['candidates'] })
    setStep('upload')
    setImportSessionId(null)
    setUploadError(null)
    onOpenChange(false)
  }

  return (
    <Sheet open={open} onOpenChange={(isOpen) => !isOpen && handleClose()}>
      <SheetContent
        side="right"
        className="w-[600px] max-w-full overflow-y-auto sm:max-w-[600px]"
      >
        <SheetHeader>
          <SheetTitle>Import Candidates</SheetTitle>
          <SheetDescription>
            Upload a Workday export file to import candidates
          </SheetDescription>
        </SheetHeader>

        {step === 'upload' && (
          <FileUploadStep
            onStartImport={handleStartImport}
            isUploading={isUploading}
            error={uploadError}
          />
        )}

        {step === 'processing' && (
          <ImportProgress sourceFileName={sourceFileName} />
        )}

        {step === 'summary' && session && (
          <ImportSummary
            createdCount={session.createdCount}
            updatedCount={session.updatedCount}
            erroredCount={session.erroredCount}
            flaggedCount={session.flaggedCount}
            rowResults={session.rowResults}
            failureReason={session.failureReason}
            onReviewMatches={
              session.flaggedCount > 0
                ? () => setStep('matchReview')
                : undefined
            }
            onDone={handleClose}
            importDocuments={session.importDocuments}
            recruitmentId={recruitmentId}
            importSessionId={importSessionId ?? undefined}
          />
        )}

        {step === 'matchReview' && session && importSessionId && (
          <MatchReviewStep
            importSessionId={importSessionId}
            flaggedRows={session.rowResults.reduce<FlaggedRow[]>(
              (acc, r, i) => {
                if (r.action === 'Flagged') acc.push({ ...r, originalIndex: i })
                return acc
              },
              [],
            )}
            onDone={handleClose}
          />
        )}
      </SheetContent>
    </Sheet>
  )
}
