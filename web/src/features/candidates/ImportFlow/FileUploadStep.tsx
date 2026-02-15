import { FileSpreadsheet, Upload } from 'lucide-react'
import { useRef, useState } from 'react'
import { WorkdayGuide } from './WorkdayGuide'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const MAX_FILE_SIZE = 10 * 1024 * 1024 // 10 MB
const ACCEPTED_EXTENSION = '.xlsx'

interface FileUploadStepProps {
  onStartImport: (file: File) => void
  isUploading: boolean
  error?: string | null
}

export function FileUploadStep({
  onStartImport,
  isUploading,
  error,
}: FileUploadStepProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [isDragOver, setIsDragOver] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  function validateFile(file: File): string | null {
    if (!file.name.toLowerCase().endsWith(ACCEPTED_EXTENSION)) {
      return 'Only .xlsx files are accepted'
    }
    if (file.size > MAX_FILE_SIZE) {
      return 'File size must be 10 MB or less'
    }
    return null
  }

  function handleFileSelect(file: File) {
    const err = validateFile(file)
    if (err) {
      setValidationError(err)
      setSelectedFile(null)
      return
    }
    setValidationError(null)
    setSelectedFile(file)
  }

  function handleInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) handleFileSelect(file)
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragOver(false)
    const file = e.dataTransfer.files[0]
    if (file) handleFileSelect(file)
  }

  function handleDragOver(e: React.DragEvent) {
    e.preventDefault()
    setIsDragOver(true)
  }

  function handleDragLeave() {
    setIsDragOver(false)
  }

  return (
    <div className="space-y-4 p-4">
      <WorkdayGuide />

      <div
        className={cn(
          'flex flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed p-8 transition-colors',
          isDragOver && 'border-primary bg-primary/5',
          validationError && 'border-destructive',
          !isDragOver && !validationError && 'border-muted-foreground/25',
        )}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        role="button"
        tabIndex={0}
        onClick={() => inputRef.current?.click()}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click()
        }}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx"
          className="hidden"
          onChange={handleInputChange}
          data-testid="file-input"
        />
        {selectedFile ? (
          <>
            <FileSpreadsheet className="size-8 text-primary" />
            <p className="font-medium">{selectedFile.name}</p>
            <p className="text-muted-foreground text-sm">
              {(selectedFile.size / 1024).toFixed(0)} KB
            </p>
          </>
        ) : (
          <>
            <Upload className="text-muted-foreground size-8" />
            <p className="text-sm font-medium">
              Drop an XLSX file here or click to browse
            </p>
            <p className="text-muted-foreground text-xs">
              Maximum file size: 10 MB
            </p>
          </>
        )}
      </div>

      {validationError && (
        <p className="text-destructive text-sm" role="alert">
          {validationError}
        </p>
      )}

      {error && (
        <p className="text-destructive text-sm" role="alert">
          {error}
        </p>
      )}

      <Button
        className="w-full"
        disabled={!selectedFile || isUploading}
        onClick={() => selectedFile && onStartImport(selectedFile)}
      >
        {isUploading ? 'Uploading...' : 'Start Import'}
      </Button>
    </div>
  )
}
