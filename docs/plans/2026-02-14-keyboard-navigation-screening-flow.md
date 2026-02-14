# Keyboard Navigation & Screening Flow Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable full keyboard-driven screening workflow with shortcuts (1/2/3 for outcomes), arrow key navigation, focus management after auto-advance, and ARIA live regions.

**Architecture:** Add `useKeyboardNavigation` hook scoped to panel refs (not document-level). Modify `OutcomeForm` to show shortcut hints and accept external outcome selection. Update `ScreeningLayout` to coordinate focus after auto-advance and add ARIA live regions. Modify `useScreeningSession` to accept an `onAutoAdvance` callback for focus coordination.

**Tech Stack:** React 19 hooks (useRef, useEffect, useCallback), Vitest + Testing Library, Tailwind CSS v4 (sr-only, focus-visible), ARIA live regions.

**Authorization:** N/A -- purely frontend story, no backend changes.

---

## Critical Codebase Facts (deviations from story artifact)

1. **OutcomeForm manages its own state internally** -- It uses `useState<OutcomeStatus | null>` for `selectedOutcome` (line 32). Since ScreeningLayout uses `key={selectedCandidate.id}` to remount on candidate switch, state resets are handled. The keyboard hook should call an `onOutcomeSelect` callback that OutcomeForm exposes, which sets its internal state.

2. **useScreeningSession has no onAutoAdvance callback** -- The auto-advance happens inside a `setTimeout` in `handleOutcomeRecorded` (line 98-104). To trigger focus-return after auto-advance, we'll add an optional `onAutoAdvance` callback parameter to the hook.

3. **CandidatePanel is not using forwardRef** -- It's a plain function component. We'll add a `candidateListRef` prop instead of using forwardRef, since the ref needs to go on the inner list container div, not the root.

4. **OutcomeForm test queries use `/pass/i`, `/fail/i`, `/hold/i`** -- After adding shortcut hints ("Pass (1)"), these regex patterns will still match. No test breakage expected.

5. **ScreeningLayout test mocks** -- PdfViewer is mocked (`vi.mock`), ResizeObserver uses class-based mock. All new ScreeningLayout tests must include these mocks.

6. **useAppToast returns `{ success, error, info }`** -- Only string arguments. The `toast` import from `sonner` is used for action buttons.

7. **Existing test pattern** -- `OutcomeForm.test.tsx` uses `@/test-utils` render. `ScreeningLayout.test.tsx` uses custom `renderWithRoute()` with MemoryRouter.

---

### Task 1: useKeyboardNavigation hook

**Files:**
- Create: `web/src/features/screening/hooks/useKeyboardNavigation.ts`
- Test: `web/src/features/screening/hooks/useKeyboardNavigation.test.tsx`

**Step 1: Write the test file**

```tsx
// web/src/features/screening/hooks/useKeyboardNavigation.test.tsx
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
```

**Step 2: Run test to verify it fails**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/hooks/useKeyboardNavigation.test.tsx`
Expected: FAIL -- module not found

**Step 3: Write the implementation**

```typescript
// web/src/features/screening/hooks/useKeyboardNavigation.ts
import { useEffect, useCallback } from 'react'
import type { OutcomeStatus } from '@/lib/api/screening.types'

const OUTCOME_KEYS: Record<string, OutcomeStatus> = {
  '1': 'Pass',
  '2': 'Fail',
  '3': 'Hold',
}

const TEXT_INPUT_TAGS = new Set(['INPUT', 'TEXTAREA', 'SELECT'])

interface UseKeyboardNavigationOptions {
  outcomePanelRef: React.RefObject<HTMLDivElement | null>
  candidateListRef: React.RefObject<HTMLDivElement | null>
  onOutcomeSelect: (outcome: OutcomeStatus) => void
  selectCandidate: (id: string) => void
  candidates: Array<{ id: string }>
  selectedCandidateId: string | null
  enabled?: boolean
}

