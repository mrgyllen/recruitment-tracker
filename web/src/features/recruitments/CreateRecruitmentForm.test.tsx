import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { CreateRecruitmentForm } from './CreateRecruitmentForm'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@/test-utils'

describe('CreateRecruitmentForm', () => {
  it('should render the form with title field', () => {
    render(<CreateRecruitmentForm />)

    expect(
      screen.getByRole('heading', { name: /create recruitment/i }),
    ).toBeInTheDocument()
    expect(screen.getByLabelText(/title/i)).toBeInTheDocument()
  })

  it('should render default workflow steps', () => {
    render(<CreateRecruitmentForm />)

    expect(screen.getByDisplayValue('Screening')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Technical Test')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Negotiation')).toBeInTheDocument()
  })

  it('should show validation error when title is empty', async () => {
    const user = userEvent.setup()
    render(<CreateRecruitmentForm />)

    await user.click(
      screen.getByRole('button', { name: /create recruitment/i }),
    )

    await waitFor(() => {
      expect(screen.getByText('Title is required')).toBeInTheDocument()
    })
  })

  it('should submit form with valid data', async () => {
    const user = userEvent.setup()
    server.use(
      http.post('/api/recruitments', () => {
        return HttpResponse.json(
          { id: 'new-recruitment-id' },
          { status: 201 },
        )
      }),
    )

    render(<CreateRecruitmentForm />)

    await user.type(screen.getByLabelText(/title/i), 'Senior .NET Developer')
    await user.click(
      screen.getByRole('button', { name: /create recruitment/i }),
    )

    await waitFor(() => {
      expect(screen.queryByText('Title is required')).not.toBeInTheDocument()
    })
  })

  it('should render description and job requisition fields', () => {
    render(<CreateRecruitmentForm />)

    expect(screen.getByLabelText(/description/i)).toBeInTheDocument()
    expect(
      screen.getByLabelText(/job requisition reference/i),
    ).toBeInTheDocument()
  })

  it('should render cancel button', () => {
    render(<CreateRecruitmentForm />)

    expect(screen.getByText('Cancel')).toBeInTheDocument()
  })
})
