import { describe, expect, it } from 'vitest'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { ImportWizard } from './ImportWizard'
import {
  mockCompletedSession,
  mockProcessingSession,
} from '@/mocks/importHandlers'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

const recruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('ImportWizard', () => {
  it('should render as Sheet when opened', () => {
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )
    expect(screen.getByText('Import Candidates')).toBeInTheDocument()
    expect(
      screen.getByText(/upload a workday export file/i),
    ).toBeInTheDocument()
  })

  it('should show upload step initially', () => {
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )
    expect(screen.getByText(/drop an xlsx file/i)).toBeInTheDocument()
  })

  it('should transition to processing after file upload', async () => {
    // Keep session in Processing state so we can see the progress step
    server.use(
      http.get('/api/import-sessions/:id', () => {
        return HttpResponse.json(mockProcessingSession)
      }),
    )

    const user = userEvent.setup()
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    await waitFor(() => {
      expect(screen.getByText('Importing candidates...')).toBeInTheDocument()
    })
  })

  it('should transition to summary when poll returns Completed', async () => {
    server.use(
      http.get('/api/import-sessions/:id', () => {
        return HttpResponse.json(mockCompletedSession)
      }),
    )

    const user = userEvent.setup()
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    await waitFor(() => {
      expect(screen.getByText('Created')).toBeInTheDocument()
    })
  })

  it('should show error and return to upload on Failed', async () => {
    server.use(
      http.get('/api/import-sessions/:id', () => {
        return HttpResponse.json({
          ...mockCompletedSession,
          status: 'Failed',
          failureReason: 'Missing required column: Email',
        })
      }),
    )

    const user = userEvent.setup()
    render(
      <ImportWizard
        recruitmentId={recruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    const file = new File(['content'], 'workday.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })
    const input = screen.getByTestId('file-input')
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /start import/i }))

    await waitFor(() => {
      expect(
        screen.getByText('Missing required column: Email'),
      ).toBeInTheDocument()
    })
  })
})
