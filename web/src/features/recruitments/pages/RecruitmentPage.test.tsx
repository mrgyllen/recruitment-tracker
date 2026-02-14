import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { http } from 'msw'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { RecruitmentPage } from './RecruitmentPage'
import {
  forbiddenRecruitmentId,
  mockRecruitmentId,
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
      <RouterProvider router={router} />
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
      expect(screen.getByText('Screening')).toBeInTheDocument()
    })
    expect(screen.getByText('Technical Test')).toBeInTheDocument()
    expect(screen.getByText('Technical Interview')).toBeInTheDocument()
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
})
