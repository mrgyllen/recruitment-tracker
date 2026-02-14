import { render, screen, fireEvent } from '@testing-library/react'
import { useRef, useState } from 'react'
import { useKeyboardNavigation } from './useKeyboardNavigation'
import type { OutcomeStatus } from '@/lib/api/screening.types'

function TestHarness({
  candidates = [{ id: 'c1' }, { id: 'c2' }, { id: 'c3' }],
  initialSelected = 'c2',
  enabled = true,
}: {
  candidates?: Array<{ id: string }>
  initialSelected?: string | null
  enabled?: boolean
}) {
  const outcomePanelRef = useRef<HTMLDivElement>(null!)
  const candidateListRef = useRef<HTMLDivElement>(null!)
  const [selected, setSelected] = useState<string | null>(initialSelected)
  const [outcome, setOutcome] = useState<OutcomeStatus | null>(null)

  const { focusOutcomePanel } = useKeyboardNavigation({
    outcomePanelRef,
    candidateListRef,
    onOutcomeSelect: setOutcome,
    selectCandidate: setSelected,
    candidates,
    selectedCandidateId: selected,
    enabled,
  })

  return (
    <div>
      <div ref={candidateListRef} tabIndex={0} data-testid="candidate-list">
        Candidate List (selected: {selected})
      </div>
      <div ref={outcomePanelRef} tabIndex={0} data-testid="outcome-panel">
        Outcome Panel
        <textarea data-testid="reason-textarea" />
        <input data-testid="search-input" />
      </div>
      <div data-testid="outcome-value">{outcome}</div>
      <div data-testid="selected-value">{selected}</div>
      <button data-testid="focus-btn" onClick={() => focusOutcomePanel()}>
        Focus
      </button>
    </div>
  )
}

describe('useKeyboardNavigation', () => {
  it('should select Pass when 1 is pressed on outcome panel', () => {
    render(<TestHarness />)
    const panel = screen.getByTestId('outcome-panel')
    panel.focus()
    fireEvent.keyDown(panel, { key: '1' })
    expect(screen.getByTestId('outcome-value')).toHaveTextContent('Pass')
  })

  it('should select Fail when 2 is pressed on outcome panel', () => {
    render(<TestHarness />)
    const panel = screen.getByTestId('outcome-panel')
    panel.focus()
    fireEvent.keyDown(panel, { key: '2' })
    expect(screen.getByTestId('outcome-value')).toHaveTextContent('Fail')
  })

  it('should select Hold when 3 is pressed on outcome panel', () => {
    render(<TestHarness />)
    const panel = screen.getByTestId('outcome-panel')
    panel.focus()
    fireEvent.keyDown(panel, { key: '3' })
    expect(screen.getByTestId('outcome-value')).toHaveTextContent('Hold')
  })

  it('should NOT trigger shortcut when typing in textarea', () => {
    render(<TestHarness />)
    const textarea = screen.getByTestId('reason-textarea')
    textarea.focus()
    fireEvent.keyDown(textarea, { key: '1' })
    expect(screen.getByTestId('outcome-value')).toHaveTextContent('')
  })

  it('should NOT trigger shortcut when typing in input', () => {
    render(<TestHarness />)
    const input = screen.getByTestId('search-input')
    input.focus()
    fireEvent.keyDown(input, { key: '2' })
    expect(screen.getByTestId('outcome-value')).toHaveTextContent('')
  })

  it('should navigate to next candidate on Arrow Down', () => {
    render(<TestHarness initialSelected="c1" />)
    const list = screen.getByTestId('candidate-list')
    list.focus()
    fireEvent.keyDown(list, { key: 'ArrowDown' })
    expect(screen.getByTestId('selected-value')).toHaveTextContent('c2')
  })

  it('should navigate to previous candidate on Arrow Up', () => {
    render(<TestHarness initialSelected="c2" />)
    const list = screen.getByTestId('candidate-list')
    list.focus()
    fireEvent.keyDown(list, { key: 'ArrowUp' })
    expect(screen.getByTestId('selected-value')).toHaveTextContent('c1')
  })

  it('should not navigate past first candidate on Arrow Up', () => {
    render(<TestHarness initialSelected="c1" />)
    const list = screen.getByTestId('candidate-list')
    list.focus()
    fireEvent.keyDown(list, { key: 'ArrowUp' })
    expect(screen.getByTestId('selected-value')).toHaveTextContent('c1')
  })

  it('should not navigate past last candidate on Arrow Down', () => {
    render(<TestHarness initialSelected="c3" />)
    const list = screen.getByTestId('candidate-list')
    list.focus()
    fireEvent.keyDown(list, { key: 'ArrowDown' })
    expect(screen.getByTestId('selected-value')).toHaveTextContent('c3')
  })

  it('should prevent default scroll on Arrow keys in candidate list', () => {
    render(<TestHarness initialSelected="c1" />)
    const list = screen.getByTestId('candidate-list')
    list.focus()
    const event = new KeyboardEvent('keydown', {
      key: 'ArrowDown',
      bubbles: true,
      cancelable: true,
    })
    const prevented = !list.dispatchEvent(event)
    expect(prevented).toBe(true)
  })

  it('should focus outcome panel when focusOutcomePanel is called', async () => {
    render(<TestHarness />)
    screen.getByTestId('focus-btn').click()
    await vi.waitFor(() => {
      expect(document.activeElement).toBe(screen.getByTestId('outcome-panel'))
    })
  })

  it('should not register listeners when enabled is false', () => {
    render(<TestHarness enabled={false} />)
    const panel = screen.getByTestId('outcome-panel')
    panel.focus()
    fireEvent.keyDown(panel, { key: '1' })
    expect(screen.getByTestId('outcome-value')).toHaveTextContent('')
  })
})
