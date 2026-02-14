# Story 4.4: Split-Panel Screening Layout

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **user (Lina)**,
I want a **split-panel layout where I can see the candidate list, their CV, and outcome controls side by side, with the ability to navigate between candidates and have auto-advance after recording an outcome**,
so that **I can screen candidates in a continuous flow without page reloads or context switching**.

## Acceptance Criteria

### AC1: Three-panel layout display
**Given** the user navigates to a recruitment's screening view
**When** the page loads
**Then** a three-panel layout is displayed: candidate list (left, min 250px), CV viewer (center, flexible width), outcome controls (right, fixed ~300px)
**And** the layout uses CSS Grid
**And** before any candidate is selected, the center and right panels show empty states ("Select a candidate to review their CV")

### AC2: Resizable left/center divider
**Given** the three-panel layout is displayed
**When** the user drags the divider between the left and center panels
**Then** the panels resize proportionally
**And** the resize ratio is persisted to localStorage
**And** on next visit, the persisted ratio is restored

### AC3: Candidate selection loads CV and outcome controls
**Given** the user selects a candidate in the left panel
**When** the candidate loads
**Then** the center panel shows their CV (via PdfViewer from Story 4.2)
**And** the right panel shows their outcome controls (via OutcomeForm from Story 4.3)
**And** the candidate name and current status are displayed in the right panel header

### AC4: Candidate switching without page reload
**Given** the user is viewing candidate A and clicks candidate B in the list
**When** candidate B is selected
**Then** the CV viewer updates to candidate B's document
**And** the outcome controls update to candidate B's current step and status
**And** the transition happens without a page reload

### AC5: Optimistic outcome with delayed persist and undo
**Given** the user records an outcome for a candidate
**When** the outcome is confirmed
**Then** the outcome is applied optimistically (shown immediately in the UI without waiting for the API)
**And** a bottom-right toast slides in (~150ms): "Pass recorded for [Name] - Undo"
**And** the toast auto-dismisses after 3 seconds
**And** during those 3 seconds, clicking "Undo" reverses the action (no API call needed)
**And** after 3 seconds, the outcome is persisted to the server via API call

### AC6: Undo reversal
**Given** the user clicks "Undo" on the toast within 3 seconds
**When** the undo is processed
**Then** the outcome is reversed in the UI
**And** the candidate returns to their previous state
**And** no API call is made (the delayed persist is cancelled)

### AC7: Auto-advance to next unscreened candidate
**Given** the user records an outcome and auto-advance is active
**When** the outcome is confirmed (after the brief ~300ms confirmation transition)
**Then** the next unscreened candidate below the current one in the list is automatically selected
**And** their CV and outcome controls load
**And** if no unscreened candidates remain below, it wraps to the top of the list
**And** if all candidates in the current filter are screened, the view stays on the current candidate with a completion indicator

### AC8: Override auto-advance
**Given** the user wants to override auto-advance
**When** they click a different candidate in the list during or after the confirmation transition
**Then** the clicked candidate is selected instead of the auto-advance target

### AC9: Screening progress display
**Given** the user is in the screening layout
**When** they view the candidate list panel
**Then** screening progress is displayed: total progress ("47 of 130 screened") and session progress ("12 this session")
**And** session progress is a client-side counter that resets on page refresh

### Prerequisites
- **Story 4.1** (Candidate List & Search/Filter) -- CandidateList.tsx with search/filter/pagination, useCandidates hook, CandidateResponse with currentWorkflowStepId/currentOutcomeStatus, react-virtuoso for virtualized list
- **Story 4.2** (PDF Viewing & Download) -- PdfViewer.tsx component, usePdfPrefetch hook, CandidateResponse.sasUrl, pdfConfig.ts worker setup
- **Story 4.3** (Outcome Recording & Workflow Enforcement) -- OutcomeForm.tsx component, useRecordOutcome hook, OutcomeHistory.tsx, screeningApi client, OutcomeResultDto with newCurrentStepId/isCompleted
- **Epic 2** -- Recruitment with WorkflowSteps, useRecruitment hook
- **Story 1.4** -- Shared UI components (StatusBadge, EmptyState, SkeletonLoader, PaginationControls, useAppToast)

### FRs Fulfilled
- **FR41:** Users can view candidate list, CV, and outcome controls simultaneously in a split-panel layout
- **FR42:** Outcome recording uses optimistic UI with undo capability (3-second window)
- **FR43:** After recording an outcome, the system auto-advances to the next unscreened candidate

## Tasks / Subtasks

