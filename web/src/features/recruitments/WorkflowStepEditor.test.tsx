import { render as rtlRender, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { DEFAULT_WORKFLOW_STEPS } from './workflowDefaults'
import { WorkflowStepEditor, type WorkflowStep } from './WorkflowStepEditor'
import { mockRecruitmentId } from '@/mocks/recruitmentHandlers'
import { render, waitFor } from '@/test-utils'

function createSteps(names: string[]): WorkflowStep[] {
  return names.map((name, i) => ({
    id: `step-${i}`,
    name,
    order: i + 1,
  }))
}

describe('WorkflowStepEditor (create mode)', () => {
  it('should render all steps', () => {
    rtlRender(<WorkflowStepEditor steps={createSteps(['Screening', 'Interview'])} onChange={vi.fn()} />)

    expect(screen.getByDisplayValue('Screening')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Interview')).toBeInTheDocument()
  })

  it('should show empty message when no steps', () => {
    rtlRender(<WorkflowStepEditor steps={[]} onChange={vi.fn()} />)

    expect(screen.getByText(/no workflow steps defined/i)).toBeInTheDocument()
  })

  it('should call onChange when step name is edited', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()

    rtlRender(<WorkflowStepEditor steps={createSteps(['Screening'])} onChange={onChange} />)

    const input = screen.getByDisplayValue('Screening')
    await user.clear(input)
    await user.type(input, 'Phone Screen')

    expect(onChange).toHaveBeenCalled()
  })

  it('should call onChange when step is removed', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()

    rtlRender(<WorkflowStepEditor steps={createSteps(['Screening', 'Interview'])} onChange={onChange} />)

    await user.click(screen.getByLabelText('Remove Screening'))

    expect(onChange).toHaveBeenCalledWith([
      expect.objectContaining({ name: 'Interview', order: 1 }),
    ])
  })

  it('should call onChange when step is added', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()

    rtlRender(<WorkflowStepEditor steps={createSteps(['Screening'])} onChange={onChange} />)

    await user.click(screen.getByText('Add Step'))

    expect(onChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({ name: 'Screening', order: 1 }),
        expect.objectContaining({ name: '', order: 2 }),
      ]),
    )
  })

  it('should have 7 default workflow steps', () => {
    expect(DEFAULT_WORKFLOW_STEPS).toHaveLength(7)
    expect(DEFAULT_WORKFLOW_STEPS.map((s) => s.name)).toEqual([
      'Screening',
      'Technical Test',
      'Technical Interview',
      'Leader Interview',
      'Personality Test',
      'Offer/Contract',
      'Negotiation',
    ])
  })

  it('should disable controls when disabled prop is true', () => {
    rtlRender(
      <WorkflowStepEditor steps={createSteps(['Screening'])} onChange={vi.fn()} disabled={true} />,
    )

    expect(screen.getByDisplayValue('Screening')).toBeDisabled()
    expect(screen.getByText('Add Step')).toBeDisabled()
  })
})

describe('WorkflowStepEditor (edit mode)', () => {
  const editSteps: WorkflowStep[] = [
    { id: 'step-1', name: 'Screening', order: 1 },
    { id: 'step-2', name: 'Technical Test', order: 2 },
    { id: 'step-3', name: 'Technical Interview', order: 3 },
  ]

  it('should render steps in edit mode', () => {
    render(
      <WorkflowStepEditor
        mode="edit"
        steps={editSteps}
        recruitmentId={mockRecruitmentId}
      />,
    )

    expect(screen.getByDisplayValue('Screening')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Technical Test')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Technical Interview')).toBeInTheDocument()
  })

  it('should show add step input and submit via API', async () => {
    const user = userEvent.setup()
    render(
      <WorkflowStepEditor
        mode="edit"
        steps={editSteps}
        recruitmentId={mockRecruitmentId}
      />,
    )

    await user.click(screen.getByText('Add Step'))

    const newStepInput = screen.getByPlaceholderText('New step name')
    await user.type(newStepInput, 'Offer')
    await user.click(screen.getByRole('button', { name: /confirm/i }))

    await waitFor(() => {
      expect(screen.queryByPlaceholderText('New step name')).not.toBeInTheDocument()
    })
  })

  it('should remove step via API when clicking remove', async () => {
    const user = userEvent.setup()
    render(
      <WorkflowStepEditor
        mode="edit"
        steps={editSteps}
        recruitmentId={mockRecruitmentId}
      />,
    )

    await user.click(screen.getByLabelText('Remove Screening'))

    // MSW returns 204 for normal step removal -- no error shown
    await waitFor(() => {
      expect(screen.queryByText(/cannot remove/i)).not.toBeInTheDocument()
    })
  })

  it('should show inline error when removing step with outcomes (409)', async () => {
    const user = userEvent.setup()
    const stepsWithOutcomes: WorkflowStep[] = [
      { id: 'step-has-outcomes', name: 'Has Outcomes', order: 1 },
    ]
    render(
      <WorkflowStepEditor
        mode="edit"
        steps={stepsWithOutcomes}
        recruitmentId={mockRecruitmentId}
      />,
    )

    await user.click(screen.getByLabelText('Remove Has Outcomes'))

    await waitFor(() => {
      expect(screen.getByText(/cannot remove/i)).toBeInTheDocument()
    })
  })

  it('should disable all controls when disabled prop is true', () => {
    render(
      <WorkflowStepEditor
        mode="edit"
        steps={editSteps}
        recruitmentId={mockRecruitmentId}
        disabled={true}
      />,
    )

    expect(screen.getByDisplayValue('Screening')).toBeDisabled()
    expect(screen.getByText('Add Step')).toBeDisabled()
  })
})
