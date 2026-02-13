import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { ProtectedRoute } from './ProtectedRoute'

vi.mock('./AuthContext', () => ({
  useAuth: vi.fn(),
}))

import { useAuth } from './AuthContext'

const mockUseAuth = vi.mocked(useAuth)

function renderProtectedRoute(initialEntry = '/') {
  const router = createMemoryRouter(
    [
      {
        element: <ProtectedRoute />,
        children: [{ path: '/', element: <p>Protected content</p> }],
      },
    ],
    { initialEntries: [initialEntry] },
  )

  return render(<RouterProvider router={router} />)
}

describe('ProtectedRoute', () => {
  it('renders child route when user is authenticated', () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      login: vi.fn(),
      signOut: vi.fn(),
    })

    renderProtectedRoute()

    expect(screen.getByText('Protected content')).toBeInTheDocument()
  })

  it('calls login() and shows redirect message when unauthenticated', () => {
    const mockLogin = vi.fn()
    mockUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      login: mockLogin,
      signOut: vi.fn(),
    })

    renderProtectedRoute()

    expect(screen.queryByText('Protected content')).not.toBeInTheDocument()
    expect(screen.getByText('Redirecting to login...')).toBeInTheDocument()
    expect(mockLogin).toHaveBeenCalledOnce()
  })
})
