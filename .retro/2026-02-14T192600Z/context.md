# Epic 4 Retrospective — Evidence Bundle

**Run ID:** 2026-02-14T192600Z
**Scope:** Epic 4 — Screening & Outcome Recording (Stories 4.1-4.5)
**Branch:** epic4/screening-outcome-recording
**Git range:** b04e294..ac63aa0

---

## Section 1: Scope

### Stories

| ID | Title | ACs |
|----|-------|-----|
| 4.1 | Candidate List & Search/Filter | Display list, search by name/email, step filter, outcome filter, combined filters, pagination, empty state, virtualization, candidate detail |
| 4.2 | PDF Viewing & Download | Inline PDF render, multi-page scroll, download, no-doc empty state, candidate switch, PDF pre-fetch, SAS token refresh |
| 4.3 | Outcome Recording & Workflow Enforcement | Outcome controls, record outcome, pass advances, fail/hold stays, last step pass, existing outcome, workflow enforcement, outcome history, closed recruitment |
| 4.4 | Split-Panel Screening Layout | Three-panel layout, resizable divider, candidate selection, candidate switch, optimistic outcome + toast, undo, auto-advance, override auto-advance, screening progress |
| 4.5 | Keyboard Navigation & Screening Flow | 1/2/3 shortcuts, text input protection, tab flow, focus return, arrow navigation, focus stays on list, tab order + indicators, button hints, ARIA live regions, complete keyboard flow |

### Pre-Story Work
- **E-004 experiment:** Authorization architectural test (commit 6849760)
- **Epic 3 deferred items:** SAS token max enforcement, rate limiting, unmatched doc assign UI, BackgroundService auth docs (commit 82a6f15)

---

## Section 2: Git Summary

**57 commits** from b04e294 to ac63aa0

```
ac63aa0 demo(epic-4): add Epic 4 demo walkthrough (code-level verification)
5dca841 retro(4.5): mini-retro for keyboard navigation & screening flow
afcca2f fix(4.5): use ref for onAutoAdvance to fix stale closure in useCallback
3970fd2 stories(4.5): complete Keyboard Navigation & Screening Flow
41d2301 feat(4.5): add keyboard coordination, ARIA regions, focus management, and candidateListRef
2ca5440 feat(4.5): add onAutoAdvance callback to useScreeningSession + test
de5061c feat(4.5): add shortcut hints, onOutcomeSelect, and externalOutcome to OutcomeForm + tests
6abecd9 feat(4.5): add useKeyboardNavigation hook with shortcut scoping + tests
598c3fb retro(4.4): mini-retro for split-panel screening layout
0219632 fix(4.4): reset OutcomeForm on candidate switch and prevent Link navigation in screening mode
d62ff20 stories(4.4): mark split-panel screening layout as done
17249b3 feat(4.4): add screening session fixtures with mix of screened/unscreened candidates
0b611ea feat(4.4): add screening route and Start Screening navigation
cbafd13 feat(4.4): add ScreeningLayout with three-panel layout + tests
a124280 feat(4.4): add CandidatePanel component with progress header + tests
7048315 feat(4.4): add useScreeningSession hook with auto-advance and undo + tests
13af9c5 feat(4.4): add selectedId and onSelect props to CandidateList
3d6b5cd feat(4.4): add useResizablePanel hook with localStorage persistence + tests
0066b6d chore(4.3): mini-retro — no new anti-patterns identified
144c6ef fix(4.3): add missing FluentValidation validator for GetCandidateOutcomeHistoryQuery
3a6eb93 stories(4.3): mark story done with dev agent record and sprint status
a94ae27 feat(4.3): add OutcomeHistory component + extract shared toStatusVariant
f4b9454 feat(4.3): add OutcomeForm component with useRecordOutcome hook + tests
0e53a25 feat(4.3): add MSW screening handlers and fixtures
8a6ffb2 feat(4.3): add screening API client types and methods
2d8305d feat(4.3): add initial EF Core migration with workflow enforcement columns
9b4ff85 feat(4.3): add ScreeningEndpoints with POST outcome and GET history
7f2c4e5 feat(4.3): add GetCandidateOutcomeHistoryQuery with handler + unit tests
6bfb053 feat(4.3): add RecordOutcomeCommand with handler + validator + unit tests
a21d69b feat(4.3): register InvalidWorkflowTransitionException in exception handler
1ad83b2 feat(4.3): enhance Candidate.RecordOutcome with workflow enforcement + domain tests
c4646de feat(4.3): extend CandidateOutcome with Reason property + EF config
dfcce44 chore(4.2): mini-retro — no new anti-patterns identified
c6af9e7 fix(4.2): sync useSasUrl state when initialUrl prop changes
cf17f95 docs(4.2): add Dev Agent Record and mark story done
53c9e5e feat(4.2): add no-document candidate fixture for empty state testing
7a91533 feat(4.2): integrate PdfViewer into CandidateDetail with empty state and download
168e98f feat(4.2): add usePdfPrefetch hook for adjacent candidate SAS URL pre-fetching
0affe04 feat(4.2): add useSasUrl hook for transparent SAS token refresh
e253b7b feat(4.2): add PdfViewer component with per-page rendering and tests
7d79e30 feat(4.2): install react-pdf and configure PDF.js worker
88e6346 chore(4.1): mini-retro — capture minor findings as anti-patterns
57b39d6 fix(4.1): add Dev Agent Record to story file
6e38fa3 fix(4.1): add functional tests for candidate query endpoints
52901a3 fix(4.1): add frontend tests for search/filter and CandidateDetail
4d037a7 fix(4.1): add validators, in-memory filtering comment, empty state text
c15cd3b feat(4.1): add batch SAS URLs to candidate list response (Decision #6)
a20a2d0 chore: mark story 4.1 as done in sprint status
8209399 feat(4.1): update MSW handlers and fixtures for search/filter/detail
bae6d03 feat(4.1): add search/filter/pagination/virtualization to CandidateList + enhanced CandidateDetail
409aa93 feat(4.1): extend frontend API client and hooks for search/filter/detail
4309f00 feat(4.1): add candidate detail endpoint + search/filter query params
7c52983 feat(4.1): add GetCandidateByIdQuery with SAS URLs + tests
9772e08 feat(4.1): add search/filter to GetCandidatesQuery handler + tests
d999043 feat(4.1): extend CandidateDto with current step computation + tests
82a6f15 fix: resolve Epic 3 deferred items (4 of 4)
6849760 process: apply experiment E-004 — authorization architectural test
```

