import { render as rtlRender } from '@testing-library/react'
import { render, screen } from '@/test-utils'
import userEvent from '@testing-library/user-event'
import { axe } from 'vitest-axe'
import { describe, expect, it, vi } from 'vitest'

import { ActionButton } from './ActionButton'

describe('ActionButton', () => {
  it('should render primary variant', () => {
    render(<ActionButton variant="primary">Create</ActionButton>)
    expect(screen.getByRole('button', { name: 'Create' })).toBeInTheDocument()
  })

  it('should render secondary variant', () => {
    render(<ActionButton variant="secondary">Cancel</ActionButton>)
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
  })

  it('should render destructive variant', () => {
    render(<ActionButton variant="destructive">Remove</ActionButton>)
    expect(screen.getByRole('button', { name: 'Remove' })).toBeInTheDocument()
  })

  it('should disable button and show loading text in loading state', () => {
    render(
      <ActionButton variant="primary" loading loadingText="Creating...">
        Create
      </ActionButton>,
    )
    const button = screen.getByRole('button')
    expect(button).toBeDisabled()
    expect(button).toHaveTextContent('Creating...')
  })

  it('should show spinner svg in loading state', () => {
    render(
      <ActionButton variant="primary" loading loadingText="Creating...">
        Create
      </ActionButton>,
    )
    const button = screen.getByRole('button')
    expect(button.querySelector('svg')).toBeInTheDocument()
  })

  it('should call onClick when clicked', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(
      <ActionButton variant="primary" onClick={onClick}>
        Create
      </ActionButton>,
    )
    await user.click(screen.getByRole('button'))
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('should not call onClick when loading', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(
      <ActionButton
        variant="primary"
        loading
        loadingText="Creating..."
        onClick={onClick}
      >
        Create
      </ActionButton>,
    )
    await user.click(screen.getByRole('button'))
    expect(onClick).not.toHaveBeenCalled()
  })

  // Accessibility
  it('should have no axe violations for primary variant', async () => {
    const { container } = rtlRender(
      <ActionButton variant="primary">Create</ActionButton>,
    )
    expect(await axe(container)).toHaveNoViolations()
  })

  it('should have no axe violations for secondary variant', async () => {
    const { container } = rtlRender(
      <ActionButton variant="secondary">Cancel</ActionButton>,
    )
    expect(await axe(container)).toHaveNoViolations()
  })

  it('should have no axe violations for destructive variant', async () => {
    const { container } = rtlRender(
      <ActionButton variant="destructive">Remove</ActionButton>,
    )
    expect(await axe(container)).toHaveNoViolations()
  })
})
