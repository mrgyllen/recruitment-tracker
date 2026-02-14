# Epic 4 Retrospective — Screening & Outcome Recording

**Run ID:** 2026-02-14T192600Z
**Branch:** epic4/screening-outcome-recording
**Git range:** b04e294..ac63aa0
**Stories:** 4.1–4.5

---

## Timing

| Metric | Value |
|--------|-------|
| First commit | 2026-02-14T17:20:58+01:00 |
| Last commit | 2026-02-14T19:25:00+01:00 |
| Elapsed | 2.07 hours |
| Total commits | 57 |
| Stories | 5 |
| Commits/story | 11.4 |

### Per-Story Breakdown

| Story | Impl | Fix | Total | Findings |
|-------|------|-----|-------|----------|
| Pre-story (E-004 + deferred) | 2 | 0 | 2 | N/A |
| 4.1 Candidate List | 9 | 5 | 14 | 3C, 3I, 4M |
| 4.2 PDF Viewing | 7 | 1 | 8 | 0C, 1I, 3M |
| 4.3 Outcome Recording | 12 | 1 | 13 | 0C, 1I, 4M |
| 4.4 Split-Panel Layout | 8 | 1 | 9 | 0C, 2I, 4M |
| 4.5 Keyboard Navigation | 5 | 1 | 6 | 0C, 1I, 3M |
| Mini-retros + demo | 5 | 0 | 5 | N/A |

---

## What Went Well

1. **E-004 eliminated authorization gaps.** Zero auth findings across all 5 stories. AuthorizationArchitectureTests.cs (commit 6849760) catches missing ITenantContext at compile time via reflection — first epic with perfect auth coverage.

2. **Full feature delivery.** 39 acceptance criteria verified PASS + 10 error paths. Keyboard-first screening flow operational with 1/2/3 shortcuts, arrow navigation, focus management, and ARIA live regions.

3. **Single-round fix cycles.** All 5 stories resolved findings in one pass — no multi-round rework despite 100% fix rate.

4. **Clean DDD compliance.** Candidate.RecordOutcome enforces workflow through aggregate root. Feature folder isolation maintained. Global query filters on all 3 aggregates.

5. **Strong test pyramid.** 285 frontend + 114 domain tests, all passing. Zero TypeScript errors. Clean production build.

---

## What Needs Improvement

1. **Code coverage blind spot (4th epic).** coverlet installed but unused. 13,046 new lines with zero coverage visibility. Previous retro A-005 still not applied.

2. **Validator gaps recur.** 3 queries shipped without FluentValidation (4.1 C1, 4.3 I1). Validators treated as polish, not security boundary.

3. **Blob URL validation unresolved (2nd epic).** A-006 from Epic 3 still not applied. AssignDocumentCommandHandler accepts arbitrary blob URLs — cross-recruitment document access vector.

4. **Stale closures recur.** 4.2 I1 (useSasUrl) and 4.5 I1 (useCallback deps) — same React hook pattern. No ESLint enforcement.

5. **P1 items carried without resolution.** A-003, A-005, A-006 from Epic 3 all NOT APPLIED. No blocking mechanism.

6. **100% fix cycle rate.** Every story needed fixes. E-005 eliminated AC incompleteness, but validators/tests/React bugs drove fix cycles.

7. **Oversized plans.** 761–2335 lines per plan. Becoming spec documents, not executable guides.

---

## Root Cause Hypotheses

- **Tooling gap:** E-004 proved architectural tests work. Pattern not extended to validators or coverage.
- **Reactive validation:** Validators added after review, not during TDD. No test-first discipline for input validation.
- **No enforcement teeth:** Retro actions lack blocking mechanism. P1 items can be perpetually deferred.
- **ESLint config gap:** exhaustive-deps available but not enforced at error level.

---

## Experiment Validation

### E-004: Authorization Architectural Test — PASS ✓ → Adopt

Zero auth findings. Compile-time reflection enforcement works. Keep permanently.

### E-005: AC Completeness Walkthrough — FAIL on metric → Modify

Fix cycle rate 100% vs target <50%. But zero AC incompleteness findings (the target problem). Metric misaligned with hypothesis. **Modify:** Change metric to "AC incompleteness findings = 0" (already achieved).

### E-006: Pre-Review Checklist — FAIL on metric → Modify

3.6 minor findings/story vs target <3. Improved from 4.4 (18% reduction). Checklist helps but can't catch design choice minors. **Modify:** Keep, target 3.0 for next epic.

---

## New Experiments