interface UseKeyboardNavigationReturn {
  focusOutcomePanel: () => void
}

export function useKeyboardNavigation({
  outcomePanelRef,
  candidateListRef,
  onOutcomeSelect,
  selectCandidate,
  candidates,
  selectedCandidateId,
  enabled = true,
}: UseKeyboardNavigationOptions): UseKeyboardNavigationReturn {
  // Outcome panel keydown: 1/2/3 shortcuts
  useEffect(() => {
    if (!enabled) return
    const panel = outcomePanelRef.current
    if (!panel) return

    const handleKeyDown = (e: KeyboardEvent) => {
      const activeTag = (e.target as HTMLElement).tagName
      if (TEXT_INPUT_TAGS.has(activeTag)) return
      if ((e.target as HTMLElement).isContentEditable) return

      const outcome = OUTCOME_KEYS[e.key]
      if (outcome) {
        e.preventDefault()
        onOutcomeSelect(outcome)
      }
    }

    panel.addEventListener('keydown', handleKeyDown)
    return () => panel.removeEventListener('keydown', handleKeyDown)
  }, [outcomePanelRef, onOutcomeSelect, enabled])

  // Candidate list keydown: Arrow Up/Down
  useEffect(() => {
    if (!enabled) return
    const list = candidateListRef.current
    if (!list) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return
      e.preventDefault()

      const currentIndex = candidates.findIndex((c) => c.id === selectedCandidateId)
      if (currentIndex === -1) return

      if (e.key === 'ArrowDown' && currentIndex < candidates.length - 1) {
        selectCandidate(candidates[currentIndex + 1].id)
      } else if (e.key === 'ArrowUp' && currentIndex > 0) {
        selectCandidate(candidates[currentIndex - 1].id)
      }
    }

    list.addEventListener('keydown', handleKeyDown)
    return () => list.removeEventListener('keydown', handleKeyDown)
  }, [candidateListRef, selectCandidate, candidates, selectedCandidateId, enabled])

  const focusOutcomePanel = useCallback(() => {
    requestAnimationFrame(() => {
      outcomePanelRef.current?.focus()
    })
  }, [outcomePanelRef])

  return { focusOutcomePanel }
}
```

**Step 4: Run tests to verify they pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/hooks/useKeyboardNavigation.test.tsx`
Expected: All 12 tests PASS

**Step 5: Commit**

```bash
git add web/src/features/screening/hooks/useKeyboardNavigation.ts web/src/features/screening/hooks/useKeyboardNavigation.test.tsx
git commit -m "feat(4.5): add useKeyboardNavigation hook with shortcut scoping + tests"
```

---

### Task 2: Update OutcomeForm with shortcut hints and external outcome selection

**Files:**
- Modify: `web/src/features/screening/OutcomeForm.tsx`
- Modify: `web/src/features/screening/OutcomeForm.test.tsx`

**Step 1: Write the new tests (append to existing test file)**

Add these tests to the existing `describe('OutcomeForm', ...)` block:

```tsx
it('should display shortcut hints on outcome buttons', () => {
  render(<OutcomeForm {...defaultProps} />)
  expect(screen.getByRole('button', { name: /pass \(1\)/i })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: /fail \(2\)/i })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: /hold \(3\)/i })).toBeInTheDocument()
})

it('should select outcome via onOutcomeSelect prop', async () => {
  const onOutcomeSelect = vi.fn()
  render(<OutcomeForm {...defaultProps} onOutcomeSelect={onOutcomeSelect} />)
  // onOutcomeSelect is called when the internal state changes
  const user = userEvent.setup()
  await user.click(screen.getByRole('button', { name: /pass/i }))
  expect(onOutcomeSelect).toHaveBeenCalledWith('Pass')
})
```

