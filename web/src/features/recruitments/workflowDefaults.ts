import type { WorkflowStep } from './WorkflowStepEditor'

let nextId = 1
function generateStepId(): string {
  return `step-${Date.now()}-${nextId++}`
}

export const DEFAULT_WORKFLOW_STEPS: WorkflowStep[] = [
  { id: generateStepId(), name: 'Screening', order: 1 },
  { id: generateStepId(), name: 'Technical Test', order: 2 },
  { id: generateStepId(), name: 'Technical Interview', order: 3 },
  { id: generateStepId(), name: 'Leader Interview', order: 4 },
  { id: generateStepId(), name: 'Personality Test', order: 5 },
  { id: generateStepId(), name: 'Offer/Contract', order: 6 },
  { id: generateStepId(), name: 'Negotiation', order: 7 },
]
