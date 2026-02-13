import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { routeConfig } from './index'

vi.mock('@/features/auth/AuthContext', () => ({
  useAuth: vi.fn(),
}))

import { useAuth } from '@/features/auth/AuthContext'

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
  const router = createMemoryRouter(routeConfig, {
    initialEntries: [path],
  })
  return render(<RouterProvider router={router} />)
}

describe('Route definitions', () => {
  const originalMatchMedia = window.matchMedia

  afterEach(() => {
    window.matchMedia = originalMatchMedia
  })

  it('renders HomePage at / for authenticated user', () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      signOut: vi.fn(),
    })

    renderRoute('/')

    expect(
      screen.getByRole('heading', { name: /create your first recruitment/i }),
    ).toBeInTheDocument()
  })

  it('renders app header with user name at /', () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      signOut: vi.fn(),
    })

    renderRoute('/')

    expect(screen.getByText('Recruitment Tracker')).toBeInTheDocument()
    expect(screen.getByText('Alice Dev')).toBeInTheDocument()
  })

  it('renders skip-to-content link', () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      signOut: vi.fn(),
    })

    renderRoute('/')

    const skipLink = screen.getByText('Skip to main content')
    expect(skipLink).toBeInTheDocument()
    expect(skipLink).toHaveAttribute('href', '#main-content')
  })

  it('redirects unauthenticated user away from /', () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      signOut: vi.fn(),
    })

    renderRoute('/')

    expect(
      screen.queryByRole('heading', { name: /create your first recruitment/i }),
    ).not.toBeInTheDocument()
  })

  it('renders main content area with correct id', () => {
    mockMatchMedia(true)
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      signOut: vi.fn(),
    })

    renderRoute('/')

    expect(screen.getByRole('main')).toHaveAttribute('id', 'main-content')
  })
})