**Step 2: Run tests to verify new tests fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/OutcomeForm.test.tsx`
Expected: 2 new tests FAIL (shortcut hints not yet shown, onOutcomeSelect prop doesn't exist)

**Step 3: Modify OutcomeForm.tsx**

Changes needed:
1. Add `onOutcomeSelect?: (outcome: OutcomeStatus) => void` to `OutcomeFormProps`
2. Change button labels from `"Pass"` to `"Pass (1)"`, `"Fail"` to `"Fail (2)"`, `"Hold"` to `"Hold (3)"`
3. Call `onOutcomeSelect?.(option.value)` in the onClick handler alongside `setSelectedOutcome`

Specifically modify the `outcomeOptions` array labels:
```typescript
const outcomeOptions: { value: OutcomeStatus; label: string; hint: string; className: string; selectedClassName: string }[] = [
  { value: 'Pass', label: 'Pass', hint: '1', className: 'border-green-300 text-green-700 hover:bg-green-50', selectedClassName: 'bg-green-600 text-white border-green-600' },
  { value: 'Fail', label: 'Fail', hint: '2', className: 'border-red-300 text-red-700 hover:bg-red-50', selectedClassName: 'bg-red-600 text-white border-red-600' },
  { value: 'Hold', label: 'Hold', hint: '3', className: 'border-amber-300 text-amber-700 hover:bg-amber-50', selectedClassName: 'bg-amber-500 text-white border-amber-500' },
]
```

And the button text:
```tsx
{option.label} <kbd className="ml-1 text-xs opacity-60">({option.hint})</kbd>
```

Add to OutcomeFormProps:
```typescript
onOutcomeSelect?: (outcome: OutcomeStatus) => void
```

In the onClick handler:
```typescript
onClick={() => {
  setSelectedOutcome(option.value)
  onOutcomeSelect?.(option.value)
}}
```

**Step 4: Run tests to verify all pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/OutcomeForm.test.tsx`
Expected: All 10 tests PASS (8 existing + 2 new)

**Step 5: Run full test suite to verify no regressions**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run`
Expected: All 267+ tests PASS

**Step 6: Commit**

```bash
git add web/src/features/screening/OutcomeForm.tsx web/src/features/screening/OutcomeForm.test.tsx
git commit -m "feat(4.5): add shortcut hints and onOutcomeSelect to OutcomeForm + tests"
```

---

### Task 3: Add onAutoAdvance callback to useScreeningSession

**Files:**
- Modify: `web/src/features/screening/hooks/useScreeningSession.ts`
- Modify: `web/src/features/screening/hooks/useScreeningSession.test.tsx`

**Step 1: Write the new test (append to existing test file)**

```tsx
it('should call onAutoAdvance callback when auto-advance fires', () => {
  const onAutoAdvance = vi.fn()
  const { result } = renderHook(
    () => useScreeningSession('r1', makeCandidates(), { onAutoAdvance }),
    { wrapper: createWrapper() },
  )

  act(() => result.current.selectCandidate('c1'))
  act(() => result.current.handleOutcomeRecorded(makeOutcomeResult('c1', 'Pass')))

  act(() => vi.advanceTimersByTime(300))
  expect(onAutoAdvance).toHaveBeenCalled()
})
```

Note: The existing test setup uses `makeCandidates()` helper (based on context summary). Check the exact helper name and signature in the test file before implementing.

**Step 2: Run test to verify it fails**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/hooks/useScreeningSession.test.tsx`
Expected: FAIL -- useScreeningSession doesn't accept options object

**Step 3: Modify useScreeningSession to accept options**

Add optional third parameter:
```typescript
interface UseScreeningSessionOptions {
  onAutoAdvance?: () => void
}

export function useScreeningSession(
  recruitmentId: string,
  candidates: CandidateResponse[],
  options?: UseScreeningSessionOptions,
) {
```

In the auto-advance setTimeout callback (line 98-104), after `setSelectedCandidateId(nextId)`, call:
```typescript
options?.onAutoAdvance?.()
```

