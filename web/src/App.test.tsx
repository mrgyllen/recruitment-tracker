import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'

import App from './App'

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

describe('App', () => {
  const originalMatchMedia = window.matchMedia

  afterEach(() => {
    window.matchMedia = originalMatchMedia
  })

  it('renders the app shell with header', () => {
    mockMatchMedia(true)
    render(<App />)

    expect(screen.getByText('Recruitment Tracker')).toBeInTheDocument()
  })
})
