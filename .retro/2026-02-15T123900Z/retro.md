# Epic 7 Retrospective: Deployment & Infrastructure Automation

**Run ID:** 2026-02-15T123900Z
**Scope:** Epic 7 (Stories 7.1-7.4) + Deferred A-007 from Epic 5
**ACs:** 25 (across 4 stories)

---

## Wins

1. **Complete Azure deployment infrastructure delivered** — Bicep IaC with parameterized SKUs, CI/CD pipeline with OIDC federated credentials, staging/production environment isolation, health check endpoints. All 4 stories completed in ~50 minutes (17 commits).

2. **Zero regressions** — 311 frontend tests pass, 114 domain tests pass, zero tsc errors, zero ESLint warnings. Pre-epic gate fixed 22 ESLint errors.

3. **Critical C-1 caught and fixed** — Auto-migration breaking all Production-env tests discovered during Story 7.3 review. Config guard (Database:AutoMigrate) applied in commit 62b2ee8.

4. **Deferred A-007 completed** — Aggregate root architecture test validates DbContext exposes no DbSets for owned entities.

5. **Experiments E-011 and E-012 validated PASS** — Quality signals checklist and zero-error pre-epic gate both working.

## Problems

### 1. DEMO BLOCKED — 3rd Consecutive Epic (CRITICAL)

**0/25 ACs verified live.** This is the 3rd consecutive blocked demo (Epics 4, 5, 7).

Epic 7 delivered CLOUD deployment infrastructure (Azure Bicep, GitHub Actions, azd) but did NOT address LOCAL development infrastructure. The API cannot start: LocalDB not supported on Linux, Docker not installed. Docker Compose exists at repo root but is non-functional without Docker runtime.

**User feedback:** *"I am very annoyed that we still can't do the demo because there are infrastructure still missing to start-up what we have built. This whole epic was about that and now when we want to demo it's still not working."*

**Root cause:** Scope/expectations mismatch. The epic title "Deployment & Infrastructure Automation" was ambiguous — user expected LOCAL infrastructure, epic delivered CLOUD infrastructure.

### 2. 10 Pre-Existing Test Failures (6+ Epics)

10 backend unit tests have failed since before Epic 7 with zero investigation:
- Blob storage mocking (5 tests)
- Stale candidate filtering (3 tests)
- Validator architecture (1 test)
- Email validation (1 test)

Reported as "pre-existing" in every story review but never triaged or escalated.

### 3. Code Coverage: CI Collects, Retro Never Reports

CI pipeline already collects coverage (ci.yml:17: `--collect:"XPlat Code Coverage"`). But retro evidence has said "Not measured" for 6 consecutive epics. The COLLECTION works — the REPORTING gap is the real issue.

### 4. Fix Cycle Rate Increasing

75% of stories (3/4) required fix commits during review, up from 50% in Epic 5. Story 7.3 had a CRITICAL finding (C-1). Pre-implementation architectural risk assessment is missing.

### 5. Dev Agent Record Empty — Systemic

Empty in ALL 4 stories. 4th consecutive occurrence. Review Agent approved with Minor severity despite team-workflow.md requiring non-empty sections.

### 6. Evidence Assembly Error

A-005 (cross-recruitment test) and A-006 (auth denial logging) were incorrectly flagged as NOT APPLIED. Both were applied in commit fccb045 (Epic 5 retro self-healing). Evidence assembly only checked Epic 7 commit range, missing the retro commits.

**Correction:** Only A-004 (coverage reporting) genuinely remains unresolved from the previous retro.

## Previous Retro Action Items

| ID | Title | Priority | Status |
|----|-------|----------|--------|
| A-001 | Fix TypeScript errors + zero-error gate | P1 | APPLIED (bf38a64) |
| A-002 | ESLint execution in retro evidence | P1 | APPLIED (4c28cb7) |
| A-003 | Document runtime setup + Docker Compose | P1 | APPLIED (4c28cb7) |
| A-004 | Code coverage CI status | P1 | PARTIAL — CI collects (ci.yml:17) but retro never reports. Auto-escalates to P0. |
| A-005 | Cross-recruitment overview test | P1 | APPLIED (fccb045) — evidence bundle incorrectly flagged NOT APPLIED |
| A-006 | Structured auth denial logging | P1 | APPLIED (fccb045) — evidence bundle incorrectly flagged NOT APPLIED |
| A-007 | Aggregate root arch test (deferred) | P2 | APPLIED (4adb83c) |

**6 of 7 APPLIED. 1 PARTIAL (A-004 → auto-escalates to P0).**

## Experiment Validation