**Step 4: Run tests to verify all pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/hooks/useScreeningSession.test.tsx`
Expected: All 12 tests PASS (11 existing + 1 new)

**Step 5: Commit**

```bash
git add web/src/features/screening/hooks/useScreeningSession.ts web/src/features/screening/hooks/useScreeningSession.test.tsx
git commit -m "feat(4.5): add onAutoAdvance callback to useScreeningSession"
```

---

### Task 4: Update ScreeningLayout with keyboard coordination, ARIA regions, and focus management

**Files:**
- Modify: `web/src/features/screening/ScreeningLayout.tsx`
- Modify: `web/src/features/screening/ScreeningLayout.test.tsx`

**Step 1: Write the new tests (append to existing test file)**

```tsx
it('should have ARIA region labels on all three panels', async () => {
  renderWithRoute()
  // Wait for data to load
  await screen.findAllByText('Select a candidate')

  expect(screen.getByRole('region', { name: /candidate list/i })).toBeInTheDocument()
  expect(screen.getByRole('region', { name: /cv viewer/i })).toBeInTheDocument()
  expect(screen.getByRole('region', { name: /outcome controls/i })).toBeInTheDocument()
})

it('should have ARIA live regions for announcements', async () => {
  renderWithRoute()
  await screen.findAllByText('Select a candidate')

  // Polite live region for candidate switches
  const politeRegion = document.querySelector('[aria-live="polite"]')
  expect(politeRegion).toBeInTheDocument()

  // Assertive live region for outcome announcements
  const assertiveRegion = document.querySelector('[aria-live="assertive"]')
  expect(assertiveRegion).toBeInTheDocument()
})
```

**Step 2: Run tests to verify new tests fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/ScreeningLayout.test.tsx`
Expected: 2 new tests FAIL

**Step 3: Modify ScreeningLayout.tsx**

Add imports:
```typescript
import { useRef, useState, useEffect } from 'react'
import { useKeyboardNavigation } from './hooks/useKeyboardNavigation'
import type { OutcomeStatus } from '@/lib/api/screening.types'
```

Add refs and state inside the component:
```typescript
const outcomePanelRef = useRef<HTMLDivElement>(null!)
const candidateListRef = useRef<HTMLDivElement>(null!)
const [candidateAnnouncement, setCandidateAnnouncement] = useState('')
const [outcomeAnnouncement, setOutcomeAnnouncement] = useState('')
```

Wire useKeyboardNavigation:
```typescript
const { focusOutcomePanel } = useKeyboardNavigation({
  outcomePanelRef,
  candidateListRef,
  onOutcomeSelect: (outcome: OutcomeStatus) => {
    // Find the outcome button and click it to set OutcomeForm's internal state
    const button = outcomePanelRef.current?.querySelector(
      `button[aria-pressed]`
    ) as HTMLButtonElement | null
    // Use a more targeted approach - dispatch on the correct button
    const buttons = outcomePanelRef.current?.querySelectorAll('button[aria-pressed]')
    const map: Record<OutcomeStatus, number> = { Pass: 0, Fail: 1, Hold: 2 }
    const target = buttons?.[map[outcome]] as HTMLButtonElement | undefined
    target?.click()
  },
  selectCandidate: session.selectCandidate,
  candidates,
  selectedCandidateId: session.selectedCandidateId,
  enabled: !!session.selectedCandidateId,
})
```

**IMPORTANT: Simpler approach for onOutcomeSelect:** Instead of programmatically clicking buttons, pass an `onOutcomeSelect` prop to OutcomeForm that sets its internal state. Since OutcomeForm already has `setSelectedOutcome` internally, we need OutcomeForm to expose a way to set it externally. **Better approach:** Use `useImperativeHandle` on OutcomeForm or, even simpler, use the `onOutcomeSelect` prop we added in Task 2 combined with a controlled pattern where the keyboard hook calls `onOutcomeSelect` which calls `setSelectedOutcome` in OutcomeForm.

