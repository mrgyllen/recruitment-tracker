import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { HomePage } from './HomePage'

vi.mock('sonner', async (importOriginal) => {
  const actual = await importOriginal<typeof import('sonner')>()
  return {
    ...actual,
    toast: {
      ...actual.toast,
      info: vi.fn(actual.toast.info),
    },
  }
})

import { toast } from 'sonner'

describe('HomePage', () => {
  it('renders heading "Create your first recruitment"', () => {
    render(<HomePage />)

    expect(
      screen.getByRole('heading', { name: /create your first recruitment/i }),
    ).toBeInTheDocument()
  })

  it('renders value proposition description', () => {
    render(<HomePage />)

    expect(
      screen.getByText(
        /track candidates from screening to offer/i,
      ),
    ).toBeInTheDocument()
  })

  it('renders "Create Recruitment" CTA button', () => {
    render(<HomePage />)

    expect(
      screen.getByRole('button', { name: /create recruitment/i }),
    ).toBeInTheDocument()
  })

  it('shows toast when CTA is clicked', async () => {
    const user = userEvent.setup()
    render(<HomePage />)

    await user.click(
      screen.getByRole('button', { name: /create recruitment/i }),
    )

    expect(toast.info).toHaveBeenCalledWith('Coming in Epic 2', {
      duration: 5000,
    })
  })
})