| ID | Hypothesis | Result | Recommendation |
|----|-----------|--------|----------------|
| E-010 | Docker Compose fallback eliminates demo BLOCKED | **FAIL** — Docker not installed, docs alone insufficient. Additionally, epic scope was CLOUD not LOCAL. | Modify — docs are necessary but not sufficient; Docker runtime must be verified |
| E-011 | Quality signals checklist eliminates "Not captured" | **PASS** — All signals have explicit data | Adopt |
| E-012 | Zero-error pre-epic gate prevents tsc accumulation | **PASS** — Zero pre-existing tsc errors | Adopt |

## Anti-Patterns Processing

All entries from `anti-patterns-pending.txt` processed:

| Entry | Decision | Rationale |
|-------|----------|-----------|
| Story 7.1 M-2: pipeline ordering | Remove (one-off) | Functionally irrelevant, not actionable as pattern |
| Story 7.1 M-3: Dev Agent Record empty | Action item A-007 | Process gap, covered by new action |
| Story 7.2 M-1: missing @description | Remove (fixed) | Fixed in follow-up commit 0f902f7 |
| Story 7.2 M-2: Dev Agent Record empty | Action item A-007 | Recurring, covered by process action |
| Story 7.3 C-1: auto-migration broke tests | Action item A-005 | Architecture-level, covered by opt-in default action |
| Story 7.3 M-2: Dev Agent Record empty | Action item A-007 | Recurring, covered by process action |
| Story 7.4 I-1: health check scope | Remove (one-off) | Fixed in 92090e6, specific to CD pipeline |
| Story 7.4 I-2: azd --output json | Remove (one-off) | Architectural preference, not recurring pattern |
| Story 7.4 M-2: Dev Agent Record empty | Action item A-007 | Recurring (4th occurrence), covered by process action |

**No new permanent anti-patterns promoted.** All recurring issues are process gaps covered by action items, not code patterns catchable by regex.

**File reset to header-only state.**

## Action Items

### P0 — Apply Immediately

| ID | Title | Type | Owner |
|----|-------|------|-------|
| A-001 | Investigate and fix 10 pre-existing backend test failures | quality_gate_gap | QA |
| A-002 | Capture code coverage in retro evidence (CI already collects) | instrumentation_gap | Delivery |
| A-003 | Resolve local dev environment for demos and testing | quality_gate_gap | SRE |

### P1 — Apply Immediately

| ID | Title | Type | Owner |
|----|-------|------|-------|
| A-004 | Remove duplicate test execution from CD pipeline | refactor | SRE |
| A-005 | Make auto-migration opt-in + add startup logging | architecture_alignment | Architecture |
| A-006 | Remove hardcoded SQL password from docker-compose.yml | security_hardening | Security |
| A-007 | Enforce Dev Agent Record population (elevate to Important) | process_change | Docs |
| A-008 | Add rollback procedures and DR documentation | docs_update | SRE |
| A-009 | Fix evidence assembly to check pre-epic retro commits | process_change | Delivery |
| A-010 | Add auto-escalation rule for long-lived test failures | process_change | QA |

### Deferred (Tracked)

None. All actions applied or in progress.

## New Experiments

| ID | Hypothesis | How | Success Metric |
|----|-----------|-----|----------------|
| E-013 | Pre-epic local env validation gate prevents blocked demos | Add validate-local-env.sh to pre-epic gate (Docker up + /health check) | Epic 8 demo executes 50%+ ACs |
| E-014 | Auto-migration opt-in default prevents startup side-effect findings | Change Database:AutoMigrate default to false | Zero startup Critical findings in Epic 8 |
| E-015 | Elevating Dev Agent Record to Important severity eliminates empty sections | Update Review Agent checklist: Dev Agent Record is Important, not Minor | Zero empty Dev Agent Record findings in Epic 8 |

## Timing

| Metric | Value |
|--------|-------|
| First commit | 2026-02-15 12:46:59 +0100 (bf38a64) |
| Last commit | 2026-02-15 13:37:02 +0100 (edea2da) |
| Elapsed | ~50 minutes |
| Commits | 17 |
| Stories | 4 + 1 deferred item |
| Commits per story | 4.25 |

## Key Insight: Scope vs Expectations

Epic 7 successfully delivered what was scoped: Azure Bicep templates, GitHub Actions CI/CD, OIDC credentials, staging/production environments, health check endpoints. The infrastructure-as-code is comprehensive and well-structured.

However, the user expected this epic to resolve the LOCAL development infrastructure gap that has blocked demos for 3 consecutive epics. The epic title "Deployment & Infrastructure Automation" suggested fixing the inability to run the system, but the scope was limited to CLOUD deployment via Azure.

**For Epic 8:** The #1 priority is resolving local dev environment blockers (Docker runtime, SQL Server, functional tests). Without this, no epic can be demo'd regardless of how well the code is written.
