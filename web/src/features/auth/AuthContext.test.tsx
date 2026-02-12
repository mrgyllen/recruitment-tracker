import { afterEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '../../test-utils'
import { AuthProvider, useAuth } from './AuthContext'

function TestConsumer() {
  const { isAuthenticated, user, signOut } = useAuth()
  return (
    <div>
      <span data-testid="is-auth">{String(isAuthenticated)}</span>
      <span data-testid="user-name">{user?.name ?? 'none'}</span>
      <button onClick={signOut}>Sign Out</button>
    </div>
  )
}

describe('AuthContext (dev mode)', () => {
  afterEach(() => {
    localStorage.clear()
    vi.unstubAllEnvs()
  })

  it('should provide auth state in dev mode', () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development')

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
      { wrapper: undefined },
    )

    expect(screen.getByTestId('is-auth')).toHaveTextContent('true')
    expect(screen.getByTestId('user-name')).toHaveTextContent('Alice Dev')
  })

  it('should provide user info from dev persona', () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development')
    localStorage.setItem(
      'dev-auth-user',
      JSON.stringify({ id: 'dev-user-b', name: 'Bob Dev' }),
    )

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
      { wrapper: undefined },
    )

    expect(screen.getByTestId('user-name')).toHaveTextContent('Bob Dev')
  })
})
