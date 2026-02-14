import { describe, expect, it } from 'vitest'
import { fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DocumentUpload } from './DocumentUpload'
import { render, screen } from '@/test-utils'
import type { CandidateDocumentDto } from '@/lib/api/candidates.types'

const defaultProps = {
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  candidateId: 'cand-1111-1111-1111-111111111111',
  existingDocument: null as CandidateDocumentDto | null,
  isClosed: false,
}

describe('DocumentUpload', () => {
  it('should render upload area when no document exists', () => {
    render(<DocumentUpload {...defaultProps} />)
    expect(screen.getByText(/no cv uploaded/i)).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /upload cv/i }),
    ).toBeInTheDocument()
  })

  it('should display current document info when document exists', () => {
    const doc: CandidateDocumentDto = {
      id: 'doc-1',
      candidateId: defaultProps.candidateId,
      documentType: 'CV',
      blobStorageUrl: 'blob://cv.pdf',
      uploadedAt: '2026-02-14T12:00:00Z',
    }
    render(<DocumentUpload {...defaultProps} existingDocument={doc} />)
    expect(screen.getByText(/cv uploaded/i)).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /replace/i }),
    ).toBeInTheDocument()
  })

  it('should validate file type and reject non-PDF', () => {
    render(<DocumentUpload {...defaultProps} />)

    const input = screen.getByTestId('file-input') as HTMLInputElement
    const textFile = new File(['hello'], 'test.txt', { type: 'text/plain' })
    // Use fireEvent directly to bypass the accept attribute filtering in userEvent
    fireEvent.change(input, { target: { files: [textFile] } })

    expect(screen.getByText(/only pdf files/i)).toBeInTheDocument()
  })

  it('should validate file size and reject files over 10 MB', async () => {
    const user = userEvent.setup()
    render(<DocumentUpload {...defaultProps} />)

    const input = screen.getByTestId('file-input') as HTMLInputElement
    const largeFile = new File(
      [new ArrayBuffer(11 * 1024 * 1024)],
      'large.pdf',
      { type: 'application/pdf' },
    )
    await user.upload(input, largeFile)

    expect(screen.getByText(/must not exceed 10 mb/i)).toBeInTheDocument()
  })

  it('should hide upload controls when recruitment is closed', () => {
    render(<DocumentUpload {...defaultProps} isClosed={true} />)
    expect(
      screen.queryByRole('button', { name: /upload cv/i }),
    ).not.toBeInTheDocument()
  })

  it('should show replacement confirmation when document exists', async () => {
    const user = userEvent.setup()
    const doc: CandidateDocumentDto = {
      id: 'doc-1',
      candidateId: defaultProps.candidateId,
      documentType: 'CV',
      blobStorageUrl: 'blob://cv.pdf',
      uploadedAt: '2026-02-14T12:00:00Z',
    }
    render(<DocumentUpload {...defaultProps} existingDocument={doc} />)

    const input = screen.getByTestId('file-input') as HTMLInputElement
    const file = new File(['pdf-content'], 'cv.pdf', {
      type: 'application/pdf',
    })
    await user.upload(input, file)

    expect(
      screen.getByText(/this will replace the existing cv/i),
    ).toBeInTheDocument()
  })
})
