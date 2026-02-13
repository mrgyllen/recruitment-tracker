import { render as rtlRender, screen } from '@testing-library/react'
import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { ErrorBoundary } from './ErrorBoundary'

function ThrowingComponent({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) throw new Error('Test error')
  return <div>Child content</div>
}

describe('ErrorBoundary', () => {
  const originalError = console.error
  beforeEach(() => {
    console.error = vi.fn()
  })
  afterEach(() => {
    console.error = originalError
  })

  it('should render children when no error occurs', () => {
    rtlRender(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={false} />
      </ErrorBoundary>,
    )
    expect(screen.getByText('Child content')).toBeInTheDocument()
  })

  it('should show default fallback when child throws', () => {
    rtlRender(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>,
    )
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
    expect(screen.getByText('Try refreshing the page')).toBeInTheDocument()
  })

  it('should show custom fallback when provided', () => {
    rtlRender(
      <ErrorBoundary fallback={<div>Custom error UI</div>}>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>,
    )
    expect(screen.getByText('Custom error UI')).toBeInTheDocument()
  })

  it('should show reload button in default fallback', () => {
    rtlRender(
      <ErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </ErrorBoundary>,
    )
    expect(screen.getByRole('button', { name: 'Reload' })).toBeInTheDocument()
  })
})
