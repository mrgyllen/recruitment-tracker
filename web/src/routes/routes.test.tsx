import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { routeConfig } from './index'
import { useAuth } from '@/features/auth/AuthContext'
import { server } from '@/mocks/server'

vi.mock('react-pdf', () => ({
  Document: ({ children }: any) => <div>{children}</div>,
  Page: () => <div />,
  pdfjs: { GlobalWorkerOptions: { workerSrc: '' } },
}))

vi.mock('@/features/auth/AuthContext', () => ({
  useAuth: vi.fn(),
}))

const mockUseAuth = vi.mocked(useAuth)

function mockMatchMedia(matches: boolean) {
  const mql = {
    matches,
    media: '(min-width: 1280px)',
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }
  window.matchMedia = vi.fn().mockReturnValue(mql)
}

function renderRoute(path: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  const router = createMemoryRouter(routeConfig, {
    initialEntries: [path],
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

describe('Route definitions', () => {
  const originalMatchMedia = window.matchMedia

  afterEach(() => {
    window.matchMedia = originalMatchMedia
  })

  it('renders HomePage at / for authenticated user', async () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      login: vi.fn(),
      signOut: vi.fn(),
    })

    server.use(
      http.get('/api/recruitments', () => {
        return HttpResponse.json({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    renderRoute('/')

    await waitFor(() => {
      expect(
        screen.getByRole('heading', {
          name: /create your first recruitment/i,
        }),
      ).toBeInTheDocument()
    })
  })

  it('renders app header with user name at /', async () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      login: vi.fn(),
      signOut: vi.fn(),
    })

    server.use(
      http.get('/api/recruitments', () => {
        return HttpResponse.json({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    renderRoute('/')

    await waitFor(() => {
      expect(screen.getByText('Recruitment Tracker')).toBeInTheDocument()
    })
    expect(screen.getByText('Alice Dev')).toBeInTheDocument()
  })

  it('renders skip-to-content link', async () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      login: vi.fn(),
      signOut: vi.fn(),
    })

    server.use(
      http.get('/api/recruitments', () => {
        return HttpResponse.json({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    renderRoute('/')

    const skipLink = screen.getByText('Skip to main content')
    expect(skipLink).toBeInTheDocument()
    expect(skipLink).toHaveAttribute('href', '#main-content')
  })

  it('calls login and shows redirect message for unauthenticated user', () => {
    mockMatchMedia(true)
    const mockLogin = vi.fn()
    mockUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      login: mockLogin,
      signOut: vi.fn(),
    })

    renderRoute('/')

    expect(
      screen.queryByRole('heading', { name: /create your first recruitment/i }),
    ).not.toBeInTheDocument()
    expect(screen.getByText('Redirecting to login...')).toBeInTheDocument()
    expect(mockLogin).toHaveBeenCalledOnce()
  })

  it('renders main content area with correct id', async () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      login: vi.fn(),
      signOut: vi.fn(),
    })

    server.use(
      http.get('/api/recruitments', () => {
        return HttpResponse.json({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        })
      }),
    )

    renderRoute('/')

    await waitFor(() => {
      expect(screen.getByRole('main')).toHaveAttribute('id', 'main-content')
    })
  })
})
