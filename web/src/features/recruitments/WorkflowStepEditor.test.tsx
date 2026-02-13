import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { DEFAULT_WORKFLOW_STEPS } from './workflowDefaults'
import { WorkflowStepEditor, type WorkflowStep } from './WorkflowStepEditor'

function createSteps(names: string[]): WorkflowStep[] {
  return names.map((name, i) => ({
    id: `step-${i}`,
    name,
    order: i + 1,
  }))
}

describe('WorkflowStepEditor', () => {
  it('should render all steps', () => {
    const steps = createSteps(['Screening', 'Interview'])
    render(<WorkflowStepEditor steps={steps} onChange={vi.fn()} />)

    expect(screen.getByDisplayValue('Screening')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Interview')).toBeInTheDocument()
  })

  it('should show empty message when no steps', () => {
    render(<WorkflowStepEditor steps={[]} onChange={vi.fn()} />)

    expect(screen.getByText(/no workflow steps defined/i)).toBeInTheDocument()
  })

  it('should call onChange when step name is edited', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    const steps = createSteps(['Screening'])

    render(<WorkflowStepEditor steps={steps} onChange={onChange} />)

    const input = screen.getByDisplayValue('Screening')
    await user.clear(input)
    await user.type(input, 'Phone Screen')

    expect(onChange).toHaveBeenCalled()
  })

  it('should call onChange when step is removed', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    const steps = createSteps(['Screening', 'Interview'])

    render(<WorkflowStepEditor steps={steps} onChange={onChange} />)

    await user.click(screen.getByLabelText('Remove Screening'))

    expect(onChange).toHaveBeenCalledWith([
      expect.objectContaining({ name: 'Interview', order: 1 }),
    ])
  })

  it('should call onChange when step is added', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    const steps = createSteps(['Screening'])

    render(<WorkflowStepEditor steps={steps} onChange={onChange} />)

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
    const steps = createSteps(['Screening'])
    render(
      <WorkflowStepEditor steps={steps} onChange={vi.fn()} disabled={true} />,
    )

    expect(screen.getByDisplayValue('Screening')).toBeDisabled()
    expect(screen.getByText('Add Step')).toBeDisabled()
  })
})
