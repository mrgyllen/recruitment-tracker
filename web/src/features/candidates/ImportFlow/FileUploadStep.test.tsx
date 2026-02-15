import { fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FileUploadStep } from './FileUploadStep'
import { render, screen } from '@/test-utils'

describe('FileUploadStep', () => {
  const defaultProps = {
    onStartImport: vi.fn(),
    isUploading: false,
  }

  it('should render upload area with instructions', () => {
    render(<FileUploadStep {...defaultProps} />)
    expect(screen.getByText(/drop an xlsx file/i)).toBeInTheDocument()
    expect(screen.getByText(/maximum file size: 10 mb/i)).toBeInTheDocument()
  })

  it('should disable Start Import button until file selected', () => {
    render(<FileUploadStep {...defaultProps} />)
    expect(screen.getByRole('button', { name: /start import/i })).toBeDisabled()
  })

  it('should show file name when valid file selected', async () => {
    const user = userEvent.setup()
    render(<FileUploadStep {...defaultProps} />)

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)

    expect(screen.getByText('workday.xlsx')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /start import/i }),
    ).toBeEnabled()
  })

  it('should reject non-xlsx files with error', () => {
    render(<FileUploadStep {...defaultProps} />)

    const file = new File(['content'], 'data.csv', { type: 'text/csv' })
    const input = screen.getByTestId('file-input')
    // Use fireEvent.change to bypass the accept attribute filtering
    fireEvent.change(input, { target: { files: [file] } })

    expect(screen.getByText(/only .xlsx files are accepted/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /start import/i })).toBeDisabled()
  })

  it('should show uploading state', () => {
    render(<FileUploadStep {...defaultProps} isUploading={true} />)
    expect(screen.getByRole('button', { name: /uploading/i })).toBeDisabled()
  })

  it('should call onStartImport when button clicked', async () => {
    const onStartImport = vi.fn()
    const user = userEvent.setup()
    render(<FileUploadStep {...defaultProps} onStartImport={onStartImport} />)

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    expect(onStartImport).toHaveBeenCalledWith(file)
  })

  it('should display server error when provided', () => {
    render(<FileUploadStep {...defaultProps} error="Upload failed" />)
    expect(screen.getByText('Upload failed')).toBeInTheDocument()
  })
})
