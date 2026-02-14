# Retrospective: Epic 2 — Recruitment & Team Setup
**Run ID:** 2026-02-14T013000Z
**Scope:** Stories 2.1-2.5 (Create Recruitment, List & Navigation, Edit & Manage Steps, Team Membership, Close Recruitment)
**Lenses:** Delivery, QA, Architecture, Docs, Security, Product
**Previous retros:** 2026-02-13T002800Z (Epic 1 stories 1.1-1.3), 2026-02-13T220300Z (Epic 1 stories 1.4-1.5)

---

## Previous Retro Validation

**All 9 deferred items from both Epic 1 retros have been resolved:**

| ID | Title | Resolution |
|----|-------|-----------|
| A6 | Consolidate IUser/ICurrentUserService | Resolved in commit 2d91064 — ICurrentUserService deleted, TenantContext uses IUser |
| A8 | Fix Guid.ToString() type mismatch | Resolved — ITenantContext.UserGuid property added (Guid? type) |
| A9 | Domain event payload verification tests | Resolved — tests now verify event payloads |
| A10 | CORS policy documentation | Resolved — documented in patterns-backend.md |
| B7 | Code coverage thresholds | Resolved (sprint-status: done) |
| B8 | ProtectedRoute edge case tests | Resolved (sprint-status: done) |
| B9 | CSP and security headers | Resolved — SecurityHeadersMiddleware.cs added |
| B10 | Complete MSAL production auth | Resolved — closed as deployment-specific config |
| B11 | Bundle size budget tracking | Resolved (sprint-status: done) |

**Zero carried items remain from Epic 1.**

---

## Wins

1. **All 30+ acceptance criteria delivered with zero scope cuts** — Complete create/list/edit/team/close workflow implemented across 5 full-stack stories. FR4-8, FR11-13, FR56-62 fulfilled. (Product, Delivery)

2. **DDD aggregate root pattern strictly followed** — All mutations go through Recruitment aggregate root with EnsureNotClosed() guards. Domain exception hierarchy (RecruitmentClosedException, StepHasOutcomesException, DuplicateStepNameException, DomainRuleViolationException) maps cleanly to HTTP status codes. (Architecture, Delivery)

3. **All 9 deferred items from two previous retros resolved** — First commit (2d91064) addressed all carried items. IUser/ICurrentUserService consolidated, Guid type mismatch fixed, security headers added, coverage thresholds set. Process commitment to clearing debt validated. (Delivery, Docs)

4. **Review process caught critical security gap** — Story 2.3 C1 found all 4 command handlers missing ITenantContext membership checks. Fixed in commit f1dee45 before approval, preventing data isolation breach. (Security, QA)

5. **High implementation velocity with consistent quality** — 42 commits, 10K+ lines, 133 files across full stack. Consistent commit conventions. Implementation plans for complex stories. 2/5 stories passed clean on first review. (Delivery)

---

## Action Items

### P0 — Apply Immediately

| ID | Type | Title | Owner |
|----|------|-------|-------|
| A-001 | docs_update | Document command handler authorization pattern in patterns-backend.md | Security |
| A-002 | process_change | Add authorization verification to review checklist in team-workflow.md | Security |

### P1 — Apply Now

| ID | Type | Title | Owner |
|----|------|-------|-------|
| A-003 | docs_update | Document endpoint registration convention (EndpointGroupBase) in api-patterns.md | Architecture |
| A-004 | docs_update | Document transient _stepsWithOutcomes pattern in patterns-backend.md | Architecture |
| A-005 | guideline_gap | Add SkeletonLoader requirement and anti-pattern for 'Loading...' text | Product |

### P2 — Fix Now If Small

| ID | Type | Title | Decision |
|----|------|-------|----------|
| A-006 | refactor | Standardize TeamEndpoints to use EndpointGroupBase | Track (>1 story point, requires touching endpoint + registration + tests) |
| A-007 | refactor | Fix CloseRecruitment to return 204 No Content | Fix now (trivial) |
| A-008 | test_gap | Add integration tests for CustomExceptionHandler mappings | Track (new test infrastructure needed) |

---

## Recurring Patterns

| Pattern | Stories | Root Cause | Action |
|---------|---------|------------|--------|
| Missing command handler authorization | 2.3 (all 4 handlers) | Pattern undocumented in architecture docs | A-001, A-002 |
| Endpoint registration inconsistency | 2.4 (TeamEndpoints) | No documented canonical pattern | A-003, A-006 |
| 'Loading...' text instead of SkeletonLoader | 2.1, 2.4 | No guideline requiring SkeletonLoader | A-005 |
| Fix cycles on first-of-kind stories | 2.1, 2.3, 2.4 | Patterns not documented after first implementation | E-003 experiment |

---

## Experiments

| ID | Hypothesis | Method | Success Metric |
|----|-----------|--------|----------------|
| E-001 | Mandatory 'Authorization Check' section in implementation plans prevents missing ITenantContext checks | Update writing-plans skill template | Zero authorization Critical findings in Epic 3 |
| E-002 | Pre-review anti-pattern scanning catches minor findings before review | Enhance verification-before-completion skill | Minor findings per story drop to 1 or fewer |
| E-003 | Pattern establishment phase after first-of-kind stories reduces fix cycles | Document established patterns before next story | Fix cycle rate drops below 40% in Epic 3 |

---

## Missing Evidence / Instrumentation Gaps

- CI pipeline test execution logs (tests compile but no green CI evidence)
- Code coverage metrics (thresholds configured but no measurement data)
- Integration test results for API endpoints
- Dependency vulnerability scan (npm audit, dotnet list --vulnerable)
- Audit log consumption — domain events raised but storage/consumption unknown
- Accessibility testing for new components

---

## Deferred (Tracked)

| ID | Title | Reason | sprint-status key |
|----|-------|--------|------------------|
| A-006 | Standardize TeamEndpoints to EndpointGroupBase | Refactor >1 story point, touches endpoint registration + program startup + tests | epic-2-deferred-team-endpoints |
| A-008 | Integration tests for CustomExceptionHandler | Requires new test infrastructure (WebApplicationFactory setup) | epic-2-deferred-exception-integration-tests |

**Total deferred: 2 items** (within the 0-2 target)
