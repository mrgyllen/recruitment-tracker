import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { PdfViewer } from './PdfViewer'

// Mock react-pdf since jsdom cannot render canvas
vi.mock('react-pdf', () => {
  const Document = ({
    onLoadSuccess,
    children,
    loading,
    error,
    file,
  }: { onLoadSuccess?: (info: { numPages: number }) => void; children: React.ReactNode; loading?: React.ReactNode; error?: React.ReactNode; file?: string }) => {
    if (!file) {
      // Simulate error for empty/invalid URL
      return <>{error}</>
    }
    // Simulate successful load with 2 pages
    setTimeout(() => onLoadSuccess?.({ numPages: 2 }), 0)
    return (
      <div data-testid="pdf-document">
        {loading}
        {children}
      </div>
    )
  }
  const Page = ({ pageNumber }: { pageNumber: number }) => (
    <div data-testid={`pdf-page-${pageNumber}`}>Page {pageNumber}</div>
  )
  return {
    Document,
    Page,
    pdfjs: { GlobalWorkerOptions: { workerSrc: '' } },
  }
})

describe('PdfViewer', () => {
  it('should render the PDF document with the given URL', async () => {
    render(
      <PdfViewer url="https://storage.blob.core.windows.net/doc.pdf?sig=mock" />,
    )

    await waitFor(() => {
      expect(screen.getByTestId('pdf-document')).toBeInTheDocument()
    })
  })

  it('should render pages after document loads', async () => {
    render(
      <PdfViewer url="https://storage.blob.core.windows.net/doc.pdf?sig=mock" />,
    )

    await waitFor(() => {
      expect(screen.getByTestId('pdf-page-1')).toBeInTheDocument()
    })
    expect(screen.getByTestId('pdf-page-2')).toBeInTheDocument()
  })

  it('should show loading indicator', () => {
    render(
      <PdfViewer url="https://storage.blob.core.windows.net/doc.pdf?sig=mock" />,
    )

    // Before onLoadSuccess fires, the loading content is rendered
    expect(screen.getByTestId('pdf-loading')).toBeInTheDocument()
  })

  it('should show error state when file prop is empty', () => {
    render(<PdfViewer url="" onError={vi.fn()} />)

    expect(screen.getByTestId('pdf-error')).toBeInTheDocument()
  })

  it('should render nothing when url is null', () => {
    const { container } = render(<PdfViewer url={null} />)
    expect(container.firstChild).toBeNull()
  })
})
