import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppHeader } from './AppHeader'
import { useAuth } from '@/features/auth/AuthContext'

vi.mock('@/features/auth/AuthContext', () => ({
  useAuth: vi.fn(),
}))

const mockUseAuth = vi.mocked(useAuth)

function renderAppHeader() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AppHeader />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('AppHeader', () => {
  const mockSignOut = vi.fn()

  beforeEach(() => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      login: vi.fn(),
      signOut: mockSignOut,
    })
    mockSignOut.mockClear()
  })

  it('renders app name "Recruitment Tracker"', () => {
    renderAppHeader()

    expect(screen.getByText('Recruitment Tracker')).toBeInTheDocument()
  })

  it('renders user display name', () => {
    renderAppHeader()

    expect(screen.getByText('Alice Dev')).toBeInTheDocument()
  })

  it('renders "Sign out" button', () => {
    renderAppHeader()

    expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
  })

  it('calls signOut when sign out button is clicked', async () => {
    const user = userEvent.setup()
    renderAppHeader()

    await user.click(screen.getByRole('button', { name: /sign out/i }))

    expect(mockSignOut).toHaveBeenCalledOnce()
  })

  it('uses semantic <header> element', () => {
    renderAppHeader()

    expect(screen.getByRole('banner')).toBeInTheDocument()
  })

  it('does not render user name when user is null', () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      login: vi.fn(),
      signOut: mockSignOut,
    })

    renderAppHeader()

    expect(screen.getByText('Recruitment Tracker')).toBeInTheDocument()
    expect(screen.queryByText('Alice Dev')).not.toBeInTheDocument()
  })
})