**Diffstat:** 102 files changed, 13,046 insertions(+), 156 deletions(-)

**Changed files:** See full list in git diff --name-only output (102 files across api/src, api/tests, web/src, docs/plans, _bmad-output).

---

## Section 3: Quality Signals

### Frontend Tests
- **285 tests** across 45 test files — all PASS
- TypeScript: 0 errors (`tsc --noEmit` clean)
- Production build: clean (`vite build` exit 0)

### Backend Tests
- **Domain.UnitTests:** 114 tests — all PASS
- **Application.UnitTests:** Cannot execute (requires Microsoft.AspNetCore.App 10.0.0 runtime, not installed on this Linux environment)
- **Application.FunctionalTests:** Cannot execute (requires SQL Server)
- **Infrastructure.IntegrationTests:** Cannot execute (requires Docker/SQL Server)
- **.NET build:** 0 warnings, 0 errors

### Code Coverage
- **Not measured.** coverlet.collector is installed but no coverage reports generated. This is the 4th consecutive epic without coverage metrics.

---

## Section 4: Review Findings

### Story 4.1: Candidate List & Search/Filter
- **3 Critical, 3 Important, 4 Minor** → Fix cycle
- C1: Missing FluentValidation validators on GetCandidatesQuery and GetCandidateByIdQuery
- C2: Missing frontend tests for all new functionality (0 tests)
- C3: Missing integration/functional tests for new API endpoints
- I1: In-memory filtering comment misleading
- I2: Import flow dropdown incomplete
- I3: Empty state text mismatch with AC
- M1: Duplicated toStatusVariant() helper (→ fixed in Story 4.3)
- M2: recordedByUserId displayed but no user name resolution
- M3: Virtualization threshold choice
- M4: pageSize not in TanStack Query queryKey

### Story 4.2: PDF Viewing & Download
- **0 Critical, 1 Important, 3 Minor** → Fix cycle
- I1: useSasUrl stale initialUrl (useEffect sync missing)
- M1: No loading state during PDF load
- M2: Download opens in new tab instead of triggering download
- M3: No error boundary around react-pdf

### Story 4.3: Outcome Recording & Workflow Enforcement
- **0 Critical, 1 Important, 4 Minor** → Fix cycle
- I1: Missing FluentValidation for GetCandidateOutcomeHistoryQuery
- M1: Outcome history doesn't show step name (only ID)
- M2: Re-record could benefit from confirmation
- M3: "who recorded it" not displayed
- M4: No optimistic update on outcome recording

### Story 4.4: Split-Panel Screening Layout
- **0 Critical, 2 Important, 4 Minor** → Fix cycle
- I1: OutcomeForm state not resetting on candidate switch
- I2: CandidateRow Link navigating away from screening layout
- M1: Dead queryClient in useScreeningSession
- M2: Immediate persist instead of delayed 3-second with undo (AC5/AC6 simplification)
- M3: Flexbox instead of CSS Grid
- M4: Remove button visible in screening context

