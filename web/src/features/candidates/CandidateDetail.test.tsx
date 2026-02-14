import { http, HttpResponse } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import { CandidateDetail } from './CandidateDetail'
import { mockCandidateDetail, mockCandidateId1 } from '@/mocks/fixtures/candidates'
import { server } from '@/mocks/server'
import { render, screen, waitFor } from '@testing-library/react'
import { AuthProvider } from '@/features/auth/AuthContext'
import { Toaster } from '@/components/ui/sonner'

const recruitmentId = '550e8400-e29b-41d4-a716-446655440000'

function renderWithRoute(candidateId: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter
          initialEntries={[
            `/recruitments/${recruitmentId}/candidates/${candidateId}`,
          ]}
        >
          <Routes>
            <Route
              path="/recruitments/:recruitmentId/candidates/:candidateId"
              element={<CandidateDetail />}
            />
          </Routes>
        </MemoryRouter>
        <Toaster position="bottom-right" visibleToasts={1} />
      </AuthProvider>
    </QueryClientProvider>,
  )
}

describe('CandidateDetail', () => {
  it('should display all candidate profile fields', async () => {
    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument()
    })

    expect(screen.getByText('alice@example.com')).toBeInTheDocument()
    expect(screen.getByText('+1-555-0101')).toBeInTheDocument()
    expect(screen.getByText('New York, NY')).toBeInTheDocument()
  })

  it('should display current step with status', async () => {
    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(screen.getByText('Interview')).toBeInTheDocument()
    })
  })

  it('should display outcome history with step name and status', async () => {
    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(screen.getByText('Outcome History')).toBeInTheDocument()
    })

    expect(screen.getByText('Screening')).toBeInTheDocument()
  })

  it('should display documents with download action', async () => {
    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(screen.getByText('Documents')).toBeInTheDocument()
    })

    expect(screen.getByText('CV')).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: /download/i }),
    ).toBeInTheDocument()
  })

  it('should show empty outcome history when no outcomes recorded', async () => {
    server.use(
      http.get(
        '/api/recruitments/:recruitmentId/candidates/:candidateId',
        () => {
          return HttpResponse.json({
            ...mockCandidateDetail,
            outcomeHistory: [],
          })
        },
      ),
    )

    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(
        screen.getByText('No outcomes recorded yet.'),
      ).toBeInTheDocument()
    })
  })

  it('should show loading skeleton while data is fetching', () => {
    server.use(
      http.get(
        '/api/recruitments/:recruitmentId/candidates/:candidateId',
        () => {
          return new Promise(() => {
            // Never resolves -- simulates loading
          })
        },
      ),
    )

    renderWithRoute(mockCandidateId1)

    expect(screen.getByTestId('skeleton-card')).toBeInTheDocument()
  })
})
