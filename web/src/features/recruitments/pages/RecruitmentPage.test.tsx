import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http } from 'msw'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { RecruitmentPage } from './RecruitmentPage'
import { AuthProvider } from '@/features/auth/AuthContext'
import {
  forbiddenRecruitmentId,
  mockRecruitmentId,
  mockRecruitmentId2,
} from '@/mocks/recruitmentHandlers'
import { server } from '@/mocks/server'

function renderWithRoute(recruitmentId: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  const router = createMemoryRouter(
    [{ path: '/recruitments/:recruitmentId', element: <RecruitmentPage /> }],
    { initialEntries: [`/recruitments/${recruitmentId}`] },
  )
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  )
}

describe('RecruitmentPage', () => {
  it('renders recruitment title when loaded', async () => {
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(
        screen.getByRole('heading', { name: /senior \.net developer/i }),
      ).toBeInTheDocument()
    })
    expect(screen.getByText('Active')).toBeInTheDocument()
  })

  it('renders workflow steps', async () => {
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByDisplayValue('Screening')).toBeInTheDocument()
    })
    expect(screen.getByDisplayValue('Technical Test')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Technical Interview')).toBeInTheDocument()
  })

  it('shows access denied on 403 response', async () => {
    renderWithRoute(forbiddenRecruitmentId)

    await waitFor(() => {
      expect(
        screen.getByRole('heading', { name: /access denied/i }),
      ).toBeInTheDocument()
    })
    expect(
      screen.getByText(/don't have access to this recruitment/i),
    ).toBeInTheDocument()
  })

  it('shows not found on 404 response', async () => {
    renderWithRoute('00000000-0000-0000-0000-000000000000')

    await waitFor(() => {
      expect(
        screen.getByRole('heading', { name: /not found/i }),
      ).toBeInTheDocument()
    })
    expect(screen.getByText(/recruitment not found/i)).toBeInTheDocument()
  })

  it('shows skeleton while loading', () => {
    server.use(
      http.get('/api/recruitments/:id', () => {
        return new Promise(() => {})
      }),
    )

    renderWithRoute(mockRecruitmentId)

    expect(screen.getByTestId('skeleton-text-block')).toBeInTheDocument()
  })

  it('renders edit form with pre-populated values for active recruitment', async () => {
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByDisplayValue('Senior .NET Developer')).toBeInTheDocument()
    })
    expect(screen.getByLabelText(/title/i)).toBeEnabled()
  })

  it('renders edit form with disabled fields for closed recruitment', async () => {
    renderWithRoute(mockRecruitmentId2)

    await waitFor(() => {
      expect(screen.getByDisplayValue('Frontend Engineer')).toBeInTheDocument()
    })
    expect(screen.getByLabelText(/title/i)).toBeDisabled()
    expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument()
  })

  it('renders workflow step editor in edit mode', async () => {
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByText('Add Step')).toBeInTheDocument()
    })
    expect(screen.getByDisplayValue('Screening')).toBeInTheDocument()
  })

  it('renders close button for active recruitment', async () => {
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: /close recruitment/i }),
      ).toBeInTheDocument()
    })
  })

  it('does not render close button for closed recruitment', async () => {
    renderWithRoute(mockRecruitmentId2)

    await waitFor(() => {
      expect(screen.getByDisplayValue('Frontend Engineer')).toBeInTheDocument()
    })

    expect(
      screen.queryByRole('button', { name: /close recruitment/i }),
    ).not.toBeInTheDocument()
  })

  it('should filter candidate list when step is clicked in overview', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    // Wait for overview to load
    await waitFor(() => {
      expect(
        screen.getByLabelText('Total Candidates: 130'),
      ).toBeInTheDocument()
    })

    // Click step in overview
    await user.click(
      screen.getByRole('button', { name: /filter by step: screening/i }),
    )

    // External filter badge should appear
    await waitFor(() => {
      expect(screen.getByLabelText('Clear overview filter')).toBeInTheDocument()
    })
  })

  it('should filter to stale candidates when stale indicator is clicked', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(
        screen.getByLabelText('Total Candidates: 130'),
      ).toBeInTheDocument()
    })

    await user.click(
      screen.getByRole('button', {
        name: /show stale candidates at step: screening/i,
      }),
    )

    await waitFor(() => {
      expect(screen.getByText(/stale only/i)).toBeInTheDocument()
    })
  })

  it('should show active filter indicator with clear button', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(
        screen.getByLabelText('Total Candidates: 130'),
      ).toBeInTheDocument()
    })

    await user.click(
      screen.getByRole('button', { name: /filter by step: screening/i }),
    )

    await waitFor(() => {
      expect(
        screen.getByLabelText('Clear overview filter'),
      ).toBeInTheDocument()
    })
  })

  it('should clear filter and show all candidates when clear button is clicked', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(
        screen.getByLabelText('Total Candidates: 130'),
      ).toBeInTheDocument()
    })

    await user.click(
      screen.getByRole('button', { name: /filter by step: screening/i }),
    )

    await waitFor(() => {
      expect(
        screen.getByLabelText('Clear overview filter'),
      ).toBeInTheDocument()
    })

    await user.click(screen.getByLabelText('Clear overview filter'))

    await waitFor(() => {
      expect(
        screen.queryByLabelText('Clear overview filter'),
      ).not.toBeInTheDocument()
    })
  })
})