- [ ] Task 1: Frontend -- useResizablePanel hook with localStorage persistence (AC: #2)
  - [ ] 1.1 Create `web/src/features/screening/hooks/useResizablePanel.ts`
  - [ ] 1.2 Hook accepts `storageKey: string`, `defaultRatio: number` (0-1 representing left panel proportion), `minLeftPx: number` (250), `minCenterPx: number` (300)
  - [ ] 1.3 Returns `{ leftWidth: number, centerWidth: number, dividerProps: { onMouseDown, ref } }` -- widths computed from container width and ratio
  - [ ] 1.4 On mousedown on divider, register mousemove/mouseup on document to track drag. Compute new ratio from mouse position relative to container. Clamp to min widths. Persist to localStorage on mouseup
  - [ ] 1.5 On mount, read persisted ratio from localStorage (or use default). On window resize, recompute widths from persisted ratio
  - [ ] 1.6 Use `useRef` for container element, `useCallback` for event handlers, `useState` for ratio
  - [ ] 1.7 Test: "should initialize with default ratio when no localStorage value"
  - [ ] 1.8 Test: "should restore ratio from localStorage on mount"
  - [ ] 1.9 Test: "should persist ratio to localStorage on drag end"
  - [ ] 1.10 Test: "should enforce minimum widths during resize"

- [ ] Task 2: Frontend -- useScreeningSession hook for session state coordination (AC: #3, #4, #5, #6, #7, #8, #9)
  - [ ] 2.1 Create `web/src/features/screening/hooks/useScreeningSession.ts`
  - [ ] 2.2 Hook accepts `recruitmentId: string`, `candidates: CandidateResponse[]` (from useCandidates)
  - [ ] 2.3 State: `selectedCandidateId: string | null`, `sessionScreenedCount: number`, `pendingOutcome: PendingOutcome | null` (holds outcome data during 3-second delay window)
  - [ ] 2.4 `PendingOutcome` type: `{ candidateId: string, candidateName: string, outcome: OutcomeStatus, reason: string | null, previousState: CandidateResponse, timeoutId: number }`
  - [ ] 2.5 `selectCandidate(id: string)` -- sets selectedCandidateId, cancels any pending auto-advance
  - [ ] 2.6 `handleOutcomeRecorded(result: OutcomeResultDto)`:
    - Optimistically update TanStack Query cache for candidates list (update candidate's currentOutcomeStatus, currentWorkflowStepId)
    - Store `PendingOutcome` with 3-second timeout
    - Show toast with undo action via `useAppToast()`
    - Increment `sessionScreenedCount`
    - After ~300ms confirmation transition, auto-advance to next unscreened candidate (currentOutcomeStatus === 'NotStarted' or null)
    - After 3 seconds, call `screeningApi.recordOutcome()` to persist
  - [ ] 2.7 `undoOutcome()`:
    - Clear pending timeout (cancel API call)
    - Restore candidate's previous state in TanStack Query cache
    - Decrement `sessionScreenedCount`
    - Select the undone candidate
    - Dismiss toast
  - [ ] 2.8 `findNextUnscreened()`:
    - From current index, search forward in candidates array for candidate with no outcome at current step (currentOutcomeStatus is null or 'NotStarted')
    - If none found below, wrap to start of list
    - If all screened, return null (stay on current candidate)
  - [ ] 2.9 Expose: `selectedCandidateId`, `selectedCandidate` (resolved from candidates array), `sessionScreenedCount`, `totalScreenedCount` (computed from candidates), `selectCandidate`, `handleOutcomeRecorded`, `undoOutcome`, `isAllScreened`
  - [ ] 2.10 Test: "should select candidate and update selectedCandidateId"
  - [ ] 2.11 Test: "should optimistically update candidate status on outcome recorded"
  - [ ] 2.12 Test: "should auto-advance to next unscreened candidate after outcome"
  - [ ] 2.13 Test: "should undo outcome and restore previous state"
  - [ ] 2.14 Test: "should cancel API call when undo is triggered"
  - [ ] 2.15 Test: "should persist outcome via API after 3 seconds without undo"
  - [ ] 2.16 Test: "should increment session count on outcome and decrement on undo"
  - [ ] 2.17 Test: "should wrap to top of list when no unscreened below"
  - [ ] 2.18 Test: "should stay on current candidate when all are screened"
  - [ ] 2.19 Test: "should override auto-advance when user clicks different candidate"

- [ ] Task 3: Frontend -- ScreeningLayout component with CSS Grid (AC: #1, #2, #3, #4)
  - [ ] 3.1 Create `web/src/features/screening/ScreeningLayout.tsx` as the main screening page component
  - [ ] 3.2 Layout structure: CSS Grid with `grid-template-columns` using pixel values from `useResizablePanel`. Three columns: left panel (candidate list), center panel (PDF viewer), right panel (outcome controls, fixed ~300px)
  - [ ] 3.3 Left panel renders `CandidatePanel` (wraps CandidateList from Story 4.1) with search/filter and progress indicator
  - [ ] 3.4 Center panel renders `PdfViewer` from Story 4.2, passing `sasUrl` from `usePdfPrefetch` or candidate's sasUrl
  - [ ] 3.5 Right panel renders `OutcomeForm` from Story 4.3 in a container with candidate name and current step in the header
  - [ ] 3.6 Before any candidate is selected, center panel shows `EmptyState` with "Select a candidate to review their CV" and right panel shows `EmptyState` with "Select a candidate to record an outcome"
  - [ ] 3.7 Draggable divider between left and center panels: a 4px vertical bar (`cursor: col-resize`) that handles mousedown from `useResizablePanel`
  - [ ] 3.8 Wire `useScreeningSession` to coordinate candidate selection, outcome recording, auto-advance, and undo
  - [ ] 3.9 Wire `usePdfPrefetch` to pre-fetch SAS URLs for upcoming candidates
  - [ ] 3.10 Pass `onOutcomeRecorded` callback from `useScreeningSession.handleOutcomeRecorded` to `OutcomeForm`
  - [ ] 3.11 Use `useParams()` to get `recruitmentId` from the route
  - [ ] 3.12 Use `useRecruitment(recruitmentId)` to get recruitment data (for workflow steps, closed status)
  - [ ] 3.13 Test: "should render three-panel layout with CSS Grid"
  - [ ] 3.14 Test: "should show empty states before any candidate is selected"
  - [ ] 3.15 Test: "should load CV and outcome controls when candidate is selected"
  - [ ] 3.16 Test: "should update panels when switching between candidates"
  - [ ] 3.17 Test: "should display resizable divider between left and center panels"

- [ ] Task 4: Frontend -- CandidatePanel component for left panel (AC: #1, #9)
  - [ ] 4.1 Create `web/src/features/screening/CandidatePanel.tsx` as the left panel wrapper
  - [ ] 4.2 Props: `recruitmentId: string`, `selectedCandidateId: string | null`, `onCandidateSelect: (id: string) => void`, `sessionScreenedCount: number`, `totalScreenedCount: number`, `totalCandidateCount: number`, `isAllScreened: boolean`
  - [ ] 4.3 Renders progress header: "47 of 130 screened" (total) and "12 this session" (session)
  - [ ] 4.4 Renders `CandidateList` from Story 4.1 with search/filter capability, passing `onSelect` callback and `selectedId` for highlighting
  - [ ] 4.5 When `isAllScreened`, show a completion banner ("All candidates screened!") above the list
  - [ ] 4.6 Selected candidate row is highlighted with a distinct background color
  - [ ] 4.7 Test: "should display screening progress header"
  - [ ] 4.8 Test: "should highlight selected candidate in list"
  - [ ] 4.9 Test: "should show completion banner when all screened"
  - [ ] 4.10 Test: "should propagate candidate selection to parent"

- [ ] Task 5: Frontend -- Route registration for screening view (AC: #1)
  - [ ] 5.1 Add route in `web/src/routes/index.tsx` for `/recruitments/:recruitmentId/screening` that renders `ScreeningLayout`
  - [ ] 5.2 Add navigation link/button to `ScreeningLayout` from the recruitment detail view (e.g., "Start Screening" action button)
  - [ ] 5.3 Ensure route is protected via `ProtectedRoute`

- [ ] Task 6: Frontend -- MSW handlers and fixtures for screening session (AC: #5, #6, #7)
  - [ ] 6.1 Extend existing MSW candidate handlers to support optimistic update scenarios
  - [ ] 6.2 Add screening session fixtures with a mix of screened and unscreened candidates for testing auto-advance logic
  - [ ] 6.3 Ensure MSW handlers return `OutcomeResultDto` with `newCurrentStepId` and `isCompleted` fields for auto-advance testing

## Dev Notes

### Affected Aggregate(s)

**No backend changes in this story.** Story 4.4 is a purely frontend story that integrates components from Stories 4.1, 4.2, and 4.3 into a split-panel layout. All API endpoints and domain logic already exist from those prerequisite stories.

**Candidate** (read-only via existing queries) -- Selected candidate data flows from `useCandidates` (Story 4.1) and `useCandidateById` (Story 4.1). Optimistic cache updates modify TanStack Query's client-side cache, not the server state directly.

**Recruitment** (read-only) -- Loaded via `useRecruitment` hook to check closed status and get workflow steps for the right panel header.

### Important Integration Architecture

**Three Isolated State Domains (architecture constraint):**

The architecture explicitly requires three isolated state domains in the screening layout. These coordinate but must not cascade re-renders:

1. **Candidate List (left panel)** -- Owned by `useCandidates` (TanStack Query). Search/filter state is local to `CandidatePanel`. Selection state flows via props.
2. **PDF Viewer (center panel)** -- Owned by `usePdfPrefetch` + `PdfViewer` internal state. Receives `sasUrl` as prop; manages loading/error state internally.
3. **Outcome Form (right panel)** -- Owned by `OutcomeForm` internal state (selection, reason text). Receives candidate context as props; calls back via `onOutcomeRecorded`.

**Coordination is via `useScreeningSession` hook** which lives at the `ScreeningLayout` level. It manages:
- `selectedCandidateId` -- drives which candidate's data flows to center and right panels
- `pendingOutcome` -- optimistic state during the 3-second undo window
- `sessionScreenedCount` -- client-side counter

**Re-render isolation:** The three panels receive only the data they need via props. When an outcome is recorded:
1. `useScreeningSession` updates `selectedCandidateId` (triggers left + center + right to update)
2. `useScreeningSession` optimistically updates TanStack Query cache (triggers left panel list to re-render with new status badges)
3. `PdfViewer` gets a new `sasUrl` prop (re-renders with new PDF or from cache)
4. `OutcomeForm` gets new `candidateId`/`currentStepId` props (re-renders with fresh form)

**Optimistic Update Flow (detailed):**

```
User clicks confirm → handleOutcomeRecorded fires:
  1. queryClient.setQueryData(['candidates', recruitmentId], (old) => {
       // Update candidate's currentOutcomeStatus in the cached list
     })
  2. setPendingOutcome({ candidateId, previousState, timeoutId: setTimeout(() => {
       // After 3 seconds: call screeningApi.recordOutcome()
       // On success: invalidate queries for fresh data
       // On error: rollback cache, show error toast
     }, 3000) })
  3. toast('Pass recorded for [Name]', { action: { label: 'Undo', onClick: undoOutcome } })
  4. setTimeout(() => {
       // After 300ms: auto-advance
       selectCandidate(findNextUnscreened())
     }, 300)
```

**Undo Flow:**

```
User clicks Undo → undoOutcome fires:
  1. clearTimeout(pendingOutcome.timeoutId)  // Cancel API call
  2. queryClient.setQueryData(['candidates', recruitmentId], (old) => {
       // Restore candidate's previousState in cache
     })
  3. setPendingOutcome(null)
  4. selectCandidate(pendingOutcome.candidateId)  // Go back to undone candidate
  5. sessionScreenedCount--
```

### Testing: Pragmatic TDD

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (useResizablePanel) | **Test-first** | Non-trivial state logic with localStorage persistence and drag math |
| Task 2 (useScreeningSession) | **Test-first** | Core orchestration logic: optimistic updates, undo, auto-advance, timer management |
| Task 3 (ScreeningLayout) | **Test-first** | Integration component: verify panels render, candidate selection flows, empty states |
| Task 4 (CandidatePanel) | **Test-first** | User-facing display: progress header, selection highlighting, completion state |
| Task 5 (Route registration) | **Characterization** | Thin routing config -- tested via component integration tests |
| Task 6 (MSW handlers) | **Characterization** | Test infrastructure supporting other tests |

### Technical Requirements

**Frontend -- useResizablePanel hook:**

```typescript
// web/src/features/screening/hooks/useResizablePanel.ts
import { useState, useRef, useCallback, useEffect } from 'react';

const STORAGE_PREFIX = 'screening-panel-ratio-';

interface UseResizablePanelOptions {
  storageKey: string;
  defaultRatio?: number; // 0-1, left panel proportion (default 0.25)
  minLeftPx?: number;    // default 250
  minCenterPx?: number;  // default 300
}

interface UseResizablePanelReturn {
  containerRef: React.RefObject<HTMLDivElement>;
  leftWidth: number;
  centerWidth: number;
  isDragging: boolean;
  dividerProps: {
    onMouseDown: (e: React.MouseEvent) => void;
    style: React.CSSProperties;
  };
}

export function useResizablePanel({
  storageKey,
  defaultRatio = 0.25,
  minLeftPx = 250,
  minCenterPx = 300,
}: UseResizablePanelOptions): UseResizablePanelReturn {
  const containerRef = useRef<HTMLDivElement>(null!);
  const [ratio, setRatio] = useState(() => {
    const stored = localStorage.getItem(STORAGE_PREFIX + storageKey);
    return stored ? parseFloat(stored) : defaultRatio;
  });
  const [isDragging, setIsDragging] = useState(false);
  const [containerWidth, setContainerWidth] = useState(0);

  // Observe container width
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const observer = new ResizeObserver(([entry]) => {
      setContainerWidth(entry.contentRect.width);
    });
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const RIGHT_PANEL_WIDTH = 300; // Fixed right panel
  const DIVIDER_WIDTH = 4;
  const availableWidth = containerWidth - RIGHT_PANEL_WIDTH - DIVIDER_WIDTH;
  const leftWidth = Math.max(minLeftPx, Math.min(availableWidth - minCenterPx, availableWidth * ratio));
  const centerWidth = availableWidth - leftWidth;

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsDragging(true);
    const startX = e.clientX;
    const startRatio = ratio;

    const onMouseMove = (moveEvent: MouseEvent) => {
      const delta = moveEvent.clientX - startX;
      const newLeft = availableWidth * startRatio + delta;
      const clamped = Math.max(minLeftPx, Math.min(availableWidth - minCenterPx, newLeft));
      setRatio(clamped / availableWidth);
    };

    const onMouseUp = () => {
      setIsDragging(false);
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      // Persist on drag end
      setRatio((currentRatio) => {
        localStorage.setItem(STORAGE_PREFIX + storageKey, currentRatio.toString());
        return currentRatio;
      });
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }, [ratio, availableWidth, minLeftPx, minCenterPx, storageKey]);

  return {
    containerRef,
    leftWidth,
    centerWidth,
    isDragging,
    dividerProps: {
      onMouseDown: handleMouseDown,
      style: { cursor: 'col-resize', width: `${DIVIDER_WIDTH}px` },
    },
  };
}
```

**Frontend -- useScreeningSession hook:**

```typescript
// web/src/features/screening/hooks/useScreeningSession.ts
import { useState, useCallback, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { screeningApi } from '@/lib/api/screening';
import { useAppToast } from '@/components/Toast/useAppToast';
import type { CandidateResponse } from '@/lib/api/candidates.types';
import type { OutcomeResultDto } from '@/lib/api/screening.types';

const UNDO_WINDOW_MS = 3000;
const AUTO_ADVANCE_DELAY_MS = 300;

interface PendingOutcome {
  candidateId: string;
  candidateName: string;
  outcome: string;
  reason: string | null;
  previousState: CandidateResponse;
  persistTimeoutId: number;
  request: {
    recruitmentId: string;
    candidateId: string;
    workflowStepId: string;
    outcome: string;
    reason?: string;
  };
}

export function useScreeningSession(
  recruitmentId: string,
  candidates: CandidateResponse[],
) {
  const [selectedCandidateId, setSelectedCandidateId] = useState<string | null>(null);
  const [sessionScreenedCount, setSessionScreenedCount] = useState(0);
  const pendingRef = useRef<PendingOutcome | null>(null);
  const autoAdvanceRef = useRef<number | null>(null);
  const queryClient = useQueryClient();
  const { toast, dismiss } = useAppToast();

  const selectedCandidate = candidates.find(c => c.id === selectedCandidateId) ?? null;

  const totalScreenedCount = candidates.filter(
    c => c.currentOutcomeStatus && c.currentOutcomeStatus !== 'NotStarted'
  ).length;

  const isAllScreened = totalScreenedCount === candidates.length && candidates.length > 0;

  const selectCandidate = useCallback((id: string) => {
    // Cancel pending auto-advance if user manually selects
    if (autoAdvanceRef.current) {
      clearTimeout(autoAdvanceRef.current);
      autoAdvanceRef.current = null;
    }
    setSelectedCandidateId(id);
  }, []);

  const findNextUnscreened = useCallback((): string | null => {
    if (!selectedCandidateId) return null;
    const currentIndex = candidates.findIndex(c => c.id === selectedCandidateId);
    if (currentIndex === -1) return null;

    // Search forward from current
    for (let i = currentIndex + 1; i < candidates.length; i++) {
      if (!candidates[i].currentOutcomeStatus || candidates[i].currentOutcomeStatus === 'NotStarted') {
        return candidates[i].id;
      }
    }
    // Wrap to start
    for (let i = 0; i < currentIndex; i++) {
      if (!candidates[i].currentOutcomeStatus || candidates[i].currentOutcomeStatus === 'NotStarted') {
        return candidates[i].id;
      }
    }
    return null; // All screened
  }, [candidates, selectedCandidateId]);

  const handleOutcomeRecorded = useCallback((result: OutcomeResultDto) => {
    const candidate = candidates.find(c => c.id === result.candidateId);
    if (!candidate) return;

    // 1. Save previous state for undo
    const previousState = { ...candidate };

    // 2. Optimistically update TanStack Query cache
    queryClient.setQueryData(
      ['candidates', recruitmentId],
      (old: any) => {
        if (!old) return old;
        return {
          ...old,
          items: old.items.map((c: CandidateResponse) =>
            c.id === result.candidateId
              ? {
                  ...c,
                  currentOutcomeStatus: result.outcome,
                  currentWorkflowStepId: result.newCurrentStepId ?? c.currentWorkflowStepId,
                }
              : c
          ),
        };
      }
    );

    // 3. Set up delayed persist with undo window
    const persistTimeoutId = window.setTimeout(() => {
      // Persist to server
      screeningApi.recordOutcome(recruitmentId, result.candidateId, {
        workflowStepId: result.workflowStepId,
        outcome: result.outcome as any,
        reason: result.reason ?? undefined,
      }).then(() => {
        // Invalidate to get fresh server data
        queryClient.invalidateQueries({ queryKey: ['candidates', recruitmentId] });
        queryClient.invalidateQueries({ queryKey: ['screening', 'history', result.candidateId] });
      }).catch(() => {
        // Rollback on failure
        queryClient.setQueryData(
          ['candidates', recruitmentId],
          (old: any) => {
            if (!old) return old;
            return {
              ...old,
              items: old.items.map((c: CandidateResponse) =>
                c.id === result.candidateId ? previousState : c
              ),
            };
          }
        );
        toast({ description: 'Failed to save outcome. Please try again.', variant: 'destructive' });
      });
      pendingRef.current = null;
    }, UNDO_WINDOW_MS);

    pendingRef.current = {
      candidateId: result.candidateId,
      candidateName: candidate.fullName,
      outcome: result.outcome,
      reason: result.reason,
      previousState,
      persistTimeoutId,
      request: {
        recruitmentId,
        candidateId: result.candidateId,
        workflowStepId: result.workflowStepId,
        outcome: result.outcome,
        reason: result.reason ?? undefined,
      },
    };

    // 4. Show toast with undo
    toast({
      description: `${result.outcome} recorded for ${candidate.fullName}`,
      action: { label: 'Undo', onClick: undoOutcome },
      duration: UNDO_WINDOW_MS,
    });

    // 5. Increment session count
    setSessionScreenedCount(prev => prev + 1);

    // 6. Auto-advance after brief delay
    autoAdvanceRef.current = window.setTimeout(() => {
      const nextId = findNextUnscreened();
      if (nextId) {
        setSelectedCandidateId(nextId);
      }
      autoAdvanceRef.current = null;
    }, AUTO_ADVANCE_DELAY_MS);
  }, [candidates, recruitmentId, queryClient, toast, findNextUnscreened]);

  const undoOutcome = useCallback(() => {
    const pending = pendingRef.current;
    if (!pending) return;

    // 1. Cancel persist timeout
    clearTimeout(pending.persistTimeoutId);

    // 2. Cancel auto-advance
    if (autoAdvanceRef.current) {
      clearTimeout(autoAdvanceRef.current);
      autoAdvanceRef.current = null;
    }

    // 3. Restore previous state in cache
    queryClient.setQueryData(
      ['candidates', recruitmentId],
      (old: any) => {
        if (!old) return old;
        return {
          ...old,
          items: old.items.map((c: CandidateResponse) =>
            c.id === pending.candidateId ? pending.previousState : c
          ),
        };
      }
    );

    // 4. Clear pending
    pendingRef.current = null;

    // 5. Go back to undone candidate
    setSelectedCandidateId(pending.candidateId);

    // 6. Decrement session count
    setSessionScreenedCount(prev => Math.max(0, prev - 1));

    // 7. Dismiss toast
    dismiss();
  }, [recruitmentId, queryClient, dismiss]);

  return {
    selectedCandidateId,
    selectedCandidate,
    sessionScreenedCount,
    totalScreenedCount,
    isAllScreened,
    selectCandidate,
    handleOutcomeRecorded,
    undoOutcome,
  };
}
```

**Frontend -- ScreeningLayout component:**

```typescript
// web/src/features/screening/ScreeningLayout.tsx
import { useParams } from 'react-router-dom';
import { useRecruitment } from '@/features/recruitments/hooks/useRecruitments';
import { useCandidates } from '@/features/candidates/hooks/useCandidates';
import { usePdfPrefetch } from './hooks/usePdfPrefetch';
import { useResizablePanel } from './hooks/useResizablePanel';
import { useScreeningSession } from './hooks/useScreeningSession';
import { CandidatePanel } from './CandidatePanel';
import { PdfViewer } from './PdfViewer';
import { OutcomeForm } from './OutcomeForm';
import { EmptyState } from '@/components/EmptyState';
import { SkeletonLoader } from '@/components/SkeletonLoader';
import type { CandidateResponse } from '@/lib/api/candidates.types';

export function ScreeningLayout() {
  const { recruitmentId } = useParams<{ recruitmentId: string }>();
  const { data: recruitment, isLoading: recruitmentLoading } = useRecruitment(recruitmentId!);
  const { data: candidateData, isLoading: candidatesLoading } = useCandidates({
    recruitmentId: recruitmentId!,
    pageSize: 50,
  });

  const candidates: CandidateResponse[] = candidateData?.items ?? [];
  const currentIndex = candidates.findIndex(c => c.id === session.selectedCandidateId);

  const panel = useResizablePanel({
    storageKey: recruitmentId!,
    defaultRatio: 0.25,
    minLeftPx: 250,
    minCenterPx: 300,
  });

  const session = useScreeningSession(recruitmentId!, candidates);
  const prefetch = usePdfPrefetch(recruitmentId!, candidates, currentIndex);

  const isRecruitmentActive = recruitment?.status !== 'Closed';
  const selectedCandidate = session.selectedCandidate;
  const sasUrl = selectedCandidate
    ? prefetch.getSasUrl(selectedCandidate.id) ?? selectedCandidate.sasUrl ?? null
    : null;

  if (recruitmentLoading || candidatesLoading) {
    return <SkeletonLoader variant="card" />;
  }

  return (
    <div
      ref={panel.containerRef}
      className="flex h-full"
      style={{ userSelect: panel.isDragging ? 'none' : 'auto' }}
    >
      {/* Left Panel: Candidate List */}
      <div style={{ width: panel.leftWidth, flexShrink: 0 }} className="border-r overflow-hidden">
        <CandidatePanel
          recruitmentId={recruitmentId!}
          selectedCandidateId={session.selectedCandidateId}
          onCandidateSelect={session.selectCandidate}
          sessionScreenedCount={session.sessionScreenedCount}
          totalScreenedCount={session.totalScreenedCount}
          totalCandidateCount={candidates.length}
          isAllScreened={session.isAllScreened}
        />
      </div>

      {/* Resizable Divider */}
      <div
        {...panel.dividerProps}
        className="bg-gray-200 hover:bg-blue-400 transition-colors flex-shrink-0"
        role="separator"
        aria-orientation="vertical"
        aria-label="Resize candidate list"
      />

      {/* Center Panel: PDF Viewer */}
      <div style={{ width: panel.centerWidth, flexShrink: 0 }} className="overflow-hidden">
        {selectedCandidate ? (
          <PdfViewer
            sasUrl={sasUrl}
            candidateName={selectedCandidate.fullName}
            isRecruitmentActive={isRecruitmentActive}
          />
        ) : (
          <EmptyState
            title="Select a candidate"
            description="Choose a candidate from the list to review their CV."
          />
        )}
      </div>

      {/* Right Panel: Outcome Controls */}
      <div className="w-[300px] flex-shrink-0 border-l overflow-y-auto">
        {selectedCandidate ? (
          <div className="flex flex-col h-full">
            <div className="p-4 border-b">
              <h2 className="font-semibold text-lg truncate">{selectedCandidate.fullName}</h2>
              <p className="text-sm text-gray-500">
                {selectedCandidate.currentWorkflowStepName ?? 'No step assigned'}
              </p>
            </div>
            <div className="flex-1 p-4">
              <OutcomeForm
                recruitmentId={recruitmentId!}
                candidateId={selectedCandidate.id}
                currentStepId={selectedCandidate.currentWorkflowStepId!}
                currentStepName={selectedCandidate.currentWorkflowStepName!}
                existingOutcome={null}
                isClosed={!isRecruitmentActive}
                onOutcomeRecorded={session.handleOutcomeRecorded}
              />
            </div>
          </div>
        ) : (
          <EmptyState
            title="Select a candidate"
            description="Choose a candidate from the list to record an outcome."
          />
        )}
      </div>
    </div>
  );
}
```

**Frontend -- CandidatePanel component:**

```typescript
// web/src/features/screening/CandidatePanel.tsx
import { CandidateList } from '@/features/candidates/CandidateList';

interface CandidatePanelProps {
  recruitmentId: string;
  selectedCandidateId: string | null;
  onCandidateSelect: (id: string) => void;
  sessionScreenedCount: number;
  totalScreenedCount: number;
  totalCandidateCount: number;
  isAllScreened: boolean;
}

export function CandidatePanel({
  recruitmentId,
  selectedCandidateId,
  onCandidateSelect,
  sessionScreenedCount,
  totalScreenedCount,
  totalCandidateCount,
  isAllScreened,
}: CandidatePanelProps) {
  return (
    <div className="flex flex-col h-full">
      {/* Progress Header */}
      <div className="p-3 border-b bg-gray-50">
        <div className="flex justify-between text-sm">
          <span className="font-medium">
            {totalScreenedCount} of {totalCandidateCount} screened
          </span>
          <span className="text-gray-500">{sessionScreenedCount} this session</span>
        </div>
        {isAllScreened && (
          <div className="mt-2 px-3 py-1.5 bg-green-50 text-green-700 text-sm rounded-md text-center font-medium">
            All candidates screened!
          </div>
        )}
      </div>

      {/* Candidate List */}
      <div className="flex-1 overflow-hidden">
        <CandidateList
          recruitmentId={recruitmentId}
          selectedId={selectedCandidateId}
          onSelect={onCandidateSelect}
        />
      </div>
    </div>
  );
}
```

### Architecture Compliance

- **Three isolated state domains:** Candidate list, PDF viewer, and outcome form are three independent state domains coordinating through `useScreeningSession` at the layout level. No cross-panel state leaks (architecture: "Three panels coordinate but must not cascade re-renders").
- **Focus management contract:** After outcome submission, focus is managed via the auto-advance flow. Focus returns to the outcome panel for the next candidate (architecture: "focus MUST return to the candidate list for keyboard navigation" -- adjusted for Story 4.5 which handles detailed focus management).
- **Optimistic outcome recording:** TanStack Query cache updated immediately, API call delayed 3 seconds with undo window (architecture: "Record locally, sync to API, show confirmation. Non-blocking retry on failure").
- **Shared components:** Uses `EmptyState`, `SkeletonLoader`, `StatusBadge`, `useAppToast` from `components/`. No feature-local equivalents.
- **httpClient.ts as single HTTP entry point:** `screeningApi` and `candidateApi` are consumed through existing API client modules. No direct `fetch` calls.
- **Desktop-first (1280px minimum):** Split-panel layout requires sufficient horizontal space. No responsive breakpoints.
- **Ubiquitous language:** "Screening" (not review), "Candidate" (not applicant), "Outcome" (not result), "Workflow Step" (not stage).
- **URL state for selection:** `recruitmentId` from URL params. Candidate selection is component state (not URL) because it changes rapidly during batch screening.
- **Feature isolation:** `features/screening/` does not import from other feature folders. It consumes `CandidateList` via a well-defined interface and shared API types.

### Library & Framework Requirements

| Library | Version | Key Notes |
|---------|---------|-----------|
| React | 19.x | Hooks for state management. `useRef` for mutable refs (timers, pending outcomes). `useCallback` for stable function references. |
| TypeScript | 5.7.x | Strict mode. |
| TanStack Query | 5.x | `queryClient.setQueryData()` for optimistic updates. `queryClient.invalidateQueries()` for server sync after persist. `queryKey` must match existing patterns. |
| React Router | 7.x | `useParams()` for `recruitmentId`. Route registered in `routes/index.tsx`. |
| Tailwind CSS | 4.x | CSS-first config. CSS Grid for layout. `border`, `bg-gray-*` for panel separators. |
| sonner (via useAppToast) | Latest | Toast with undo action. Auto-dismiss after 3 seconds. Bottom-right position. |
| react-virtuoso | Latest | Already installed by Story 4.1 for CandidateList. No additional install needed. |
| react-pdf | 9.x | Already installed by Story 4.2. PdfViewer consumed as-is. |

### File Structure Requirements

**New files to create:**
```
web/src/features/screening/
  ScreeningLayout.tsx
  ScreeningLayout.test.tsx
  CandidatePanel.tsx
  CandidatePanel.test.tsx
  hooks/
    useResizablePanel.ts
    useResizablePanel.test.ts
    useScreeningSession.ts
    useScreeningSession.test.ts
```

**Existing files to modify:**
```
web/src/routes/index.tsx                     -- Add /recruitments/:recruitmentId/screening route
web/src/mocks/candidateHandlers.ts           -- Extend for optimistic update scenarios
web/src/mocks/fixtures/candidates.ts         -- Add screening session fixtures (mix of screened/unscreened)
```

**Files consumed from prerequisite stories (NOT modified):**
```
web/src/features/screening/PdfViewer.tsx              -- Story 4.2 (center panel)
web/src/features/screening/hooks/usePdfPrefetch.ts    -- Story 4.2 (SAS URL pre-fetching)
web/src/features/screening/OutcomeForm.tsx             -- Story 4.3 (right panel)
web/src/features/screening/hooks/useRecordOutcome.ts   -- Story 4.3 (mutation, bypassed for optimistic flow)
web/src/features/screening/OutcomeHistory.tsx          -- Story 4.3 (outcome history display)
web/src/features/screening/hooks/useOutcomeHistory.ts  -- Story 4.3 (history query)
web/src/features/candidates/CandidateList.tsx          -- Story 4.1 (left panel list)
web/src/features/candidates/hooks/useCandidates.ts     -- Story 4.1 (candidate data)
web/src/features/candidates/hooks/useCandidateById.ts  -- Story 4.1 (single candidate detail)
web/src/lib/api/screening.ts                           -- Story 4.3 (screening API client)
web/src/lib/api/screening.types.ts                     -- Story 4.3 (screening types)
web/src/lib/api/candidates.ts                          -- Story 4.1 (candidate API client)
web/src/lib/api/candidates.types.ts                    -- Story 4.1 (candidate types)
web/src/lib/pdfConfig.ts                               -- Story 4.2 (PDF.js worker config)
web/src/components/EmptyState.tsx                      -- Story 1.4
web/src/components/SkeletonLoader.tsx                  -- Story 1.4
web/src/components/StatusBadge.tsx                     -- Story 1.4
web/src/components/Toast/useAppToast.ts                -- Story 1.4 (useAppToast for undo toast)
```

### Testing Requirements

**Frontend tests (Vitest + Testing Library + MSW):**

useResizablePanel:
- "should initialize with default ratio when no localStorage value"
- "should restore ratio from localStorage on mount"
- "should persist ratio to localStorage on drag end"
- "should enforce minimum left width constraint"
- "should enforce minimum center width constraint"
- "should recompute widths on container resize"

useScreeningSession:
- "should initialize with no selected candidate"
- "should select candidate and update selectedCandidateId"
- "should optimistically update candidate status on outcome recorded"
- "should show undo toast after recording outcome"
- "should auto-advance to next unscreened candidate after 300ms delay"
- "should undo outcome and restore previous state when undo clicked"
- "should cancel API persist when undo is triggered within 3 seconds"
- "should persist outcome via API after 3 seconds without undo"
- "should rollback cache on API failure after 3 seconds"
- "should increment session screened count on outcome"
- "should decrement session screened count on undo"
- "should wrap to top of list when no unscreened candidates below"
- "should stay on current candidate and indicate completion when all screened"
- "should override auto-advance when user manually selects candidate"
- "should compute total screened count from candidates array"

ScreeningLayout:
- "should render three-panel layout"
- "should show empty states in center and right panels before candidate selection"
- "should load PdfViewer and OutcomeForm when candidate is selected"
- "should update all panels when switching between candidates"
- "should show skeleton loader while data is loading"
- "should display resizable divider with correct aria attributes"
- "should show candidate name and step in right panel header"

CandidatePanel:
- "should display total screening progress"
- "should display session screening progress"
- "should highlight selected candidate in list"
- "should show completion banner when all candidates screened"
- "should call onCandidateSelect when candidate is clicked"

### Previous Story Intelligence

**From Story 4.1 (Candidate List & Search/Filter):**
- `CandidateList.tsx` exists with search/filter/pagination. This story wraps it in `CandidatePanel` for the screening layout.
- `useCandidates` hook with `{ recruitmentId, page, search, stepId, outcomeStatus }` params. Query key: `['candidates', recruitmentId, { page, search, stepId, outcomeStatus }]`
- `CandidateResponse` has `currentWorkflowStepId`, `currentWorkflowStepName`, `currentOutcomeStatus` fields -- used for screening progress calculation and auto-advance logic.
- `useCandidateById(recruitmentId, candidateId)` hook available for detailed candidate data.
- react-virtuoso already installed for virtualized list rendering.
- `useDebounce` hook available in `web/src/hooks/useDebounce.ts`.

**From Story 4.2 (PDF Viewing & Download):**
- `PdfViewer.tsx` at `web/src/features/screening/PdfViewer.tsx` with props: `sasUrl`, `candidateName`, `isRecruitmentActive`, `onUploadClick?`. Consumed directly in center panel.
- `usePdfPrefetch.ts` at `web/src/features/screening/hooks/usePdfPrefetch.ts` with `getSasUrl(candidateId)` and `isExpired(candidateId)`. Accepts `(recruitmentId, candidates, currentIndex)`.
- `CandidateResponse.sasUrl` -- nullable SAS URL populated by batch query. Falls back to `usePdfPrefetch.getSasUrl()`.
- `pdfConfig.ts` at `web/src/lib/pdfConfig.ts` -- already imported in `main.tsx`.
- `candidateApi.refreshDocumentSas(recruitmentId, candidateId)` available for SAS refresh.

**From Story 4.3 (Outcome Recording & Workflow Enforcement):**
- `OutcomeForm.tsx` at `web/src/features/screening/OutcomeForm.tsx` with props: `recruitmentId`, `candidateId`, `currentStepId`, `currentStepName`, `existingOutcome`, `isClosed`, `onOutcomeRecorded` callback.
- `onOutcomeRecorded` callback receives `OutcomeResultDto` with `newCurrentStepId` and `isCompleted` -- used by `useScreeningSession.handleOutcomeRecorded()` for auto-advance decisions.
- `useRecordOutcome.ts` mutation hook at `web/src/features/screening/hooks/useRecordOutcome.ts` -- **NOTE: Story 4.4's optimistic flow bypasses this hook's direct mutation**. Instead, `useScreeningSession` handles the delayed API call itself to support the 3-second undo window. The `OutcomeForm` should still call `onOutcomeRecorded` but NOT use `useRecordOutcome` directly when in the screening layout context.
- `screeningApi.recordOutcome()` at `web/src/lib/api/screening.ts` -- called directly by `useScreeningSession` after the 3-second undo window.
- Query key for invalidation: `['screening', 'history', candidateId]` and `['candidates', recruitmentId]`.
- MSW handlers at `web/src/mocks/screeningHandlers.ts` -- POST outcome and GET history.

**From Story 1.4 (Shared UI Components):**
- `EmptyState` component for empty panel states.
- `SkeletonLoader` for loading states (never "Loading..." text).
- `StatusBadge` for outcome status display.
- `useAppToast()` for toast notifications with undo action.
- `PaginationControls` for paginated lists.

**From Story 1.2 (SSO Auth):**
- `ProtectedRoute` wraps screening route.
- `useAuth()` for user identity.

**From Story 2.1-2.5 (Recruitment CRUD):**
- `useRecruitment(recruitmentId)` hook provides recruitment data including `status` and `steps`.
- `recruitment.status === 'Closed'` drives `isClosed` prop for OutcomeForm.

### Git Intelligence

**Commit convention:** `feat(story):` for features, `fix(story):` for fixes. Granular commits per component/task.

**Suggested commit sequence:**
1. `feat(4.4): add useResizablePanel hook with localStorage persistence + tests`
2. `feat(4.4): add useScreeningSession hook with optimistic updates, undo, auto-advance + tests`
3. `feat(4.4): add CandidatePanel component with progress header + tests`
4. `feat(4.4): add ScreeningLayout with CSS Grid three-panel layout + tests`
5. `feat(4.4): add screening route and navigation`
6. `feat(4.4): update MSW handlers and fixtures for screening session`

### Latest Tech Information

- **React 19.2:** `useRef` for mutable refs that persist across renders without causing re-renders (timer IDs, pending outcome state). `useCallback` with dependency arrays for stable function references passed as props.
- **TanStack Query 5.90.x:** `queryClient.setQueryData()` for synchronous cache updates (optimistic UI). The updater function receives the old data and returns the new data. `queryClient.invalidateQueries()` triggers background refetch. `queryKey` array format matches existing patterns.
- **ResizeObserver API:** Supported in all modern browsers (Edge/Chrome). No polyfill needed for desktop-first app. Used in `useResizablePanel` to track container width changes.
- **localStorage:** Synchronous API. Used for persisting panel resize ratio. Key format: `screening-panel-ratio-{recruitmentId}`.
- **sonner (via useAppToast):** The existing toast system supports action buttons. The `action` prop accepts `{ label: string, onClick: () => void }`. Duration is configurable. Bottom-right position is the default.
- **CSS Grid vs Flexbox:** The architecture says "CSS Grid" but the actual layout is better served by flexbox with explicit widths from `useResizablePanel`. This is because the left/center panel widths are dynamically computed from the drag state, which maps naturally to `flex-shrink: 0` + explicit pixel widths. The right panel is fixed at 300px. This is functionally equivalent to `grid-template-columns: ${leftWidth}px ${dividerWidth}px ${centerWidth}px 300px`.

### Project Structure Notes

- `ScreeningLayout.tsx` is the main page component in `features/screening/`. It is the composition root for all three panels.
- `CandidatePanel.tsx` is in `features/screening/` (not `features/candidates/`) because it is a screening-specific wrapper around `CandidateList`. It adds progress tracking and selection highlighting.
- `useResizablePanel.ts` and `useScreeningSession.ts` are in `features/screening/hooks/` alongside `usePdfPrefetch.ts` and `useKeyboardNavigation.ts` (Story 4.5).
- Test files co-locate with source: `ScreeningLayout.test.tsx` next to `ScreeningLayout.tsx`.
- The screening route is registered in `routes/index.tsx` under `/recruitments/:recruitmentId/screening`.
- `CandidateList` from Story 4.1 needs a `selectedId` prop and `onSelect` callback for integration with the screening layout. If Story 4.1's implementation does not include these props, Task 3 will need to add them as a minor extension.

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-4-screening-outcome-recording.md` -- Story 4.4 acceptance criteria, FR mapping, technical notes]
- [Source: `_bmad-output/planning-artifacts/architecture.md` -- Frontend Architectural Constraints: Batch Screening (PDF pre-fetching, client-side state isolation, focus management contract, optimistic outcome recording)]
- [Source: `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` -- Batch Screening Architecture, State Management (TanStack Query + component-local + Context), Folder Structure]
- [Source: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` -- Loading States (skeleton, optimistic, inline), Empty State Pattern, Toast Notifications, Animation Accessibility]
- [Source: `_bmad-output/planning-artifacts/architecture/project-structure.md` -- screening/ folder structure (ScreeningLayout, CandidatePanel, hooks), Component Boundaries (isolated re-renders), useScreeningSession coordination]
- [Source: `_bmad-output/planning-artifacts/architecture/testing-standards.md` -- Vitest + Testing Library + MSW, frontend test naming convention, pragmatic TDD modes]
- [Source: `_bmad-output/implementation-artifacts/4-1-candidate-list-search-filter.md` -- CandidateList.tsx, useCandidates hook, CandidateResponse type with currentWorkflowStep fields]
- [Source: `_bmad-output/implementation-artifacts/4-2-pdf-viewing-download.md` -- PdfViewer.tsx, usePdfPrefetch hook, CandidateResponse.sasUrl, pdfConfig.ts]
- [Source: `_bmad-output/implementation-artifacts/4-3-outcome-recording-workflow-enforcement.md` -- OutcomeForm.tsx, useRecordOutcome hook, screeningApi, OutcomeResultDto with newCurrentStepId/isCompleted]
- [Source: `docs/testing-pragmatic-tdd.md` -- Testing policy, mode declarations]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

### Completion Notes List

### File List