| ID | Hypothesis | How | Metric |
|----|-----------|-----|--------|
| E-007 | Validator architectural test eliminates validator-gap findings (replicating E-004) | Add ValidatorArchitectureTests.cs (A-001) | Zero validator Critical/Important in Epic 5 |
| E-008 | ESLint exhaustive-deps at error level eliminates stale closures | Enable rule + document ref pattern (A-006) | Zero stale closure findings in Epic 5 |
| E-009 | P0/P1 blocking mechanism prevents carried actions | Pre-epic gate in team-workflow.md (A-005) | Zero carried P1 items at Epic 5 retro |

---

## Action Items

### P0 — Apply Immediately

| ID | Title | Type | Owner |
|----|-------|------|-------|
| A-001 | Add FluentValidation architectural test | test_gap | Dev Agent |
| A-002 | Enable code coverage CI with thresholds | quality_gate_gap | Dev Agent |
| A-003 | Enforce blob ownership verification in AssignDocumentCommand | security_hardening | Dev Agent |
| A-004 | Add cross-recruitment isolation tests | test_gap | Dev Agent |
| A-005 | Add P0/P1 action enforcement to retro workflow | process_change | Team Lead |

### P1 — Apply Immediately

| ID | Title | Type | Owner |
|----|-------|------|-------|
| A-006 | Enable ESLint exhaustive-deps + document ref pattern | quality_gate_gap | Dev Agent + Team Lead |
| A-007 | Complete entity key redaction in exception handlers | security_hardening | Dev Agent |
| A-008 | Add Authorization section to writing-plans skill | process_change | Team Lead |
| A-009 | Fix pageSize queryKey + pagination param guideline | guideline_gap | Dev Agent + Team Lead |
| A-010 | Add screening routing table + DTO display value guideline | docs_update | Team Lead |

### Previous Retro Action Status

| ID | Title | Priority | Epic 4 Status |
|----|-------|----------|---------------|
| A-001 | Auth architectural test + docs | P0 | APPLIED (commit 6849760) |
| A-002 | Unit tests for AssignDocument/UploadDocument | P0 | APPLIED (commit 82a6f15) |
| A-003 | Global query filters on Recruitment/ImportSession | P1 | PARTIALLY APPLIED — filters present, tests not verified |
| A-004 | Architecture.md ImportSession expansion | P1 | APPLIED (self-healing commit 3e89f85) |
| A-005 | Code coverage CI thresholds | P1 | NOT APPLIED → Escalated to P0 as current A-002 |
| A-006 | Validate blob URL belongs to recruitment | P1 | NOT APPLIED → Escalated to P0 as current A-003 |
| A-007 | AC completeness walkthrough (E-005) | P1 | APPLIED (team-workflow.md updated) |
| A-008 | State-change endpoint convention | P2 | APPLIED (api-patterns.md updated) |
| A-009 | Redact entity keys from ProblemDetails | P2 | PARTIALLY APPLIED → Current A-007 completes it |

---

## Anti-Patterns Processed

All 11 pending entries from Epic 4 mini-retros resolved:

| Entry | Resolution |
|-------|-----------|
| 4.1 M1: duplicated toStatusVariant | ONE-OFF — fixed in 4.3 (commit a94ae27) |
| 4.1 M2: recordedByUserId no name | PRODUCT GAP — future story for user name resolution |
| 4.1 M4: pageSize not in queryKey | BUG — fixed by A-009 |
| 4.3 M3: "who recorded it" missing | PRODUCT GAP — same as 4.1 M2 |
| 4.4 M2: immediate persist vs delayed | DESIGN CHOICE — document rationale |
| 4.4 M3: panel min widths | UX POLISH — one-off, removed |
| 4.4 M4: no loading indicator | UX POLISH — one-off, removed |
| 4.5 I1: stale closure (useCallback) | RECURRING — promoted to ESLint enforcement (A-006) |
| 4.5 M1: missing listbox roles | ACCESSIBILITY — future story for ARIA semantics |
| 4.5 M2: no scroll-into-view | UX POLISH — one-off, removed |
| 4.5 M3: dead queryClient | RECURRING (2 stories) — covered by ESLint no-unused-vars |

**Promoted to permanent anti-patterns.txt:**
- ESLint exhaustive-deps disable prevention (stale closure enforcement)

---

## Quality Metrics

| Metric | Epic 3 | Epic 4 | Trend |
|--------|--------|--------|-------|
| Stories | 5 | 5 | — |
| Commits | 53 | 57 | +8% |
| Fix cycle rate | 80% | 100% | ↑ worse |
| Critical findings | 2 | 3 | ↑ worse |
| Important findings | 8 | 8 | — |
| Minor findings/story | 4.4 | 3.6 | ↓ better |
| Auth findings | 1 C | 0 | ↓ better |
| Frontend tests | 92 | 285 | +210% |
| Domain tests | 87 | 114 | +31% |
| Elapsed hours | 2.05 | 2.07 | — |
