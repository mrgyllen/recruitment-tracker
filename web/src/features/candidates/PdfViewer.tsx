import { useCallback, useEffect, useRef, useState } from 'react'
import { Document, Page } from 'react-pdf'
import '@/lib/pdfWorkerConfig'
import 'react-pdf/dist/Page/AnnotationLayer.css'
import 'react-pdf/dist/Page/TextLayer.css'

interface PdfViewerProps {
  url: string | null
  onError?: () => void
}

export function PdfViewer({ url, onError }: PdfViewerProps) {
  const [numPages, setNumPages] = useState<number>(0)
  const [containerWidth, setContainerWidth] = useState<number>(600)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const el = containerRef.current
    if (!el || typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        setContainerWidth(entry.contentRect.width)
      }
    })
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  const handleLoadSuccess = useCallback(
    ({ numPages: total }: { numPages: number }) => {
      setNumPages(total)
    },
    [],
  )

  const handleLoadError = useCallback(() => {
    onError?.()
  }, [onError])

  if (url === null || url === undefined) {
    return null
  }

  return (
    <div ref={containerRef} className="overflow-auto rounded-md border">
      <Document
        file={url || undefined}
        onLoadSuccess={handleLoadSuccess}
        onLoadError={handleLoadError}
        loading={
          <div
            data-testid="pdf-loading"
            className="flex items-center justify-center p-8"
          >
            <p className="text-muted-foreground text-sm">Loading PDF...</p>
          </div>
        }
        error={
          <div
            data-testid="pdf-error"
            className="flex items-center justify-center p-8"
          >
            <p className="text-muted-foreground text-sm">
              Failed to load PDF.
            </p>
          </div>
        }
      >
        {Array.from({ length: numPages }, (_, index) => (
          <Page
            key={`page-${index + 1}`}
            pageNumber={index + 1}
            width={containerWidth}
            renderTextLayer={true}
            renderAnnotationLayer={true}
          />
        ))}
      </Document>
    </div>
  )
}
