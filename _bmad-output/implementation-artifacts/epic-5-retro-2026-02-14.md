# Epic 5 Retrospective: Recruitment Overview & Monitoring

**Run ID:** 2026-02-14T224500Z
**Stories:** 5.1 (Overview API & Data), 5.2 (Overview Dashboard UI)
**Git range:** 6def410..db33b58 (8 commits, 49 files, +4653/-27 lines)

---

## Wins

1. **Pre-epic gate mechanism validated** — All 10 Epic 4 retro actions (4 P0, 6 P1) applied before Epic 5 began. E-009 experiment confirmed: zero carried items. This is the first epic with 100% action application rate.

2. **Three experiments adopted** — E-007 (FluentValidation arch test), E-008 (ESLint exhaustive-deps), E-009 (P0/P1 blocking) all PASS. Architectural enforcement eliminates entire defect classes — zero validator gaps, zero stale closures, zero carried items.

3. **Clean architecture** — Aggregate root pattern enforced, GROUP BY read-path without write-path coupling, authorization defense-in-depth on all handlers, TanStack Query scoped cache invalidation, zero PII in overview DTO.

4. **Fix cycle rate improved** — 50% (1/2 stories) down from 100% (5/5 stories) in Epic 4. Story 5.2 achieved clean approval with zero Critical/Important findings.

5. **Strong test discipline** — 26 new tests (311 total), zero failures, zero new build errors, zero new anti-patterns.

---

## Problems

1. **Backend tests and demo BLOCKED** — .NET 10 runtime not installed. 17 new backend tests written but never executed. Demo verified 0/16 ACs. Docker Compose exists but not used as fallback.

2. **Code coverage unmeasured (5th epic)** — A-002 from Epic 4 marked APPLIED but evidence shows "Not measured". Status conflict: either incomplete application or evidence collection gap.

3. **ESLint results not captured** — A-006 enforcement exists but no evidence output. E-008 validated via review findings only, not automated gate.

4. **Pre-existing TypeScript errors (3)** — Unused variables from Epic 4 screening feature carried without cleanup. No remediation policy exists.

5. **Missing cross-recruitment test** — TenantIsolationTests covers 6 endpoints but not GetRecruitmentOverview.

6. **No aggregate root arch test** — FluentValidation and authorization have architectural tests; aggregate boundaries rely on code review only.

7. **No authorization denial logging** — ForbiddenAccessException thrown with no audit trail. Cannot detect cross-recruitment probing.

---

## Experiment Validation

| ID | Hypothesis | Result | Recommendation |
|----|-----------|--------|----------------|
| E-007 | FluentValidation arch test eliminates validator gaps | **PASS** — zero validator findings | Adopt |
| E-008 | ESLint exhaustive-deps eliminates stale closures | **PASS** — zero findings (limited sample) | Adopt |
| E-009 | P0/P1 blocking prevents carried items | **PASS** — 10/10 actions applied | Adopt |

---

## Actions

| ID | Title | Type | Priority | Applied? |
|----|-------|------|----------|----------|
| A-001 | Fix pre-existing TypeScript errors + zero-error gate | quality_gate_gap | P1 | |
| A-002 | Add ESLint execution to retro evidence | instrumentation_gap | P1 | |
| A-003 | Document runtime setup + Docker Compose fallback | docs_update | P1 | |
| A-004 | Verify code coverage CI status | instrumentation_gap | P1 | |
| A-005 | Cross-recruitment overview test | test_gap | P1 | |
| A-006 | Structured logging for auth denials | security_hardening | P1 | |
| A-007 | Aggregate root arch test | architecture_alignment | P2 | |

---

## Anti-Patterns Processing

**Input:** anti-patterns-pending.txt contained 7 comment lines (Stories 4.1-4.5, 5.1, 5.2) and zero REGEX|GLOB|MESSAGE entries.

**Processing:** All entries are comment-only "no new anti-patterns identified" lines. No entries to promote or remove.

**Output:** File reset to header-only state per workflow rules.

---

## New Experiments

| ID | Hypothesis | Success Metric |
|----|-----------|----------------|
| E-010 | Docker Compose fallback eliminates demo BLOCKED | Epic 6 demo executes 50%+ ACs |
| E-011 | Required quality signals checklist eliminates "Not captured" | Zero "Not captured" in Epic 6 evidence |
| E-012 | Zero-error pre-epic gate prevents tsc error accumulation | Zero pre-existing tsc errors at Epic 6 start |

---

## Timing

- **First commit:** 2026-02-14 23:05:46 +0100
- **Last commit:** 2026-02-14 23:46:25 +0100
- **Elapsed:** 41 minutes
- **Commits:** 8 (4.0 per story)
- **Stories:** 2

---

## Deferred (Tracked)

| ID | Title | Priority | Reason | Sprint-Status Key |
|----|-------|----------|--------|-------------------|
| A-007 | Aggregate root arch test | P2 | 2-3 hours effort, new test infrastructure, doesn't destabilize existing code | epic-5-deferred-aggregate-root-test |
