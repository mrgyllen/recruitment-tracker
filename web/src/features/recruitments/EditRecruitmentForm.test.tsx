import { describe, expect, it } from 'vitest'
import { EditRecruitmentForm } from './EditRecruitmentForm'
import type { RecruitmentDetail } from '@/lib/api/recruitments.types'
import { render, screen, waitFor } from '@/test-utils'
import userEvent from '@testing-library/user-event'

const activeRecruitment: RecruitmentDetail = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  title: 'Senior .NET Developer',
  description: 'Backend role',
  jobRequisitionId: 'REQ-001',
  status: 'Active',
  createdAt: new Date().toISOString(),
  closedAt: null,
  createdByUserId: 'user-1',
  steps: [],
  members: [],
}

const closedRecruitment: RecruitmentDetail = {
  ...activeRecruitment,
  status: 'Closed',
  closedAt: new Date().toISOString(),
}

describe('EditRecruitmentForm', () => {
  it('should render form with existing values', () => {
    render(<EditRecruitmentForm recruitment={activeRecruitment} />)

    expect(screen.getByDisplayValue('Senior .NET Developer')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Backend role')).toBeInTheDocument()
    expect(screen.getByDisplayValue('REQ-001')).toBeInTheDocument()
  })

  it('should disable all fields when recruitment is closed', () => {
    render(<EditRecruitmentForm recruitment={closedRecruitment} />)

    expect(screen.getByLabelText(/title/i)).toBeDisabled()
    expect(screen.getByLabelText(/description/i)).toBeDisabled()
    expect(screen.getByLabelText(/job requisition/i)).toBeDisabled()
  })

  it('should show validation error for empty title on submit', async () => {
    const user = userEvent.setup()
    render(<EditRecruitmentForm recruitment={activeRecruitment} />)

    const titleInput = screen.getByLabelText(/title/i)
    await user.clear(titleInput)
    await user.click(screen.getByRole('button', { name: /save/i }))

    await waitFor(() => {
      expect(screen.getByText(/title is required/i)).toBeInTheDocument()
    })
  })

  it('should not show save button when recruitment is closed', () => {
    render(<EditRecruitmentForm recruitment={closedRecruitment} />)

    expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument()
  })
})
