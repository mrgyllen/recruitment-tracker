import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { RecruitmentSelector } from './RecruitmentSelector'
import { mockRecruitmentId } from '@/mocks/recruitmentHandlers'
import { server } from '@/mocks/server'

function renderWithRoute(recruitmentId: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  const router = createMemoryRouter(
    [
      {
        path: '/recruitments/:recruitmentId',
        element: <RecruitmentSelector />,
      },
    ],
    { initialEntries: [`/recruitments/${recruitmentId}`] },
  )
  return render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

describe('RecruitmentSelector', () => {
  it('renders current recruitment name in breadcrumb', async () => {
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByText('Senior .NET Developer')).toBeInTheDocument()
    })
  })

  it('shows dropdown with all recruitments when clicked', async () => {
    const user = userEvent.setup()
    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByText('Senior .NET Developer')).toBeInTheDocument()
    })

    // The trigger should have a chevron indicating dropdown
    const trigger = screen.getByRole('button', {
      name: /senior \.net developer/i,
    })
    await user.click(trigger)

    await waitFor(() => {
      expect(screen.getByRole('menuitem', { name: /senior \.net developer/i })).toBeInTheDocument()
      expect(screen.getByRole('menuitem', { name: /frontend engineer/i })).toBeInTheDocument()
    })
  })

  it('renders plain text when only one recruitment', async () => {
    server.use(
      http.get('/api/recruitments', () => {
        return HttpResponse.json({
          items: [
            {
              id: mockRecruitmentId,
              title: 'Only Recruitment',
              description: null,
              status: 'Active',
              createdAt: new Date().toISOString(),
              closedAt: null,
              stepCount: 1,
              memberCount: 1,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    renderWithRoute(mockRecruitmentId)

    await waitFor(() => {
      expect(screen.getByText('Only Recruitment')).toBeInTheDocument()
    })

    // Should not be a button (no dropdown)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })
})
