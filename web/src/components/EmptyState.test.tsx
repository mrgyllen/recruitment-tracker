import { render as rtlRender } from '@testing-library/react'
import { render, screen } from '@/test-utils'
import userEvent from '@testing-library/user-event'
import { axe } from 'vitest-axe'
import { describe, expect, it, vi } from 'vitest'

import { EmptyState } from './EmptyState'

describe('EmptyState', () => {
  it('should render heading at h2 level by default', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" />)
    expect(
      screen.getByRole('heading', { level: 2, name: 'No items' }),
    ).toBeInTheDocument()
  })

  it('should render heading at h3 level when specified', () => {
    render(
      <EmptyState
        heading="No items"
        description="Nothing here yet"
        headingLevel="h3"
      />,
    )
    expect(
      screen.getByRole('heading', { level: 3, name: 'No items' }),
    ).toBeInTheDocument()
  })

  it('should render description text', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" />)
    expect(screen.getByText('Nothing here yet')).toBeInTheDocument()
  })

  it('should render CTA button when actionLabel provided', () => {
    render(
      <EmptyState
        heading="No items"
        description="Nothing here yet"
        actionLabel="Create"
        onAction={() => {}}
      />,
    )
    expect(screen.getByRole('button', { name: 'Create' })).toBeInTheDocument()
  })

  it('should not render CTA button when actionLabel omitted', () => {
    render(<EmptyState heading="No items" description="Nothing here yet" />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('should call onAction when CTA clicked', async () => {
    const user = userEvent.setup()
    const onAction = vi.fn()
    render(
      <EmptyState
        heading="No items"
        description="Nothing here yet"
        actionLabel="Create"
        onAction={onAction}
      />,
    )
    await user.click(screen.getByRole('button', { name: 'Create' }))
    expect(onAction).toHaveBeenCalledOnce()
  })

  it('should render icon when provided', () => {
    render(
      <EmptyState
        heading="No items"
        description="Nothing here yet"
        icon={<svg data-testid="custom-icon" />}
      />,
    )
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument()
  })

  it('should have no axe violations', async () => {
    const { container } = rtlRender(
      <EmptyState
        heading="No items"
        description="Nothing here yet"
        actionLabel="Create"
        onAction={() => {}}
      />,
    )
    expect(await axe(container)).toHaveNoViolations()
  })
})
