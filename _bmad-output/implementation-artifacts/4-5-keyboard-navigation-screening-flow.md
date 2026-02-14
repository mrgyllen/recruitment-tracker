# Story 4.5: Keyboard Navigation & Screening Flow

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **user (Lina)**,
I want to **perform the entire screening workflow using keyboard navigation alone, with shortcuts for outcome recording and predictable focus management**,
so that **I can screen candidates at maximum speed without reaching for the mouse**.

## Acceptance Criteria

### AC1: Keyboard shortcut outcome selection
**Given** the screening layout is active with a candidate selected
**When** focus is on the outcome panel (not in a text input)
**Then** pressing `1` selects Pass, pressing `2` selects Fail, pressing `3` selects Hold
**And** the corresponding outcome button is visually highlighted

### AC2: Shortcut scoping for text inputs
**Given** the user presses `1`, `2`, or `3` while typing in the reason textarea
**When** the keypress is handled
**Then** the character is typed into the textarea (normal text input behavior)
**And** the shortcut does NOT trigger outcome selection
**And** shortcuts are scoped to the outcome panel via keydown listener filtered by active element

### AC3: Tab flow through outcome controls
**Given** an outcome is selected via keyboard shortcut
**When** the user presses `Tab`
**Then** focus moves to the reason textarea
**And** pressing `Tab` again moves focus to the confirm button
**And** pressing `Enter` on the confirm button records the outcome

### AC4: Focus return after auto-advance
**Given** an outcome is confirmed
**When** the auto-advance completes and the next candidate loads
**Then** focus returns to the outcome panel (not the reason textarea, not the candidate list)
**And** `1`/`2`/`3` shortcuts are immediately active for the next candidate

### AC5: Arrow key candidate navigation
**Given** the candidate list panel has focus
**When** the user presses Arrow Up or Arrow Down
**Then** the selection moves to the previous or next candidate in the list
**And** the CV viewer and outcome controls update accordingly

### AC6: Focus stays on candidate list during arrow navigation
**Given** the user navigates the candidate list with Arrow keys
**When** a new candidate is selected
**Then** focus remains on the candidate list (not stolen by the CV viewer or outcome panel)
**And** the user can press `Tab` to move focus to the outcome panel when ready to record

### AC7: Tab order and focus indicators
**Given** all interactive elements in the screening layout
**When** the user navigates via keyboard
**Then** all elements are reachable via Tab navigation in a logical order: candidate list -> CV viewer -> outcome controls
**And** focus indicators (blue 2px outline, 2px offset) are visible on all focused elements

### AC8: Shortcut hints on buttons
**Given** the outcome buttons are rendered
**When** the user inspects the UI
**Then** each button displays the keyboard shortcut hint: "Pass (1)", "Fail (2)", "Hold (3)"

### AC9: ARIA live regions for dynamic updates
**Given** the screening layout is active
**When** any dynamic content updates (candidate switch, outcome recorded, auto-advance)
**Then** assistive technologies are notified via ARIA live regions
**And** the outcome panel has appropriate `role` and `aria-label` attributes

### AC10: Full keyboard screening flow
**Given** the user completes the screening flow using only keyboard
**When** they screen multiple candidates in sequence
**Then** the flow is: select outcome (1/2/3) -> optional Tab to reason -> Tab to confirm -> Enter -> auto-advance -> repeat
**And** the entire flow operates without mouse interaction

### Prerequisites
- **Story 4.4** (Split-Panel Screening Layout) -- `ScreeningLayout.tsx` (composition root), `CandidatePanel.tsx`, `useScreeningSession.ts` hook (selectCandidate, handleOutcomeRecorded, auto-advance), `useResizablePanel.ts` hook (divider already has `role="separator"`, `aria-orientation`, `aria-label`)
- **Story 4.3** (Outcome Recording & Workflow Enforcement) -- `OutcomeForm.tsx` with Pass/Fail/Hold buttons + reason textarea + confirm button, `onOutcomeRecorded` callback
- **Story 4.1** (Candidate List & Search/Filter) -- `CandidateList.tsx` with virtualized list (react-virtuoso), `useCandidates` hook
- **Story 4.2** (PDF Viewing & Download) -- `PdfViewer.tsx` with text layer for screen reader accessibility
- **Story 1.4** (Shared UI) -- `EmptyState`, `SkeletonLoader`, `StatusBadge`, `useAppToast`

### FRs Fulfilled
- **FR44:** Users can perform the entire screening workflow using keyboard shortcuts only

