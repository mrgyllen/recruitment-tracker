import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { ViewportGuard } from './ViewportGuard'

function mockMatchMedia(matches: boolean) {
  const listeners: Array<(e: MediaQueryListEvent) => void> = []
  const mql = {
    matches,
    media: '(min-width: 1280px)',
    addEventListener: vi.fn((_event: string, cb: (e: MediaQueryListEvent) => void) => {
      listeners.push(cb)
    }),
    removeEventListener: vi.fn(),
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }
  window.matchMedia = vi.fn().mockReturnValue(mql)
  return { mql, listeners }
}

describe('ViewportGuard', () => {
  const originalMatchMedia = window.matchMedia

  afterEach(() => {
    window.matchMedia = originalMatchMedia
  })

  it('renders children when viewport is 1280px or wider', () => {
    mockMatchMedia(true)

    render(
      <ViewportGuard>
        <p>App content</p>
      </ViewportGuard>,
    )

    expect(screen.getByText('App content')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('shows alert message when viewport is narrower than 1280px', () => {
    mockMatchMedia(false)

    render(
      <ViewportGuard>
        <p>App content</p>
      </ViewportGuard>,
    )

    expect(screen.queryByText('App content')).not.toBeInTheDocument()
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText(/designed for desktop browsers/)).toBeInTheDocument()
  })

  it('alert message has aria-live="assertive"', () => {
    mockMatchMedia(false)

    render(
      <ViewportGuard>
        <p>App content</p>
      </ViewportGuard>,
    )

    const alert = screen.getByRole('alert')
    expect(alert).toHaveAttribute('aria-live', 'assertive')
  })

  it('responds to viewport changes via matchMedia listener', () => {
    const { listeners } = mockMatchMedia(true)

    render(
      <ViewportGuard>
        <p>App content</p>
      </ViewportGuard>,
    )

    expect(screen.getByText('App content')).toBeInTheDocument()

    // Simulate viewport shrinking below 1280px
    act(() => {
      for (const listener of listeners) {
        listener({ matches: false } as MediaQueryListEvent)
      }
    })

    expect(screen.queryByText('App content')).not.toBeInTheDocument()
    expect(screen.getByRole('alert')).toBeInTheDocument()
  })
})
