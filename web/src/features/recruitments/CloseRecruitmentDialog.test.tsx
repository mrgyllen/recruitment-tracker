import { describe, expect, it, vi } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '@/test-utils'
import { CloseRecruitmentDialog } from './CloseRecruitmentDialog'

const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('CloseRecruitmentDialog', () => {
  it('should render explanation text when open', () => {
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(screen.getByText(/lock the recruitment/i)).toBeInTheDocument()
    expect(screen.getByText(/retention period/i)).toBeInTheDocument()
  })

  it('should render close recruitment button', () => {
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(
      screen.getByRole('button', { name: /close recruitment/i }),
    ).toBeInTheDocument()
  })

  it('should render cancel button', () => {
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
  })

  it('should call API and close dialog on confirm', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={onOpenChange}
      />,
    )

    await user.click(screen.getByRole('button', { name: /close recruitment/i }))

    await waitFor(
      () => {
        expect(onOpenChange).toHaveBeenCalledWith(false)
      },
      { timeout: 2000 },
    )
  })

  it('should call onOpenChange(false) when cancel is clicked', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    render(
      <CloseRecruitmentDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={onOpenChange}
      />,
    )

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    expect(onOpenChange).toHaveBeenCalledWith(false)
  })
})