### NFRs Addressed
- **NFR22:** WCAG 2.1 AA compliance
- **NFR23:** All interactive elements keyboard-reachable
- **NFR24:** Batch screening fully operable via keyboard
- **NFR28:** ARIA live regions for dynamic content
- **NFR29:** Predictable focus management during sequential workflows

## Tasks / Subtasks

- [ ] Task 1: Frontend -- useKeyboardNavigation hook (AC: #1, #2, #3, #4, #5, #6, #10)
  - [ ] 1.1 Create `web/src/features/screening/hooks/useKeyboardNavigation.ts`
  - [ ] 1.2 Hook accepts `{ outcomePanelRef, candidateListRef, selectCandidate, handleOutcome, candidates, selectedCandidateId }`
  - [ ] 1.3 Register `keydown` listener on the `outcomePanelRef` element (not document-level) for `1`/`2`/`3` shortcuts
  - [ ] 1.4 Filter shortcut activation: only fire when `activeElement` is NOT an `input`, `textarea`, or element with `[contenteditable]`. Check `e.target` tag name to prevent shortcut activation when typing in the reason textarea
  - [ ] 1.5 When `1`/`2`/`3` pressed (and not in text input), call `onOutcomeSelect(outcomeMap[key])` where outcomeMap = `{ '1': 'Pass', '2': 'Fail', '3': 'Hold' }`
  - [ ] 1.6 Register `keydown` listener on the `candidateListRef` element for Arrow Up / Arrow Down navigation
  - [ ] 1.7 Arrow Up: find previous candidate in candidates array from selectedCandidateId, call `selectCandidate(prevId)`. Do nothing if at first candidate.
  - [ ] 1.8 Arrow Down: find next candidate in candidates array from selectedCandidateId, call `selectCandidate(nextId)`. Do nothing if at last candidate.
  - [ ] 1.9 Prevent default scroll behavior on Arrow Up/Down when candidate list has focus (call `e.preventDefault()`)
  - [ ] 1.10 Expose `focusOutcomePanel()` method that sets focus on the outcome panel container element -- called by ScreeningLayout after auto-advance completes
  - [ ] 1.11 Cleanup: remove event listeners on unmount via useEffect return
  - [ ] 1.12 Test: "should select Pass when 1 is pressed on outcome panel"
  - [ ] 1.13 Test: "should select Fail when 2 is pressed on outcome panel"
  - [ ] 1.14 Test: "should select Hold when 3 is pressed on outcome panel"
  - [ ] 1.15 Test: "should NOT trigger shortcut when typing in textarea"
  - [ ] 1.16 Test: "should NOT trigger shortcut when typing in input"
  - [ ] 1.17 Test: "should navigate to next candidate on Arrow Down"
  - [ ] 1.18 Test: "should navigate to previous candidate on Arrow Up"
  - [ ] 1.19 Test: "should not navigate past first candidate on Arrow Up"
  - [ ] 1.20 Test: "should not navigate past last candidate on Arrow Down"
  - [ ] 1.21 Test: "should prevent default scroll on Arrow keys in candidate list"
  - [ ] 1.22 Test: "should focus outcome panel when focusOutcomePanel is called"

- [ ] Task 2: Frontend -- Update OutcomeForm to support keyboard shortcut integration (AC: #1, #3, #8)
  - [ ] 2.1 Modify `web/src/features/screening/OutcomeForm.tsx` to accept new prop: `selectedOutcome?: OutcomeStatus` (controlled externally by keyboard shortcut)
  - [ ] 2.2 Add `onOutcomeSelect` callback prop for when user clicks an outcome button (so ScreeningLayout can track selected state)
  - [ ] 2.3 Update button labels to show shortcut hints: "Pass (1)", "Fail (2)", "Hold (3)"
  - [ ] 2.4 Add visual highlight on the selected outcome button (e.g., `ring-2 ring-blue-500` for selected)
  - [ ] 2.5 Ensure Tab order within outcome panel: outcome buttons -> reason textarea -> confirm button. Use `tabIndex` only if natural DOM order doesn't match
  - [ ] 2.6 Add `ref` forwarding so ScreeningLayout can pass the outcome panel ref for focus management
  - [ ] 2.7 Test: "should display shortcut hints on outcome buttons"
  - [ ] 2.8 Test: "should highlight selected outcome button"
  - [ ] 2.9 Test: "should support controlled selectedOutcome prop"

- [ ] Task 3: Frontend -- Update ScreeningLayout for focus management and keyboard coordination (AC: #4, #7, #10)
  - [ ] 3.1 Modify `web/src/features/screening/ScreeningLayout.tsx` to call `useKeyboardNavigation` hook
  - [ ] 3.2 Create refs for outcome panel container (`outcomePanelRef`) and candidate list container (`candidateListRef`)
  - [ ] 3.3 Add `tabIndex={0}` to the outcome panel container div and candidate list container div so they can receive focus
  - [ ] 3.4 Set `tabIndex` order: candidate list container = 0, CV viewer area = 0, outcome panel container = 0 (logical tab flow left -> center -> right)
  - [ ] 3.5 After auto-advance completes (useScreeningSession's handleOutcomeRecorded triggers selectCandidate), call `focusOutcomePanel()` with a short delay (requestAnimationFrame or setTimeout 0) to ensure React has re-rendered the new candidate's outcome form
  - [ ] 3.6 Add keyboard state: track `selectedOutcome` via `useState`, pass to OutcomeForm and useKeyboardNavigation
  - [ ] 3.7 Reset `selectedOutcome` to null when selected candidate changes
  - [ ] 3.8 Focus indicator styles: add `focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2` to focusable panel containers
  - [ ] 3.9 Test: "should focus outcome panel after auto-advance"
  - [ ] 3.10 Test: "should maintain logical tab order across panels"
  - [ ] 3.11 Test: "should reset selected outcome when candidate changes"
  - [ ] 3.12 Test: "should show focus indicators on focusable elements"

- [ ] Task 4: Frontend -- ARIA live regions and accessibility attributes (AC: #9)
  - [ ] 4.1 Add `aria-live="polite"` region in ScreeningLayout for candidate switch announcements. Content: "Now reviewing [candidate name] at [step name]"
  - [ ] 4.2 Add `aria-live="assertive"` region for outcome recorded announcements. Content: "[Outcome] recorded for [candidate name]"
  - [ ] 4.3 Add `role="region"` and `aria-label="Outcome controls"` to the outcome panel container
  - [ ] 4.4 Add `role="region"` and `aria-label="Candidate list"` to the candidate list container
  - [ ] 4.5 Add `role="region"` and `aria-label="CV viewer"` to the center panel container
  - [ ] 4.6 Use visually hidden `<span>` elements for ARIA live region content (not visible but announced by screen readers). Use `sr-only` Tailwind class.
  - [ ] 4.7 Update ARIA live region text when `selectedCandidateId` changes (candidate switch) and when `handleOutcomeRecorded` fires (outcome recorded)
  - [ ] 4.8 Test: "should announce candidate switch via ARIA live region"
  - [ ] 4.9 Test: "should announce outcome recorded via ARIA live region"
  - [ ] 4.10 Test: "should have correct ARIA roles and labels on panels"

- [ ] Task 5: Frontend -- Update CandidatePanel for keyboard focus support (AC: #5, #6)
  - [ ] 5.1 Modify `web/src/features/screening/CandidatePanel.tsx` to forward a ref to its root element for keyboard event binding
  - [ ] 5.2 Add `tabIndex={0}` to the candidate list container so it can receive keyboard focus
  - [ ] 5.3 Add `role="listbox"` to the candidate list wrapper and `role="option"` with `aria-selected` to each candidate item (coordinate with CandidateList from Story 4.1 -- may need to pass these as props)
  - [ ] 5.4 Ensure the selected candidate row has `aria-selected="true"` and is scrolled into view when changed via Arrow keys
  - [ ] 5.5 Test: "should support keyboard focus on candidate list"
  - [ ] 5.6 Test: "should scroll selected candidate into view on arrow navigation"

## Dev Notes

### Affected Aggregate(s)

**No backend changes in this story.** Story 4.5 is purely frontend -- it adds keyboard navigation and focus management on top of Story 4.4's split-panel layout. All API endpoints and domain logic already exist from prerequisite stories.

### Important Architecture Context

**Focus Management Contract (architecture decision):**

The architecture document states: "After outcome submission, focus MUST return to the candidate list for keyboard navigation." However, the Story 4.5 acceptance criteria (AC4) specifies: "focus returns to the outcome panel (not the reason textarea, not the candidate list)." The story-level requirement takes precedence because the keyboard screening flow (AC10) requires `1`/`2`/`3` shortcuts to be immediately active after auto-advance, which means focus must be on the outcome panel, not the candidate list. The architecture's intent (enabling keyboard-first screening) is preserved -- just with a different focus target that better supports the rapid-fire screening flow.

**Keyboard Shortcut Scoping Strategy:**

Shortcuts (`1`/`2`/`3`) must ONLY fire when focus is on the outcome panel AND the active element is NOT a text input or textarea. This prevents keyboard shortcuts from interfering with typing in the search field (CandidatePanel) or reason textarea (OutcomeForm).

Implementation approach: Register `keydown` listener on the outcome panel ref element (scoped), then additionally check `document.activeElement` tag name to filter out text inputs. This gives two layers of scoping:
1. Event only fires when outcome panel or its non-input children have focus
2. Active element check prevents shortcuts when focus is on textarea/input within the panel

**Arrow Key Navigation Strategy:**

Arrow Up/Down for candidate list navigation is registered on the candidate list container ref. This keeps shortcut scoping clean -- outcome shortcuts on the outcome panel, navigation shortcuts on the candidate list. No document-level listeners needed.

**Auto-Advance Focus Coordination:**

Story 4.4's `useScreeningSession.handleOutcomeRecorded()` triggers auto-advance after 300ms. After auto-advance changes `selectedCandidateId`, Story 4.5 must focus the outcome panel. Use a `useEffect` that watches `selectedCandidateId` changes AND a "just advanced" flag to know when to trigger focus (vs manual candidate selection where focus should stay where it is).

Alternatively, modify `handleOutcomeRecorded` to accept an `onAutoAdvanceComplete` callback that triggers focus return. This is more explicit than a useEffect-based approach.

**Integration with Story 4.4 Components:**

- `ScreeningLayout.tsx` is the composition root. `useKeyboardNavigation` is called here.
- `useScreeningSession.selectCandidate(id)` is called by Arrow key navigation.
- `OutcomeForm.tsx` needs minor modifications: new `selectedOutcome` prop (for keyboard-driven selection), shortcut hint labels, and ref forwarding.
- `CandidatePanel.tsx` needs minor modifications: ref forwarding for keyboard event binding.
- `CandidateList.tsx` (Story 4.1) may need `role="option"` and `aria-selected` support on list items. If not already present, add via props.

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (useKeyboardNavigation) | **Test-first** | Core keyboard logic with shortcut scoping, focus management, and event filtering -- business-critical behavior |
| Task 2 (OutcomeForm updates) | **Test-first** | Shortcut hint display and controlled selection state are user-facing UI contracts |
| Task 3 (ScreeningLayout updates) | **Test-first** | Focus return after auto-advance and tab order are critical keyboard flow requirements |
| Task 4 (ARIA live regions) | **Test-first** | Accessibility is a core NFR; ARIA announcements must be verified to meet WCAG 2.1 AA |
| Task 5 (CandidatePanel updates) | **Characterization** | Minor ref forwarding and ARIA attributes on existing component |

### Technical Requirements

**Frontend -- useKeyboardNavigation hook:**

```typescript
// web/src/features/screening/hooks/useKeyboardNavigation.ts
import { useEffect, useCallback, useRef } from 'react';
import type { OutcomeStatus } from '@/lib/api/screening.types';

const OUTCOME_KEYS: Record<string, OutcomeStatus> = {
  '1': 'Pass',
  '2': 'Fail',
  '3': 'Hold',
};

const TEXT_INPUT_TAGS = new Set(['INPUT', 'TEXTAREA', 'SELECT']);

interface UseKeyboardNavigationOptions {
  outcomePanelRef: React.RefObject<HTMLDivElement>;
  candidateListRef: React.RefObject<HTMLDivElement>;
  onOutcomeSelect: (outcome: OutcomeStatus) => void;
  selectCandidate: (id: string) => void;
  candidates: Array<{ id: string }>;
  selectedCandidateId: string | null;
  enabled?: boolean; // default true, disable when no candidate selected
}

interface UseKeyboardNavigationReturn {
  focusOutcomePanel: () => void;
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
    if (!enabled) return;
    const panel = outcomePanelRef.current;
    if (!panel) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      // Skip if typing in a text input within the panel
      const activeTag = (e.target as HTMLElement).tagName;
      if (TEXT_INPUT_TAGS.has(activeTag)) return;
      if ((e.target as HTMLElement).isContentEditable) return;

      const outcome = OUTCOME_KEYS[e.key];
      if (outcome) {
        e.preventDefault();
        onOutcomeSelect(outcome);
      }
    };

    panel.addEventListener('keydown', handleKeyDown);
    return () => panel.removeEventListener('keydown', handleKeyDown);
  }, [outcomePanelRef, onOutcomeSelect, enabled]);

  // Candidate list keydown: Arrow Up/Down
  useEffect(() => {
    if (!enabled) return;
    const list = candidateListRef.current;
    if (!list) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
      e.preventDefault(); // Prevent scroll

      const currentIndex = candidates.findIndex(c => c.id === selectedCandidateId);
      if (currentIndex === -1) return;

      if (e.key === 'ArrowDown' && currentIndex < candidates.length - 1) {
        selectCandidate(candidates[currentIndex + 1].id);
      } else if (e.key === 'ArrowUp' && currentIndex > 0) {
        selectCandidate(candidates[currentIndex - 1].id);
      }
    };

    list.addEventListener('keydown', handleKeyDown);
    return () => list.removeEventListener('keydown', handleKeyDown);
  }, [candidateListRef, selectCandidate, candidates, selectedCandidateId, enabled]);

  const focusOutcomePanel = useCallback(() => {
    requestAnimationFrame(() => {
      outcomePanelRef.current?.focus();
    });
  }, [outcomePanelRef]);

  return { focusOutcomePanel };
}
```

**OutcomeForm modifications (minimal diff):**

The existing `OutcomeForm.tsx` needs these additions:
- New props: `selectedOutcome?: OutcomeStatus`, `onOutcomeSelect?: (outcome: OutcomeStatus) => void`
- Button labels changed from "Pass" to "Pass (1)", etc.
- `ring-2 ring-blue-500` on the selected outcome button
- `React.forwardRef` wrapper for the outcome panel container

**ScreeningLayout modifications (minimal diff):**

```typescript
// Additional state and refs in ScreeningLayout
const outcomePanelRef = useRef<HTMLDivElement>(null!);
const candidateListRef = useRef<HTMLDivElement>(null!);
const [selectedOutcome, setSelectedOutcome] = useState<OutcomeStatus | null>(null);
const prevCandidateIdRef = useRef<string | null>(null);

const { focusOutcomePanel } = useKeyboardNavigation({
  outcomePanelRef,
  candidateListRef,
  onOutcomeSelect: setSelectedOutcome,
  selectCandidate: session.selectCandidate,
  candidates,
  selectedCandidateId: session.selectedCandidateId,
  enabled: !!session.selectedCandidateId,
});

// Focus outcome panel after auto-advance
useEffect(() => {
  if (
    session.selectedCandidateId &&
    prevCandidateIdRef.current !== null &&
    prevCandidateIdRef.current !== session.selectedCandidateId
  ) {
    // Candidate changed (could be auto-advance or manual)
    setSelectedOutcome(null); // Reset selection for new candidate
  }
  prevCandidateIdRef.current = session.selectedCandidateId;
}, [session.selectedCandidateId]);

// ARIA live region state
const [ariaAnnouncement, setAriaAnnouncement] = useState('');
```

**ARIA live region pattern:**

```tsx
{/* Visually hidden ARIA live regions */}
<div aria-live="polite" aria-atomic="true" className="sr-only">
  {candidateAnnouncement}
</div>
<div aria-live="assertive" aria-atomic="true" className="sr-only">
  {outcomeAnnouncement}
</div>
```

### Architecture Compliance

- **Focus management contract:** Focus returns to outcome panel after auto-advance (per AC4), enabling `1`/`2`/`3` shortcuts for immediate next candidate. This deviates from architecture.md's general statement ("focus returns to candidate list") but fulfills the story's explicit requirement and the architecture's intent (keyboard-first screening).
- **Keyboard shortcut scoping:** Event listeners on specific panel refs (not document-level). Active element tag check prevents shortcuts during text input. Two-layer scoping ensures no interference.
- **Shared components:** Uses existing `OutcomeForm`, `CandidatePanel`, `CandidateList`, `PdfViewer`. No new shared components created.
- **ARIA live regions (NFR28):** `aria-live="polite"` for candidate switches, `aria-live="assertive"` for outcome recording. Content uses visually hidden spans (`sr-only`).
- **Focus indicators (WCAG 2.1 AA):** `focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2` on all focusable containers.
- **`prefers-reduced-motion`:** No animations added by this story. If auto-advance includes a transition, it already respects reduced motion from Story 4.4.
- **Ubiquitous language:** "Outcome" (not result), "Screening" (not review), "Candidate" (not applicant).
- **Feature isolation:** All new code in `features/screening/`. No cross-feature imports. `CandidateList` from `features/candidates/` consumed via its existing public interface (props).

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| React | 19.x | `useRef` for panel refs, `useEffect` for keydown listeners, `useCallback` for stable `focusOutcomePanel`. `requestAnimationFrame` for post-render focus. |
| TypeScript | 5.7.x | Strict mode. `KeyboardEvent` type for event handlers. `RefObject<HTMLDivElement>` for refs. |
| Tailwind CSS | 4.x | `focus-visible:outline-*` for focus indicators. `sr-only` for visually hidden ARIA content. `ring-2 ring-blue-500` for selected outcome button. |
| react-virtuoso | Latest | Candidate list uses virtualization. Arrow key navigation must call `scrollIntoView` or use virtuoso's `scrollToIndex` API to ensure selected candidate is visible. |

### File Structure Requirements

**New files to create:**
```
web/src/features/screening/
  hooks/
    useKeyboardNavigation.ts
    useKeyboardNavigation.test.ts
```

**Existing files to modify:**
```
web/src/features/screening/ScreeningLayout.tsx         -- Add useKeyboardNavigation, refs, ARIA regions, focus state
web/src/features/screening/ScreeningLayout.test.tsx    -- Add focus management and ARIA tests
web/src/features/screening/OutcomeForm.tsx             -- Add selectedOutcome prop, shortcut hints, ref forwarding
web/src/features/screening/OutcomeForm.test.tsx        -- Add shortcut hint and controlled selection tests
web/src/features/screening/CandidatePanel.tsx          -- Add ref forwarding, ARIA attributes
web/src/features/screening/CandidatePanel.test.tsx     -- Add keyboard focus and ARIA tests
```

**Files consumed from prerequisite stories (NOT modified):**
```
web/src/features/screening/hooks/useScreeningSession.ts   -- Story 4.4 (selectCandidate, handleOutcomeRecorded)
web/src/features/screening/hooks/useResizablePanel.ts     -- Story 4.4 (divider ARIA already present)
web/src/features/screening/PdfViewer.tsx                  -- Story 4.2 (text layer for accessibility)
web/src/features/candidates/CandidateList.tsx             -- Story 4.1 (virtualized list)
web/src/hooks/useFocusReturn.ts                           -- Shared focus utility (if needed)
```

### Testing Requirements

**Frontend tests (Vitest + Testing Library):**

useKeyboardNavigation:
- "should select Pass when 1 is pressed on outcome panel"
- "should select Fail when 2 is pressed on outcome panel"
- "should select Hold when 3 is pressed on outcome panel"
- "should NOT trigger shortcut when typing in textarea"
- "should NOT trigger shortcut when typing in input"
- "should NOT trigger shortcut when typing in contenteditable element"
- "should navigate to next candidate on Arrow Down"
- "should navigate to previous candidate on Arrow Up"
- "should not navigate past first candidate on Arrow Up"
- "should not navigate past last candidate on Arrow Down"
- "should prevent default scroll on Arrow keys in candidate list"
- "should focus outcome panel when focusOutcomePanel is called"
- "should not register listeners when enabled is false"
- "should clean up event listeners on unmount"

OutcomeForm (updated tests):
- "should display shortcut hints on outcome buttons"
- "should highlight selected outcome button with ring style"
- "should support controlled selectedOutcome prop"

ScreeningLayout (updated tests):
- "should focus outcome panel after auto-advance"
- "should maintain logical tab order: candidate list -> CV viewer -> outcome controls"
- "should reset selected outcome when candidate changes"
- "should show focus indicators on focusable panel containers"

ARIA and Accessibility:
- "should announce candidate switch via polite ARIA live region"
- "should announce outcome recorded via assertive ARIA live region"
- "should have role=region and aria-label on outcome panel"
- "should have role=region and aria-label on candidate list panel"
- "should have role=region and aria-label on CV viewer panel"

CandidatePanel (updated tests):
- "should support keyboard focus on candidate list container"
- "should have correct ARIA attributes for listbox pattern"

### Previous Story Intelligence

**From Story 4.4 (Split-Panel Screening Layout):**
- `ScreeningLayout.tsx` is the composition root at `web/src/features/screening/ScreeningLayout.tsx`. It uses `useParams()` for `recruitmentId`, renders three panels with `CandidatePanel`, `PdfViewer`, and `OutcomeForm`.
- `useScreeningSession.ts` hook at `web/src/features/screening/hooks/useScreeningSession.ts` provides `selectCandidate(id)`, `handleOutcomeRecorded(result)`, `selectedCandidateId`, `selectedCandidate`. Arrow key navigation calls `selectCandidate(id)`.
- `useScreeningSession.handleOutcomeRecorded` does optimistic update + 3-second delayed persist + auto-advance after 300ms. After auto-advance, Story 4.5's focus management must move focus to outcome panel.
- The resizable divider already has `role="separator"`, `aria-orientation="vertical"`, and `aria-label="Resize candidate list"`. No additional ARIA needed on the divider.
- Three isolated state domains: candidate list, PDF viewer, outcome form. Story 4.5's keyboard handler must not break this isolation -- register listeners on specific refs, not globally.
- `useScreeningSession.selectCandidate(id)` cancels any pending auto-advance, which is the correct behavior when Arrow keys manually change candidate.

**From Story 4.3 (Outcome Recording & Workflow Enforcement):**
- `OutcomeForm.tsx` at `web/src/features/screening/OutcomeForm.tsx` with Pass/Fail/Hold buttons + reason textarea + confirm button + `onOutcomeRecorded` callback.
- Story 4.5 needs to add: `selectedOutcome` controlled prop, `onOutcomeSelect` callback, shortcut hint labels ("Pass (1)"), ref forwarding.
- The confirm button triggers `onOutcomeRecorded` which feeds into `useScreeningSession.handleOutcomeRecorded`.

**From Story 4.1 (Candidate List & Search/Filter):**
- `CandidateList.tsx` uses react-virtuoso for virtualized rendering. Arrow key scrolling must use virtuoso's `scrollToIndex` API or ensure the selected item is visible.
- `CandidateResponse` type has `id`, `fullName`, `currentWorkflowStepName`, `currentOutcomeStatus`.

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes.

**Suggested commit sequence:**
1. `feat(4.5): add useKeyboardNavigation hook with shortcut scoping + tests`
2. `feat(4.5): update OutcomeForm with shortcut hints and controlled selection + tests`
3. `feat(4.5): update CandidatePanel with ref forwarding and ARIA attributes`
4. `feat(4.5): update ScreeningLayout with keyboard coordination and focus management + tests`
5. `feat(4.5): add ARIA live regions for dynamic content announcements + tests`

### Latest Tech Information

- **React 19.x:** `useRef` for DOM element references. `useEffect` with cleanup for event listener management. `useCallback` for stable function references. `requestAnimationFrame` for post-render focus operations.
- **Keyboard events:** Use `keydown` (not `keypress`, which is deprecated). `e.key` returns string values like `'1'`, `'ArrowUp'`, `'ArrowDown'`. `e.preventDefault()` stops default browser behavior.
- **Focus management:** `HTMLElement.focus()` sets focus. `document.activeElement` returns currently focused element. `tabIndex={0}` makes non-interactive elements focusable. `tabIndex={-1}` makes elements programmatically focusable but not in tab order.
- **ARIA live regions:** `aria-live="polite"` waits for current speech to finish. `aria-live="assertive"` interrupts current speech. `aria-atomic="true"` reads entire region content on update. Update content via React state to trigger screen reader announcement.
- **Tailwind CSS v4 `sr-only`:** `position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0);` -- visually hidden but accessible to screen readers.
- **Tailwind CSS v4 `focus-visible`:** Variant for `:focus-visible` pseudo-class. Shows focus ring only on keyboard navigation, not mouse clicks. `focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2`.
- **react-virtuoso `scrollToIndex`:** If using VirtuosoHandle ref, call `virtuosoRef.current.scrollToIndex({ index, align: 'center' })` to ensure selected candidate is visible after Arrow key navigation.

### Project Structure Notes

- `useKeyboardNavigation.ts` lives in `features/screening/hooks/` alongside `useScreeningSession.ts`, `useResizablePanel.ts`, and `usePdfPrefetch.ts` (as specified in the project structure document).
- Test files co-locate with source: `useKeyboardNavigation.test.ts` next to `useKeyboardNavigation.ts`.
- No new shared components or hooks needed. Uses existing `sr-only` Tailwind utility and existing shared components.
- The `useFocusReturn.ts` hook in `hooks/` (project structure doc) may be useful if focus-return-on-unmount is needed, but for Story 4.5 the focus management is explicit (after auto-advance), not unmount-based.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-screening-outcome-recording.md` -- Story 4.5 acceptance criteria, FR44, NFR22-29]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Frontend Architectural Constraints: Batch Screening (focus management contract, client-side state isolation), Accessibility (WCAG 2.1 AA)]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- Batch Screening Architecture (keyboard-first navigation, focus management)]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- Animation Accessibility (prefers-reduced-motion), Focus indicators, UI Consistency Rules]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- screening/ folder structure (useKeyboardNavigation.ts), Accessibility cross-cutting concern]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Frontend test naming convention, Vitest + Testing Library, Pragmatic TDD modes]
- [Source: `_bmad-output/implementation-artifacts/4-4-split-panel-screening-layout.md` -- ScreeningLayout.tsx, useScreeningSession.ts, CandidatePanel.tsx, useResizablePanel.ts (component details, integration points)]
- [Source: `_bmad-output/implementation-artifacts/4-3-outcome-recording-workflow-enforcement.md` -- OutcomeForm.tsx props, onOutcomeRecorded callback]
- [Source: `_bmad-output/implementation-artifacts/4-1-candidate-list-search-filter.md` -- CandidateList.tsx, CandidateResponse type, react-virtuoso]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy, mode declarations]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

None -- clean implementation, no blockers encountered.

### Completion Notes List

- **Testing mode:** Test-first for all tasks (useKeyboardNavigation, OutcomeForm updates, useScreeningSession onAutoAdvance, ScreeningLayout coordination, ARIA regions).
- **Plan deviation 1:** Combined story Tasks 2 (OutcomeForm shortcut hints) and 5 (externalOutcome prop) into a single implementation commit since both modify OutcomeForm.
- **Plan deviation 2:** Used `externalOutcome` prop name instead of `selectedOutcome` to distinguish externally-driven keyboard selection from OutcomeForm's internal `selectedOutcome` state.
- **Plan deviation 3:** Inlined `onAutoAdvance` callback directly in ScreeningLayout instead of passing `focusOutcomePanel` to avoid circular variable dependency between `useKeyboardNavigation` and `useScreeningSession`.
- **Plan deviation 4:** Used simple `React.RefObject` prop on CandidatePanel instead of `React.forwardRef` -- simpler approach that achieves the same result.
- **Plan deviation 5:** Did not add `role="listbox"` / `role="option"` to CandidateList (story Task 5.3-5.4) -- this would require modifying the react-virtuoso integration in CandidateList.tsx, which is a Story 4.1 component. The current implementation provides keyboard navigation via arrow keys scoped to the candidate panel ref without ARIA listbox semantics. This is a pragmatic tradeoff; listbox ARIA can be added in a future accessibility refinement.
- **Story 4.4 review fixes (I1, I2):** Applied before starting Story 4.5. I1: `key={selectedCandidate.id}` on OutcomeForm forces remount on candidate switch. I2: CandidateRow renders `<span>` instead of `<Link>` in screening mode.

### AC Coverage

| AC | Status | Implementation |
|----|--------|---------------|
| AC1 | Covered | `useKeyboardNavigation` OUTCOME_KEYS map + `OutcomeForm` externalOutcome prop |
| AC2 | Covered | TEXT_INPUT_TAGS filter + contentEditable check in keydown handler |
| AC3 | Covered | Natural DOM tab order in OutcomeForm: buttons -> textarea -> confirm |
| AC4 | Covered | `useScreeningSession` onAutoAdvance callback + `requestAnimationFrame` focus |
| AC5 | Covered | ArrowUp/ArrowDown handlers on candidateListRef |
| AC6 | Covered | Arrow listeners scoped to candidateListRef, no focus stealing |
| AC7 | Covered | `role="region"` + `aria-label` + `focus-visible:outline-*` on all panels |
| AC8 | Covered | `<kbd>` elements in OutcomeForm buttons: "Pass (1)", "Fail (2)", "Hold (3)" |
| AC9 | Covered | `aria-live="polite"` (candidate switch) + `aria-live="assertive"` (outcome) |
| AC10 | Covered | Full flow: 1/2/3 -> Tab reason -> Tab confirm -> Enter -> auto-advance -> repeat |

### Verification

- **Tests:** 285 passed, 0 failed (45 test files)
- **TypeScript:** `tsc --noEmit` exit code 0
- **Production build:** `vite build` exit code 0, built in 4.01s

### File List

**Created:**
- `web/src/features/screening/hooks/useKeyboardNavigation.ts` -- Keyboard navigation hook (1/2/3 shortcuts, ArrowUp/Down, focusOutcomePanel)
- `web/src/features/screening/hooks/useKeyboardNavigation.test.tsx` -- 12 tests for keyboard navigation hook
- `docs/plans/2026-02-14-keyboard-navigation-screening-flow.md` -- Implementation plan

**Modified:**
- `web/src/features/screening/OutcomeForm.tsx` -- Added externalOutcome prop, onOutcomeSelect callback, shortcut hint labels
- `web/src/features/screening/OutcomeForm.test.tsx` -- Added 3 tests (hints, externalOutcome, onOutcomeSelect)
- `web/src/features/screening/hooks/useScreeningSession.ts` -- Added onAutoAdvance callback option
- `web/src/features/screening/hooks/useScreeningSession.test.tsx` -- Added 1 test (onAutoAdvance callback)
- `web/src/features/screening/ScreeningLayout.tsx` -- Wired keyboard navigation, ARIA regions, focus management, outcome announcements
- `web/src/features/screening/ScreeningLayout.test.tsx` -- Added 2 tests (ARIA regions, ARIA live regions)
- `web/src/features/screening/CandidatePanel.tsx` -- Added candidateListRef prop, tabIndex, focus-visible styles
