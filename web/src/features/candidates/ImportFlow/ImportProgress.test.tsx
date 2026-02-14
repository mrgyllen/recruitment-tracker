import { describe, expect, it } from 'vitest'
import { ImportProgress } from './ImportProgress'
import { render, screen } from '@/test-utils'

describe('ImportProgress', () => {
  it('should render progress indicator with descriptive text', () => {
    render(<ImportProgress sourceFileName="workday-export.xlsx" />)
    expect(screen.getByText('Importing candidates...')).toBeInTheDocument()
    expect(screen.getByText(/workday-export.xlsx/)).toBeInTheDocument()
  })

  it('should render progress bar', () => {
    render(<ImportProgress sourceFileName="test.xlsx" />)
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })
})