**Actually, the cleanest approach:** In ScreeningLayout, maintain `keyboardSelectedOutcome` state. Pass it to OutcomeForm as a new `externalOutcome` prop. When OutcomeForm receives a new `externalOutcome`, it calls `setSelectedOutcome(externalOutcome)` via a `useEffect`. This keeps OutcomeForm in control of its own state while allowing external keyboard input.

**Revised approach for OutcomeForm (update in Task 2):** Add `externalOutcome?: OutcomeStatus | null` prop. Add `useEffect`:
```typescript
useEffect(() => {
  if (externalOutcome) {
    setSelectedOutcome(externalOutcome)
  }
}, [externalOutcome])
```

**For the plan, the ScreeningLayout keyboard integration becomes:**
```typescript
const [keyboardOutcome, setKeyboardOutcome] = useState<OutcomeStatus | null>(null)

const { focusOutcomePanel } = useKeyboardNavigation({
  outcomePanelRef,
  candidateListRef,
  onOutcomeSelect: setKeyboardOutcome,
  selectCandidate: session.selectCandidate,
  candidates,
  selectedCandidateId: session.selectedCandidateId,
  enabled: !!session.selectedCandidateId,
})
```

And in the OutcomeForm:
```tsx
<OutcomeForm
  key={selectedCandidate.id}
  externalOutcome={keyboardOutcome}
  ...
/>
```

Pass onAutoAdvance to useScreeningSession:
```typescript
const session = useScreeningSession(recruitmentId!, candidates, {
  onAutoAdvance: focusOutcomePanel,
})
```

Add ARIA live region announcement on candidate change:
```typescript
useEffect(() => {
  if (selectedCandidate) {
    setCandidateAnnouncement(
      `Now reviewing ${selectedCandidate.fullName} at ${selectedCandidate.currentWorkflowStepName ?? 'unknown step'}`
    )
  }
}, [selectedCandidate])
```

Wrap handleOutcomeRecorded to set outcome announcement:
```typescript
const handleOutcomeWithAnnouncement = useCallback(
  (result: OutcomeResultDto) => {
    const candidate = candidates.find((c) => c.id === result.candidateId)
    if (candidate) {
      setOutcomeAnnouncement(`${result.outcome} recorded for ${candidate.fullName}`)
    }
    setKeyboardOutcome(null)
    session.handleOutcomeRecorded(result)
  },
  [candidates, session.handleOutcomeRecorded],
)
```

Add ARIA attributes and live regions to JSX:
- Left panel: `role="region" aria-label="Candidate list"` + `ref={candidateListRef}` + `tabIndex={0}`
- Center panel: `role="region" aria-label="CV viewer"`
- Right panel: `role="region" aria-label="Outcome controls"` + `ref={outcomePanelRef}` + `tabIndex={0}`
- Add `focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2` to focusable panels
- Add two `sr-only` live region divs at end of component

**Step 4: Run tests to verify all pass**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run src/features/screening/ScreeningLayout.test.tsx`
Expected: All 5 tests PASS (3 existing + 2 new)

**Step 5: Run full test suite**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run`
Expected: All tests PASS

**Step 6: Commit**

```bash
git add web/src/features/screening/ScreeningLayout.tsx web/src/features/screening/ScreeningLayout.test.tsx
git commit -m "feat(4.5): add keyboard coordination, ARIA regions, and focus management to ScreeningLayout"
```

---

### Task 5: Update OutcomeForm with externalOutcome prop (Task 2 follow-up)

This task extends Task 2's changes to support the keyboard-driven outcome selection from ScreeningLayout.

**Files:**
- Modify: `web/src/features/screening/OutcomeForm.tsx`
- Modify: `web/src/features/screening/OutcomeForm.test.tsx`

**Step 1: Write the test**

```tsx
it('should apply externalOutcome prop to select outcome', () => {
  render(<OutcomeForm {...defaultProps} externalOutcome="Hold" />)
  expect(screen.getByRole('button', { name: /hold/i })).toHaveAttribute('aria-pressed', 'true')
  expect(screen.getByRole('button', { name: /confirm/i })).toBeEnabled()
})
```

