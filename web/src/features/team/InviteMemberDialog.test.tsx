import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { InviteMemberDialog } from './InviteMemberDialog'
import { render } from '@/test-utils'

const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('InviteMemberDialog', () => {
  it('should render search input when open', () => {
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(screen.getByPlaceholderText(/search/i)).toBeInTheDocument()
  })

  it('should show search results after typing and debounce', async () => {
    const user = userEvent.setup()
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    await user.type(screen.getByPlaceholderText(/search/i), 'Erik')

    await waitFor(() => {
      expect(screen.getByText('Erik Leader')).toBeInTheDocument()
    }, { timeout: 2000 })

    expect(screen.getByText('erik@dev.local')).toBeInTheDocument()
  })

  it('should not search with less than 2 characters', async () => {
    const user = userEvent.setup()
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    await user.type(screen.getByPlaceholderText(/search/i), 'E')

    // Wait past the debounce period
    await new Promise(resolve => setTimeout(resolve, 500))
    expect(screen.queryByText('Erik Leader')).not.toBeInTheDocument()
  })

  it('should call API and close dialog when selecting a user', async () => {
    const user = userEvent.setup()
    const onOpenChange = vi.fn()
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={onOpenChange}
      />,
    )

    await user.type(screen.getByPlaceholderText(/search/i), 'Sara')

    await waitFor(() => {
      expect(screen.getByText('Sara Specialist')).toBeInTheDocument()
    }, { timeout: 2000 })

    await user.click(screen.getByText('Sara Specialist'))

    await waitFor(() => {
      expect(onOpenChange).toHaveBeenCalledWith(false)
    }, { timeout: 2000 })
  })

  it('should show dialog title', () => {
    render(
      <InviteMemberDialog
        recruitmentId={mockRecruitmentId}
        open={true}
        onOpenChange={() => {}}
      />,
    )

    expect(screen.getByText('Invite Team Member')).toBeInTheDocument()
  })
})
