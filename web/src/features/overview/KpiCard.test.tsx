import { describe, expect, it } from 'vitest'
import { KpiCard } from './KpiCard'
import { render, screen } from '@/test-utils'

describe('KpiCard', () => {
  it('should render label and value', () => {
    render(<KpiCard label="Total Candidates" value={130} />)

    expect(screen.getByText('130')).toBeInTheDocument()
    expect(screen.getByText('Total Candidates')).toBeInTheDocument()
  })

  it('should apply warning styles for stale count', () => {
    render(<KpiCard label="Stale" value={3} variant="warning" />)

    const valueElement = screen.getByText('3')
    expect(valueElement).toHaveClass('text-amber-600')
  })

  it('should have accessible aria-label', () => {
    render(<KpiCard label="Total Candidates" value={130} />)

    expect(
      screen.getByLabelText('Total Candidates: 130'),
    ).toBeInTheDocument()
  })
})
