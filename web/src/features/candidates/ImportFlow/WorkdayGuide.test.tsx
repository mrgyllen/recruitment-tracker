import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { WorkdayGuide } from './WorkdayGuide'
import { render, screen } from '@/test-utils'

describe('WorkdayGuide', () => {
  it('should render collapsed trigger', () => {
    render(<WorkdayGuide />)
    expect(screen.getByText('Workday export instructions')).toBeInTheDocument()
  })

  it('should expand to show instructions when clicked', async () => {
    const user = userEvent.setup()
    render(<WorkdayGuide />)

    await user.click(screen.getByText('Workday export instructions'))

    expect(screen.getByText(/navigate to your recruitment/)).toBeInTheDocument()
    expect(screen.getByText(/always export all candidates/i)).toBeInTheDocument()
  })
})
