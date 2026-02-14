import { render, screen } from '@/test-utils'
import { describe, expect, it } from 'vitest'
import { PendingActionsPanel } from './PendingActionsPanel'

describe('PendingActionsPanel', () => {
  it('should render pending action count', () => {
    render(<PendingActionsPanel count={47} />)

    expect(screen.getByText('47')).toBeInTheDocument()
    expect(screen.getByText('Pending Actions')).toBeInTheDocument()
    expect(screen.getByLabelText('Pending Actions: 47')).toBeInTheDocument()
  })
})
