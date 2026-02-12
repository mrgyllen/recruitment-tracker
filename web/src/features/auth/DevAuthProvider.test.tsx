import { afterEach, describe, expect, it } from 'vitest'
import { render, screen } from '../../test-utils'
import userEvent from '@testing-library/user-event'
import { DevAuthProvider, useDevAuth } from './DevAuthProvider'

function TestConsumer() {
  const { currentUser } = useDevAuth()
  return <div data-testid="current-user">{currentUser?.name ?? 'none'}</div>
}

describe('DevAuthProvider', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('should render dev toolbar with persona options', () => {
    render(
      <DevAuthProvider>
        <div>App</div>
      </DevAuthProvider>,
      { wrapper: undefined },
    )

    expect(screen.getByText('DEV MODE')).toBeInTheDocument()
    expect(screen.getByRole('combobox')).toBeInTheDocument()
  })

  it('should default to User A persona', () => {
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
      { wrapper: undefined },
    )

    expect(screen.getByTestId('current-user')).toHaveTextContent('Alice Dev')
  })

  it('should switch personas when selecting from dropdown', async () => {
    const user = userEvent.setup()
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
      { wrapper: undefined },
    )

    await user.selectOptions(screen.getByRole('combobox'), 'dev-user-b')

    expect(screen.getByTestId('current-user')).toHaveTextContent('Bob Dev')
  })

  it('should persist selected persona to localStorage', async () => {
    const user = userEvent.setup()
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
      { wrapper: undefined },
    )

    await user.selectOptions(screen.getByRole('combobox'), 'dev-admin')

    const stored = JSON.parse(localStorage.getItem('dev-auth-user') ?? 'null')
    expect(stored).toEqual({ id: 'dev-admin', name: 'Admin Dev' })
  })

  it('should clear identity when selecting Unauthenticated', async () => {
    const user = userEvent.setup()
    render(
      <DevAuthProvider>
        <TestConsumer />
      </DevAuthProvider>,
      { wrapper: undefined },
    )

    await user.selectOptions(screen.getByRole('combobox'), 'unauthenticated')

    expect(screen.getByTestId('current-user')).toHaveTextContent('none')
    expect(localStorage.getItem('dev-auth-user')).toBeNull()
  })
})
