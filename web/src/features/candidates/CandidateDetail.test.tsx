import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { MemoryRouter, Route, Routes } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { CandidateDetail } from './CandidateDetail'
import { Toaster } from '@/components/ui/sonner'
import { AuthProvider } from '@/features/auth/AuthContext'
import {
  mockCandidateDetail,
  mockCandidateId1,
} from '@/mocks/fixtures/candidates'
import { server } from '@/mocks/server'

// Mock react-pdf since jsdom cannot render canvas
vi.mock('react-pdf', () => {
  const Document = ({ onLoadSuccess, children, file }: { onLoadSuccess?: (info: { numPages: number }) => void; children: React.ReactNode; file?: string }) => {
    if (file) {
      setTimeout(() => onLoadSuccess?.({ numPages: 1 }), 0)
    }
    return (
      <div data-testid="pdf-document">
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

const recruitmentId = '550e8400-e29b-41d4-a716-446655440000'

function renderWithRoute(candidateId: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
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

  it('should render the PDF viewer when candidate has a document', async () => {
    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(screen.getByTestId('pdf-document')).toBeInTheDocument()
    })
  })

  it('should show download button with SAS URL', async () => {
    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(screen.getByText('CV')).toBeInTheDocument()
    })

    const downloadLink = screen.getByRole('link', { name: /download/i })
    expect(downloadLink).toBeInTheDocument()
    expect(downloadLink).toHaveAttribute(
      'href',
      mockCandidateDetail.documents[0].sasUrl,
    )
  })

  it('should show empty state when candidate has no document', async () => {
    server.use(
      http.get(
        '/api/recruitments/:recruitmentId/candidates/:candidateId',
        () => {
          return HttpResponse.json({
            ...mockCandidateDetail,
            documents: [],
          })
        },
      ),
    )

    renderWithRoute(mockCandidateId1)

    await waitFor(() => {
      expect(screen.getByText('No CV available')).toBeInTheDocument()
    })
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
