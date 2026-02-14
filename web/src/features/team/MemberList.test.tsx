import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '@/test-utils'
import { MemberList } from './MemberList'

const mockRecruitmentId = '550e8400-e29b-41d4-a716-446655440000'

describe('MemberList', () => {
  it('should render member names and roles', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Dev User A')).toBeInTheDocument()
    })
    expect(screen.getByText('Recruiting Leader')).toBeInTheDocument()
    expect(screen.getByText('Dev User B')).toBeInTheDocument()
    expect(screen.getByText('SME/Collaborator')).toBeInTheDocument()
  })

  it('should show Creator badge for the creator member', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Creator')).toBeInTheDocument()
    })
  })

  it('should not show remove button for creator', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Dev User A')).toBeInTheDocument()
    })

    // Creator should not have a remove button
    expect(screen.queryByLabelText('Remove Dev User A')).not.toBeInTheDocument()
  })

  it('should show remove button for non-creator members', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Dev User B')).toBeInTheDocument()
    })

    expect(screen.getByLabelText('Remove Dev User B')).toBeInTheDocument()
  })

  it('should show invite button', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /invite/i })).toBeInTheDocument()
    })
  })

  it('should not show invite button when disabled', async () => {
    render(<MemberList recruitmentId={mockRecruitmentId} disabled />)

    await waitFor(() => {
      expect(screen.getByText('Dev User A')).toBeInTheDocument()
    })

    expect(screen.queryByRole('button', { name: /invite/i })).not.toBeInTheDocument()
  })

  it('should show confirmation when clicking remove', async () => {
    const user = userEvent.setup()
    render(<MemberList recruitmentId={mockRecruitmentId} />)

    await waitFor(() => {
      expect(screen.getByText('Dev User B')).toBeInTheDocument()
    })

    await user.click(screen.getByLabelText('Remove Dev User B'))

    expect(screen.getByText(/Remove Dev User B from this recruitment/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /confirm/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
  })
})
