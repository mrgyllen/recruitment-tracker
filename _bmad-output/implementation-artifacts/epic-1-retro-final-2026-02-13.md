# Retrospective: Epic 1 Final (Stories 1.4-1.5)
**Run ID:** 2026-02-13T220300Z
**Type:** Synthesis retro (stories 1.4-1.5 + validation of previous retro 2026-02-13T002800Z)
**Scope:** Shared UI Components & Design Tokens (1.4), App Shell & Empty State Landing (1.5)
**Lenses:** Delivery, QA, Architecture, Docs, Security

---

## Previous Retro Validation (2026-02-13T002800Z)

| ID | Title | Status |
|----|-------|--------|
| A1 | Remove AddDefaultIdentity | Applied |
| A2 | Document Fluent API for domain events | Applied |
| A3 | Add template cleanup checklist | Applied |
| A4 | Document middleware pipeline ordering | Applied |
| A5 | Enforce Dev Agent Record completion | Applied |
| A6 | Consolidate IUser/ICurrentUserService | **Still deferred** (backend, not in scope for 1.4-1.5) |
| A7 | Promote anti-patterns to permanent | Applied |
| A8 | Fix Guid.ToString() type mismatch | **Still deferred** (backend) |
| A9 | Add domain event payload verification tests | **Still deferred** (backend) |
| A10 | Document CORS policy | **Still deferred** (backend) |

**Assessment:** All applied items verified. 4 deferred items are backend-focused — stories 1.4-1.5 were frontend-only. Carry forward to Epic 2.

---

## Wins

1. **High velocity with low rework** — 27 commits, 10K+ lines, 90 tests, only 14% fix commits. Review findings addressed cleanly in dedicated fix commits. (Delivery)

2. **Accessibility built-in from foundation** — vitest-axe integrated, prefers-reduced-motion guards, skip-to-content link, ARIA labels on StatusBadge, ViewportGuard role="alert". WCAG compliance designed in, not bolted on. (QA, Architecture)

3. **Review process caught critical auth bug** — C1 (ProtectedRoute navigating to non-existent /login) caught before merge. Would have broken production auth flow entirely. (Security, QA)

4. **Clean component architecture** — Custom components average 45 LOC, feature-based folder structure, shared components in components/, shadcn/ui in ui/ subdirectory. Zero coupling violations. (Architecture, Delivery)

5. **Dev Agent Record quality improved** — Both stories have populated records with testing mode rationale, key decisions, and file lists. Previous retro action A5 is working. (Docs)

---

## Action Items

### P0 — Apply Immediately

| ID | Type | Title | Lenses |
|----|------|-------|--------|
| B1 | hook_update | Promote 3 anti-patterns from pending to permanent | QA, Delivery |

### P1 — Apply Now (Low-Risk Edits)

| ID | Type | Title | Lenses |
|----|------|-------|--------|
| B2 | docs_update | Document ProtectedRoute auth guard pattern in frontend-architecture.md | Docs, Security |
| B3 | docs_update | Document Ghost button deferral in patterns-frontend.md | Docs |
| B4 | guideline_gap | Add prefers-reduced-motion animation requirement to patterns-frontend.md | Docs, QA |
| B5 | docs_update | Document useAppToast naming as intentional pattern | Docs |

### P2 — Fix Now If Small, Track If Large

| ID | Type | Title | Effort | Decision |
|----|------|-------|--------|----------|
| B6 | refactor | Remove dead @custom-variant dark from index.css | Trivial | Fix now |
| B7 | quality_gate_gap | Add code coverage thresholds to CI | Medium | Track |
| B8 | test_gap | Add ProtectedRoute edge case tests | Small | Track |
| B9 | security_hardening | Add CSP and security headers | Medium | Track |
| B10 | security_hardening | Complete MSAL production auth | Large | Track |
| B11 | observability | Add bundle size budget tracking | Medium | Track |

---

## Recurring Patterns

| Pattern | Stories | Root Cause | Action |
|---------|---------|------------|--------|
| Missing reduced-motion guard | 1.4 (I3) | No architecture doc requirement | B4: Add to patterns-frontend.md |
| Test assertions too shallow | 1.4 (I1, I2, M4) | Tests verify rendering, not behavior/config | Anti-pattern promoted (B1) |
| Story task checkboxes unchecked | 1.4 (M3), 1.5 (M2) | Dev Agent not marking tasks complete | Low severity, noted |
| Auth redirect pattern confusion | 1.5 (C1) | Architecture showed Navigate pattern, not login() callback | B2: Document correct patterns |

---

## Missing Evidence / Instrumentation Gaps

- Code coverage percentage (no vitest coverage configured)
- Bundle size per-component breakdown
- Review finding resolution time (no timestamps)
- Manual accessibility testing logs (screen reader, keyboard)
- CI test execution evidence

---

## Deferred (Tracked)

Items tracked in sprint-status.yaml under epic-1:

| ID | Title | sprint-status key |
|----|-------|------------------|
| B7 | Code coverage thresholds | epic-1-deferred-coverage |
| B8 | ProtectedRoute edge case tests | epic-1-deferred-protectedroute-tests |
| B9 | CSP and security headers | epic-1-deferred-csp-headers |
| B10 | Complete MSAL production auth | epic-1-deferred-msal-auth |
| B11 | Bundle size budget tracking | epic-1-deferred-bundle-budget |
| A6 | Consolidate IUser/ICurrentUserService | (carried from previous retro) |
| A8 | Fix Guid.ToString() type mismatch | (carried from previous retro) |
| A9 | Domain event payload verification tests | (carried from previous retro) |
| A10 | CORS policy documentation | (carried from previous retro) |
