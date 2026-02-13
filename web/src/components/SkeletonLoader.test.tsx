import { render } from '@/test-utils'
import { describe, expect, it } from 'vitest'

import { SkeletonLoader } from './SkeletonLoader'

describe('SkeletonLoader', () => {
  it('should render card variant with expected structure', () => {
    const { container } = render(<SkeletonLoader variant="card" />)
    expect(
      container.querySelector('[data-testid="skeleton-card"]'),
    ).toBeInTheDocument()
  })

  it('should render list-row variant with expected structure', () => {
    const { container } = render(<SkeletonLoader variant="list-row" />)
    expect(
      container.querySelector('[data-testid="skeleton-list-row"]'),
    ).toBeInTheDocument()
  })

  it('should render text-block variant with expected structure', () => {
    const { container } = render(<SkeletonLoader variant="text-block" />)
    expect(
      container.querySelector('[data-testid="skeleton-text-block"]'),
    ).toBeInTheDocument()
  })

  it('should apply animate-pulse class', () => {
    const { container } = render(<SkeletonLoader variant="card" />)
    const skeleton = container.firstElementChild
    expect(skeleton?.className).toMatch(/animate-pulse/)
  })
})
