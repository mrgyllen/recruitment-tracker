import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from '@/features/auth/AuthContext'
import { Toaster } from '@/components/ui/sonner'
import { ScreeningLayout } from './ScreeningLayout'

// Mock PdfViewer to avoid pdfjs-dist DOMMatrix dependency in jsdom
vi.mock('@/features/candidates/PdfViewer', () => ({
  PdfViewer: ({ url }: { url: string | null }) => (
    url ? <div data-testid="pdf-viewer">PDF: {url}</div> : null
  ),
}))

class MockResizeObserver {
  observe = vi.fn()
  disconnect = vi.fn()
  unobserve = vi.fn()
}

beforeEach(() => {
  vi.stubGlobal('ResizeObserver', MockResizeObserver)
})

afterEach(() => {
  vi.restoreAllMocks()
})

function renderWithRoute() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter
          initialEntries={[
            '/recruitments/550e8400-e29b-41d4-a716-446655440000/screening',
          ]}
        >
          <Routes>
            <Route
              path="/recruitments/:recruitmentId/screening"
              element={<ScreeningLayout />}
            />
          </Routes>
        </MemoryRouter>
        <Toaster position="bottom-right" visibleToasts={1} />
      </AuthProvider>
    </QueryClientProvider>,
  )
}

describe('ScreeningLayout', () => {
  it('should show skeleton loader while data is loading', () => {
    renderWithRoute()
    expect(screen.getByTestId('skeleton-card')).toBeInTheDocument()
  })

  it('should show empty states before any candidate is selected', async () => {
    renderWithRoute()
    const headings = await screen.findAllByText('Select a candidate')
    expect(headings.length).toBeGreaterThanOrEqual(1)
  })

  it('should render resizable divider with separator role', async () => {
    renderWithRoute()
    const separator = await screen.findByRole('separator')
    expect(separator).toBeInTheDocument()
    expect(separator).toHaveAttribute('aria-orientation', 'vertical')
  })
})