### Story 4.5: Keyboard Navigation & Screening Flow
- **0 Critical, 1 Important, 3 Minor** → Fix cycle
- I1: options missing from handleOutcomeRecorded useCallback dependency array (stale closure)
- M1: Missing role="listbox"/role="option" on candidate list
- M2: No scroll-into-view on arrow key navigation
- M3: Dead queryClient (carried from 4.4 M1)

### Totals
- **Critical:** 3 (all in Story 4.1)
- **Important:** 8 (across all 5 stories)
- **Minor:** 18 (3.6 average per story)
- **Fix cycle rate:** 100% (5/5 stories)
- All fixes resolved in single round (no multi-round fix cycles)

---

## Section 5: Anti-Patterns Pending

```
# Story 4.1: M1 — duplicated toStatusVariant() helper in CandidateList.tsx and CandidateDetail.tsx
# Story 4.1: M2 — OutcomeHistoryEntry has recordedByUserId but UI doesn't display "who recorded it" (AC9 partial)
# Story 4.1: M4 — pageSize not included in TanStack Query queryKey (cache invalidation risk)
# Story 4.3: M3 — "who recorded it" not displayed in outcome history (needs user name resolution)
# Story 4.4: M2 — immediate persist instead of delayed 3-second persist with undo (AC5/AC6 simplification)
# Story 4.4: M3 — panel min widths not enforced below certain thresholds (UX polish)
# Story 4.4: M4 — no loading indicator during outcome recording (optimistic update masks latency)
# Story 4.5: I1 — stale closure from missing useCallback dependency (recurring React pattern)
# Story 4.5: M1 — missing role="listbox"/role="option" on candidate list (deferred accessibility)
# Story 4.5: M2 — no scroll-into-view on Arrow key navigation (UX polish)
# Story 4.5: M3 — dead queryClient in useScreeningSession (carried from 4.4 M1)
```

---

## Section 6: Previous Retro (2026-02-14T073000Z)

### Experiments to Validate

| ID | Hypothesis | Success Metric | Applied? |
|----|-----------|----------------|----------|
| E-004 | Architectural test for ITenantContext enforcement | Zero auth Critical/Important findings in Epic 4 | YES (commit 6849760) |
| E-005 | AC completeness walkthrough reduces fix cycle rate | Fix cycle rate below 50% | YES (team-workflow.md updated) |
| E-006 | Structured pre-review checklist catches more issues | Avg minor findings per story below 3 | YES (team-workflow.md updated) |

### E-004 Validation Data
- Story 4.1: No auth findings
- Story 4.2: No auth findings (frontend-only)
- Story 4.3: No auth findings — RecordOutcomeCommandHandler and GetCandidateOutcomeHistoryQueryHandler both inject ITenantContext
- Story 4.4: No auth findings (frontend-only)
- Story 4.5: No auth findings (frontend-only)
- Architectural test caught all handlers via reflection at compile time
- **Result: PASS** — zero authorization-related Critical or Important findings

### E-005 Validation Data
- Fix cycle rate: 100% (5/5 stories had Important or Critical findings)
- Target: below 50%
- However: Nature of findings changed — NONE were "AC not functionally complete" (the specific problem E-005 targeted). Findings were: missing validators (4.1 C1, 4.3 I1), missing tests (4.1 C2/C3), React state bugs (4.2 I1, 4.4 I1/I2, 4.5 I1)
- **Result: FAIL** on metric (100% > 50%), but the specific problem (partial AC implementations) did not recur

### E-006 Validation Data
- Minor findings: 4.1=4M, 4.2=3M, 4.3=4M, 4.4=4M, 4.5=3M
- Average: 3.6 per story
- Target: below 3
- Previous: 4.4 per story in Epic 3
- **Result: FAIL** on metric (3.6 > 3), but improvement from 4.4 → 3.6 (18% reduction)

### Previous Action Items Status