**Step 2: Run test to verify it fails**

**Step 3: Add externalOutcome prop to OutcomeForm**

Add to props interface:
```typescript
externalOutcome?: OutcomeStatus | null
```

Add useEffect in OutcomeForm body:
```typescript
useEffect(() => {
  if (externalOutcome) {
    setSelectedOutcome(externalOutcome)
  }
}, [externalOutcome])
```

**Step 4: Run tests to verify all pass**

**Step 5: Commit**

```bash
git add web/src/features/screening/OutcomeForm.tsx web/src/features/screening/OutcomeForm.test.tsx
git commit -m "feat(4.5): add externalOutcome prop to OutcomeForm for keyboard integration"
```

---

### Task 6: Update CandidatePanel with ref forwarding and ARIA attributes

**Files:**
- Modify: `web/src/features/screening/CandidatePanel.tsx`
- Modify: `web/src/features/screening/CandidatePanel.test.tsx`

**Step 1: Write the test**

```tsx
it('should forward candidateListRef to container', () => {
  const ref = { current: null } as React.RefObject<HTMLDivElement | null>
  render(<CandidatePanel {...defaultProps} candidateListRef={ref} />)
  expect(ref.current).toBeInstanceOf(HTMLDivElement)
})
```

**Step 2: Modify CandidatePanel**

Add `candidateListRef?: React.RefObject<HTMLDivElement | null>` to props.

Apply ref to the root div:
```tsx
<div ref={candidateListRef} className="flex h-full flex-col">
```

**Step 3: Run tests**

**Step 4: Commit**

```bash
git add web/src/features/screening/CandidatePanel.tsx web/src/features/screening/CandidatePanel.test.tsx
git commit -m "feat(4.5): add candidateListRef prop to CandidatePanel"
```

---

### Task 7: Verification and story completion

**Step 1: Run full test suite**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vitest run`
Expected: All tests PASS

**Step 2: Run TypeScript type check**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx tsc --noEmit`
Expected: No errors

**Step 3: Run production build**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic4/web && npx vite build`
Expected: Exit 0

**Step 4: AC Coverage Map**

| AC | Status | Evidence |
|----|--------|----------|
| AC1: Keyboard shortcut outcome selection | Covered | useKeyboardNavigation tests: 1->Pass, 2->Fail, 3->Hold |
| AC2: Shortcut scoping for text inputs | Covered | useKeyboardNavigation tests: textarea/input filtering |
| AC3: Tab flow through outcome controls | Covered | Natural DOM order in OutcomeForm: buttons -> textarea -> confirm. tabIndex={0} on panel |
| AC4: Focus return after auto-advance | Covered | useScreeningSession onAutoAdvance -> focusOutcomePanel |
| AC5: Arrow key candidate navigation | Covered | useKeyboardNavigation tests: ArrowDown/ArrowUp |
| AC6: Focus stays on candidate list during arrow nav | Covered | keydown on candidateListRef, focus stays on container |
| AC7: Tab order and focus indicators | Covered | focus-visible:outline-* on panels, tabIndex={0} |
| AC8: Shortcut hints on buttons | Covered | OutcomeForm "Pass (1)" etc. + test |
| AC9: ARIA live regions | Covered | polite (candidate switch) + assertive (outcome recorded) |
| AC10: Full keyboard screening flow | Covered | Integration of AC1-AC9 |

**Step 5: Update sprint-status.yaml**

Change `4-5-keyboard-navigation-screening-flow: ready-for-dev` to `done`.

**Step 6: Update story artifact**

Update status to `done`, add Dev Agent Record with completion notes and file list.

**Step 7: Commit and notify team lead**

```bash
git add _bmad-output/implementation-artifacts/sprint-status.yaml _bmad-output/implementation-artifacts/4-5-keyboard-navigation-screening-flow.md
git commit -m "stories(4.5): mark keyboard navigation & screening flow as done"
```
