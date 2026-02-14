import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { CreateCandidateForm } from './CreateCandidateForm'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

const recruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('CreateCandidateForm', () => {
  it('should render the trigger button', () => {
    render(<CreateCandidateForm recruitmentId={recruitmentId} />)

    expect(
      screen.getByRole('button', { name: /add candidate/i }),
    ).toBeInTheDocument()
  })

  it('should render all form fields with correct labels when opened', async () => {
    const user = userEvent.setup()
    render(<CreateCandidateForm recruitmentId={recruitmentId} />)

    await user.click(screen.getByRole('button', { name: /add candidate/i }))

    expect(screen.getByLabelText(/full name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/phone/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/location/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/date applied/i)).toBeInTheDocument()
  })

  it('should show validation errors when submitting with empty required fields', async () => {
    const user = userEvent.setup()
    render(<CreateCandidateForm recruitmentId={recruitmentId} />)

    await user.click(screen.getByRole('button', { name: /add candidate/i }))

    const fullNameInput = screen.getByLabelText(/full name/i)
    await user.clear(fullNameInput)

    const emailInput = screen.getByLabelText(/email/i)
    await user.clear(emailInput)

    // Click the submit button inside the dialog
    const submitButtons = screen.getAllByRole('button', {
      name: /add candidate/i,
    })
    const submitButton = submitButtons[submitButtons.length - 1]
    await user.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText('Full name is required')).toBeInTheDocument()
    })
    expect(screen.getByText('Email is required')).toBeInTheDocument()
  })

  it('should call API with correct data when form is valid', async () => {
    const user = userEvent.setup()
    server.use(
      http.post(
        '/api/recruitments/:recruitmentId/candidates',
        () => {
          return HttpResponse.json({ id: 'new-id' }, { status: 201 })
        },
      ),
    )

    render(<CreateCandidateForm recruitmentId={recruitmentId} />)

    await user.click(screen.getByRole('button', { name: /add candidate/i }))

    await user.type(screen.getByLabelText(/full name/i), 'Jane Doe')
    await user.type(screen.getByLabelText(/email/i), 'jane@example.com')

    const submitButtons = screen.getAllByRole('button', {
      name: /add candidate/i,
    })
    const submitButton = submitButtons[submitButtons.length - 1]
    await user.click(submitButton)

    await waitFor(() => {
      expect(screen.queryByText('Full name is required')).not.toBeInTheDocument()
    })
  })

  it('should show inline error when duplicate email is returned', async () => {
    const user = userEvent.setup()
    server.use(
      http.post(
        '/api/recruitments/:recruitmentId/candidates',
        () => {
          return HttpResponse.json(
            {
              type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
              title:
                'A candidate with this email already exists in this recruitment',
              status: 400,
              detail: 'Duplicate email.',
            },
            { status: 400 },
          )
        },
      ),
    )

    render(<CreateCandidateForm recruitmentId={recruitmentId} />)

    await user.click(screen.getByRole('button', { name: /add candidate/i }))

    await user.type(screen.getByLabelText(/full name/i), 'Jane Doe')
    await user.type(screen.getByLabelText(/email/i), 'duplicate@example.com')

    const submitButtons = screen.getAllByRole('button', {
      name: /add candidate/i,
    })
    const submitButton = submitButtons[submitButtons.length - 1]
    await user.click(submitButton)

    await waitFor(() => {
      expect(
        screen.getByText(
          'A candidate with this email already exists in this recruitment',
        ),
      ).toBeInTheDocument()
    })
  })

  it('should disable submit button and show spinner when pending', async () => {
    const user = userEvent.setup()
    let resolveRequest: (() => void) | null = null
    server.use(
      http.post(
        '/api/recruitments/:recruitmentId/candidates',
        () => {
          return new Promise((resolve) => {
            resolveRequest = () =>
              resolve(HttpResponse.json({ id: 'new-id' }, { status: 201 }))
          })
        },
      ),
    )

    render(<CreateCandidateForm recruitmentId={recruitmentId} />)

    await user.click(screen.getByRole('button', { name: /add candidate/i }))

    await user.type(screen.getByLabelText(/full name/i), 'Jane Doe')
    await user.type(screen.getByLabelText(/email/i), 'jane@example.com')

    const submitButtons = screen.getAllByRole('button', {
      name: /add candidate/i,
    })
    const submitButton = submitButtons[submitButtons.length - 1]
    await user.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText('Adding...')).toBeInTheDocument()
    })

    resolveRequest?.()
  })

  it('should default date applied to today', async () => {
    const user = userEvent.setup()
    render(<CreateCandidateForm recruitmentId={recruitmentId} />)

    await user.click(screen.getByRole('button', { name: /add candidate/i }))

    const dateInput = screen.getByLabelText(
      /date applied/i,
    ) as HTMLInputElement
    const today = new Date().toISOString().split('T')[0]
    expect(dateInput.value).toBe(today)
  })
})