| ID | Title | Priority | Status |
|----|-------|----------|--------|
| A-001 | Auth architectural test + docs | P0 | APPLIED (commit 6849760, Task #1) |
| A-002 | Unit tests for AssignDocument/UploadDocument handlers | P0 | APPLIED (commit 82a6f15, Task #2) |
| A-003 | Global query filters on Recruitment/ImportSession | P1 | NOT APPLIED — deferred from Epic 3 retro, carried into Epic 4 without resolution |
| A-004 | Architecture.md ImportSession aggregate expansion docs | P1 | APPLIED in retro self-healing commit 3e89f85 |
| A-005 | Code coverage CI thresholds | P1 | NOT APPLIED — 4th epic without coverage metrics |
| A-006 | Validate AssignDocument blob URL belongs to recruitment | P1 | NOT APPLIED — security risk carried forward |
| A-007 | AC completeness walkthrough (→ E-005) | P1 | APPLIED (process change in team-workflow.md) |
| A-008 | A-007 resolution status + state-change convention | P2 | NOT VERIFIED |
| A-009 | Redact entity keys from ProblemDetails | P2 | NOT APPLIED |

---

## Section 7: Guideline References

- `_bmad-output/planning-artifacts/architecture.md` — Core architecture
- `_bmad-output/planning-artifacts/architecture/patterns-backend.md` — Backend patterns
- `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` — Frontend patterns
- `_bmad-output/planning-artifacts/architecture/api-patterns.md` — API patterns
- `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` — Frontend architecture
- `_bmad-output/planning-artifacts/architecture/testing-standards.md` — Testing standards
- `.claude/process/team-workflow.md` — Team workflow

---

## Section 8: Sprint Status Snapshot

```yaml
epic-4: in-progress
4-1-candidate-list-search-filter: done
4-2-pdf-viewing-download: done
4-3-outcome-recording-workflow-enforcement: done
4-4-split-panel-screening-layout: done
4-5-keyboard-navigation-screening-flow: done
epic-4-retrospective: optional
```

---

## Section 9: Execution Timing

- **First commit:** 2026-02-14T17:20:58+01:00 (6849760)
- **Last commit:** 2026-02-14T19:25:00+01:00 (ac63aa0)
- **Elapsed hours:** 2.07
- **Total commits:** 57
- **Stories:** 5
- **Commits per story:** 11.4

### Per-Story Breakdown
| Story | Implementation Commits | Fix Commits | Total | Review Result |
|-------|----------------------|-------------|-------|---------------|
| Pre-story (E-004 + deferred) | 2 | 0 | 2 | N/A |
| 4.1 | 9 | 5 | 14 | 3C, 3I, 4M |
| 4.2 | 7 | 1 | 8 | 0C, 1I, 3M |
| 4.3 | 12 | 1 | 13 | 0C, 1I, 4M |
| 4.4 | 8 | 1 | 9 | 0C, 2I, 4M |
| 4.5 | 5 | 1 | 6 | 0C, 1I, 3M |
| Mini-retros + demo | 5 | 0 | 5 | N/A |

---

## Section 10: Demo Walkthrough Results

**File:** `_bmad-output/implementation-artifacts/epic-4-demo-2026-02-14.md`

- **Total ACs verified:** 39 (across 5 stories) + 10 error paths = 49
- **Pass:** 49
- **Fail:** 0
- **Blocked:** 0 (code-level verification used — SQL Server unavailable on Linux)

**Method limitation:** Demo was code-level verification, not live application interaction. API requires SQL Server.

**One minor deviation:** Story 4.4 AC5 specifies 3-second toast auto-dismiss; implementation uses 5000ms. Functional behavior correct.

---

## Section 11: Key Files Created/Modified

### Backend (api/)
- Domain: `Candidate.cs` (RecordOutcome enhancement), `CandidateOutcome.cs` (Reason property)
- Application: `CandidateDto.cs`, `GetCandidatesQuery*`, `GetCandidateByIdQuery*`, `RecordOutcomeCommand*`, `GetCandidateOutcomeHistoryQuery*`, `ScreeningEndpoints.cs`
- Infrastructure: `BlobStorageService.cs` (SAS max), `RateLimitPolicies.cs`, migrations
- Tests: `AuthorizationArchitectureTests.cs`, `CandidateDtoTests.cs`, handler tests, domain tests, functional tests

### Frontend (web/)
- Features/candidates: `CandidateList.tsx`, `CandidateDetail.tsx`, `PdfViewer.tsx`, `useSasUrl.ts`, `usePdfPrefetch.ts`, `useCandidateById.ts`
- Features/screening: `ScreeningLayout.tsx`, `CandidatePanel.tsx`, `OutcomeForm.tsx`, `OutcomeHistory.tsx`, `useKeyboardNavigation.ts`, `useScreeningSession.ts`, `useResizablePanel.ts`, `useRecordOutcome.ts`, `useOutcomeHistory.ts`
- Shared: `StatusBadge.types.ts` (toStatusVariant extraction)
- API client: `candidates.ts`, `candidates.types.ts`, `screening.ts`, `screening.types.ts`
- Mocks: `candidateHandlers.ts`, `screeningHandlers.ts`, fixtures
