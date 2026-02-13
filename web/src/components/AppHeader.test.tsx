import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { AppHeader } from './AppHeader'

vi.mock('@/features/auth/AuthContext', () => ({
  useAuth: vi.fn(),
}))

import { useAuth } from '@/features/auth/AuthContext'

const mockUseAuth = vi.mocked(useAuth)

describe('AppHeader', () => {
  const mockSignOut = vi.fn()

  beforeEach(() => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'dev-user-a', name: 'Alice Dev' },
      signOut: mockSignOut,
    })
    mockSignOut.mockClear()
  })

  it('renders app name "Recruitment Tracker"', () => {
    render(<AppHeader />)

    expect(screen.getByText('Recruitment Tracker')).toBeInTheDocument()
  })

  it('renders user display name', () => {
    render(<AppHeader />)

    expect(screen.getByText('Alice Dev')).toBeInTheDocument()
  })

  it('renders "Sign out" button', () => {
    render(<AppHeader />)

    expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
  })

  it('calls signOut when sign out button is clicked', async () => {
    const user = userEvent.setup()
    render(<AppHeader />)

    await user.click(screen.getByRole('button', { name: /sign out/i }))

    expect(mockSignOut).toHaveBeenCalledOnce()
  })

  it('uses semantic <header> element', () => {
    render(<AppHeader />)

    expect(screen.getByRole('banner')).toBeInTheDocument()
  })

  it('does not render user name when user is null', () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      signOut: mockSignOut,
    })

    render(<AppHeader />)

    expect(screen.getByText('Recruitment Tracker')).toBeInTheDocument()
    expect(screen.queryByText('Alice Dev')).not.toBeInTheDocument()
  })
})
