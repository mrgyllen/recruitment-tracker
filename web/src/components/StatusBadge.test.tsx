import { render as rtlRender } from '@testing-library/react'
import { render, screen } from '@/test-utils'
import { axe } from 'vitest-axe'
import { describe, expect, it } from 'vitest'

import { StatusBadge } from './StatusBadge'
import type { StatusVariant } from './StatusBadge.types'

describe('StatusBadge', () => {
  const variants: { status: StatusVariant; label: string; hasIcon: boolean }[] =
    [
      { status: 'pass', label: 'Pass outcome', hasIcon: true },
      { status: 'fail', label: 'Fail outcome', hasIcon: true },
      { status: 'hold', label: 'Hold outcome', hasIcon: true },
      { status: 'stale', label: 'Stale outcome', hasIcon: true },
      { status: 'not-started', label: 'Not Started outcome', hasIcon: false },
    ]

  variants.forEach(({ status, label }) => {
    it(`should render ${status} variant with correct aria-label`, () => {
      render(<StatusBadge status={status} />)
      expect(screen.getByLabelText(label)).toBeInTheDocument()
    })
  })

  it('should render checkmark icon for pass variant', () => {
    render(<StatusBadge status="pass" />)
    const badge = screen.getByLabelText('Pass outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should render X icon for fail variant', () => {
    render(<StatusBadge status="fail" />)
    const badge = screen.getByLabelText('Fail outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should render pause icon for hold variant', () => {
    render(<StatusBadge status="hold" />)
    const badge = screen.getByLabelText('Hold outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should render clock icon for stale variant', () => {
    render(<StatusBadge status="stale" />)
    const badge = screen.getByLabelText('Stale outcome')
    expect(badge.querySelector('svg')).toBeInTheDocument()
  })

  it('should not render icon for not-started variant', () => {
    render(<StatusBadge status="not-started" />)
    const badge = screen.getByLabelText('Not Started outcome')
    expect(badge.querySelector('svg')).not.toBeInTheDocument()
  })

  it('should use outlined style for stale variant (distinct from hold)', () => {
    render(<StatusBadge status="stale" />)
    const badge = screen.getByLabelText('Stale outcome')
    expect(badge.className).toMatch(/border/)
  })

  it('should allow custom aria-label', () => {
    render(<StatusBadge status="pass" aria-label="Custom label" />)
    expect(screen.getByLabelText('Custom label')).toBeInTheDocument()
  })

  // Accessibility — render without provider wrapper to isolate axe to StatusBadge only
  variants.forEach(({ status }) => {
    it(`should have no axe violations for ${status} variant`, async () => {
      const { container } = rtlRender(<StatusBadge status={status} />)
      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })
})
