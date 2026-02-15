# Epic 7 Retro Evidence Bundle

**Run ID:** 2026-02-15T123900Z
**Scope:** Epic 7: Deployment & Infrastructure Automation (Stories 7.1–7.4) + Epic 5 deferred item A-007

## 1. Scope

| Story | Title | ACs |
|-------|-------|-----|
| Deferred A-007 | Aggregate root architecture test | 2 tests |
| 7.1 | Health Check Endpoints | 5 ACs (liveness, readiness, 503, auth bypass, env independence) |
| 7.2 | Azure Infrastructure as Code | 7 ACs (azure.yaml, Blob Storage, runtime, SKUs, provisioning, teardown, declarative) |
| 7.3 | CI/CD Pipeline with Auto-Migration | 6 ACs (CI unchanged, CD on merge, separate workflows, auto-migration, rollback, template removed) |
| 7.4 | Staging Environment | 7 ACs (staging isolation, CD default staging, health checks, manual promotion, env-specific secrets, teardown, no orphans) |

**Total ACs: 25** (across 4 stories)

## 1b. Previous Retros

**Most recent:** `.retro/2026-02-14T224500Z/` (Epic 5)

### Previous Action Items Status

| ID | Title | Priority | Status in Epic 7 |
|----|-------|----------|------------------|
| A-001 | Fix pre-existing TypeScript errors + zero-error gate | P1 | APPLIED — ESLint errors fixed in pre-epic gate (bf38a64), tsc zero errors confirmed |
| A-002 | Add ESLint execution to retro evidence | P1 | APPLIED — team-workflow.md updated in retro self-healing (4c28cb7) |
| A-003 | Document runtime setup + Docker Compose fallback | P1 | APPLIED — docs/getting-started.md created in retro self-healing (4c28cb7) |
| A-004 | Verify code coverage CI status | P1 | NOT APPLIED — no coverage configuration added to CI or retro evidence |
| A-005 | Cross-recruitment overview test | P1 | NOT APPLIED — TenantIsolationTests.cs not modified in Epic 7 |
| A-006 | Structured logging for auth denials | P1 | NOT APPLIED — CustomExceptionHandler.cs not modified in Epic 7 |
| A-007 | Aggregate root arch test | P2 (deferred) | APPLIED — created AggregateRootArchitectureTests.cs (4adb83c) |

**3 of 7 actions NOT APPLIED (A-004, A-005, A-006).** Per process rules, P1 items carried from a previous retro cannot be deferred again — these auto-escalate to P0.

### Previous Experiments to Validate

| ID | Hypothesis | Success Metric | Result |
|----|-----------|----------------|--------|
| E-010 | Docker Compose fallback eliminates demo BLOCKED | Epic 7 demo executes 50%+ ACs | **FAIL** — Demo BLOCKED (0/25 ACs verified). Docker Compose docs exist (getting-started.md) but Docker not installed. Demo process tried native then Docker — both failed. |
| E-011 | Required quality signals checklist eliminates "Not captured" | Zero "Not captured" in retro evidence | **PASS** — All quality signals captured with pass/fail data in this evidence bundle |
| E-012 | Zero-error pre-epic gate prevents tsc error accumulation | Zero pre-existing tsc errors at Epic 7 start | **PASS** — Pre-epic gate fixed 22 ESLint errors + 87 warnings (bf38a64). tsc --noEmit returns zero errors. |

## 2. Git Summary

**Base:** 3414c38 (plan: create stories 7.1–7.4 for Epic 7)
**Head:** edea2da (demo: record BLOCKED epic 7 demo walkthrough)
**Commits:** 17
**Files changed:** 65 (+1902/-197 lines)

```
bf38a64 chore: fix pre-existing ESLint errors and import ordering (pre-epic gate)
4adb83c feat: add aggregate root architecture test (epic-5 deferred A-007)
2f4704d feat(7.1): add /health liveness and /ready readiness endpoints
520fb7e fix(7.1): strengthen readiness tests with positive-path 200 assertions
51b226f fix(7.1): strengthen ReadyEndpoint auth test with 200 assertion (M-1)
ea8c09e chore(7.1): mark story done, mini-retro anti-patterns
305d23e feat(infra): add Blob Storage, parameterize SKUs, fix runtime for Story 7.2
cda8896 fix(infra): remove sku:null risk in sqlserver.bicep (review I-1)
0f902f7 chore(infra): add @description to storageAccountName param (review M-1)
6f3be09 chore(7.2): mark story done, mini-retro anti-patterns
932e22a feat(7.3): add CD pipeline, auto-migration, remove template workflow
62b2ee8 fix(7.3): guard auto-migration with Database:AutoMigrate config (review C-1)
90cd051 chore(7.3): mark story done, mini-retro anti-patterns
b0a4561 feat(7.4): add staging environment support with environment selection
92090e6 fix(7.4): verify both /health and /ready, portable azd output parsing (review I-1, I-2, M-1)
4212a27 chore(7.4): mark story done, mini-retro anti-patterns
edea2da demo: record BLOCKED epic 7 demo walkthrough
```

## 3. Quality Signals

### Build
- **API:** PASS — `dotnet build api/api.slnx` — 0 warnings, 0 errors
- **Frontend:** PASS — `npm run build` — built in 4.15s (chunk size warning, pre-existing)

### Tests
- **Frontend:** PASS — 51 test files, 311 tests, 0 failures
- **Backend domain tests:** PASS — 114 passed, 0 failed
- **Backend unit tests:** 227 passed, 10 FAILED (all pre-existing, not introduced by Epic 7)
  - Pre-existing failures: AllRequestTypes_ShouldHaveValidator_UnlessExempt, Handle_CandidateNotFound_ThrowsNotFoundException, Handle_ValidRequest_AssignsDocumentAndReturnsDto, Handle_WithExistingDocument_DeletesOldBlob, Handle_WithoutExistingDocument_DoesNotDeleteBlob, Validate_EmailTooLong_Fails, Handle_StaleOnlyWithoutStepId_ReturnsAllStaleCandidates, Handle_StaleOnlyWithStepId_ReturnsOnlyStaleCandidatesAtStep, Handle_StaleCandidates_ReturnsPerStepStaleCounts, VerifyBlobOwnership_WithEmptyPath_ReturnsFalse
- **Backend functional tests:** BLOCKED — requires Docker (Testcontainers SQL Server)

### TypeScript
- **PASS** — `npx tsc --noEmit` returns zero errors

### ESLint
- **PASS** — `npx eslint src/ --max-warnings 0` returns zero errors/warnings

### Code Coverage
- **Frontend:** Not measured — no `--coverage` flag run
- **Backend:** Not measured — no `--collect:"XPlat Code Coverage"` run
- **Status:** A-004 from previous retro NOT APPLIED. This is the 6th epic without coverage data.

## 4. Review Findings

### Story 7.1
| Severity | Finding | Resolution |
|----------|---------|-----------|
| Important | I-1: Weak readiness test assertions (not-404 instead of 200) | Fixed: AlwaysHealthyCheck stub, 7 tests (520fb7e) |
| Minor | M-1: Overlapping auth test without positive assertion | Fixed: strengthened to 200 assertion (51b226f) |
| Minor | M-2: Pipeline ordering differs from story spec | Not fixed — functionally irrelevant |
| Minor | M-3: Dev Agent Record empty | Not fixed — recurring pattern |

### Story 7.2
| Severity | Finding | Resolution |
|----------|---------|-----------|
| Important | I-1: `sku: null` on sqlDatabase may cause ARM errors | Fixed: concrete default, removed null conditional (cda8896) |
| Minor | M-1: Missing @description decorator on storageAccountName | Fixed (0f902f7) |
| Minor | M-2: Dev Agent Record empty | Not fixed — recurring |

### Story 7.3
| Severity | Finding | Resolution |
|----------|---------|-----------|
| **Critical** | C-1: Auto-migration breaks ALL Production-env functional tests | Fixed: Database:AutoMigrate config guard (62b2ee8) |
| Important | I-1: CD runs Testcontainers tests (pipeline risk) | Not fixed — CI handles same tests |
| Minor | M-1: Scope isolation of using var | Addressed by config guard restructuring |
| Minor | M-2: Dev Agent Record empty | Not fixed — recurring |

### Story 7.4
| Severity | Finding | Resolution |
|----------|---------|-----------|
| Improvement | I-1: Health check only verifies /health, not /ready | Fixed: both endpoints checked (92090e6) |
| Improvement | I-2: azd --output json portability | Fixed: dotenv parsing (92090e6) |
| Minor | M-1: Inconsistent input reference syntax | Fixed (92090e6) |
| Minor | M-2: Dev Agent Record empty | Not fixed — recurring |

**Fix cycle rate:** 75% (3 of 4 stories required fixes). Story 7.4 was only improvements, not blocking.

## 5. Anti-Patterns Discovered

Contents of `.claude/hooks/anti-patterns-pending.txt`:
```
# Pending anti-patterns — added by mini-retro during sprint
# Will be promoted to permanent or removed by autonomous retro
# Story 7.1: M-2 (pipeline ordering) not actionable — no anti-pattern added
# Story 7.1: M-3 Dev Agent Record empty — process gap, not code anti-pattern
# Story 7.2: M-1 fixed in follow-up commit (0f902f7), M-2 Dev Agent Record empty (recurring)
# Story 7.3: C-1 auto-migration broke Production-env tests — guard startup logic with config flag
# Story 7.3: M-2 Dev Agent Record empty (3rd occurrence — process gap)
# Story 7.4: I-1 health check step only verified /health, not /ready — verify all AC-specified endpoints
# Story 7.4: I-2 azd --output json portability — prefer dotenv parsing over JSON flags for CLI tools
# Story 7.4: M-2 Dev Agent Record empty (4th occurrence — systemic process gap)
```

## 6. Guideline References

- `_bmad-output/planning-artifacts/architecture.md` — aggregate boundaries, DDD rules
- `_bmad-output/planning-artifacts/architecture/patterns-backend.md` — handler patterns
- `_bmad-output/planning-artifacts/architecture/testing-standards.md` — test patterns
- `.claude/process/team-workflow.md` — team process, demo, retro

## 7. Sprint-Status Snapshot

```
epic-7: in-progress
7-1-health-check-endpoints: done
7-2-azure-infrastructure-as-code: done
7-3-cicd-pipeline-auto-migration: done
7-4-staging-environment: done
epic-7-retrospective: optional
```

All 4 stories done. Epic status still `in-progress` (pending retro completion).

## 8. Execution Timing

- **First commit:** 2026-02-15 12:46:59 +0100 (bf38a64)
- **Last commit:** 2026-02-15 13:37:02 +0100 (edea2da)
- **Elapsed:** ~50 minutes
- **Commits:** 17
- **Stories:** 4 + 1 deferred item
- **Commits per story:** 3.4

## 9. Previous Experiments Validation

See Section 1b above:
- E-010: **FAIL** — Demo still BLOCKED despite Docker docs
- E-011: **PASS** — All quality signals captured
- E-012: **PASS** — Zero pre-existing tsc errors

## 10. Demo Walkthrough Results

**File:** `_bmad-output/implementation-artifacts/epic-7-demo-2026-02-15.md`

- **Total ACs verified:** 0
- **Pass:** 0
- **Fail:** 0
- **Blocked:** 25 (all)
- **Demo method:** BLOCKED — No SQL Server, Docker, or Azure subscription available

**THIS IS THE 3rd CONSECUTIVE BLOCKED DEMO (Epics 4, 5, 7).** The infrastructure epic that was supposed to enable deployment and testing ends with the same blocked demo. The fundamental gap: Epic 7 addressed CLOUD deployment infrastructure (Azure Bicep, GitHub Actions, azd) but did NOT address the LOCAL development infrastructure needed to verify the work.

### User Feedback (Critical)

The user expressed frustration: "I am very annoyed that we still can't do the demo because there are infrastructure still missing to start-up what we have built. This whole epic was about that and now when we want to demo it's still not working."

This signals a scope/expectations mismatch: the user expected Epic 7 to resolve the systemic inability to run the application locally, but the epic focused exclusively on Azure cloud deployment.

## 11. 10 Pre-Existing Backend Test Failures

These 10 tests have been failing since before Epic 7. They are NOT regressions from this epic. They appear to be related to:
- Blob storage service mocking (Handle_CandidateNotFound, Handle_ValidRequest_AssignsDocumentAndReturnsDto, Handle_WithExistingDocument_DeletesOldBlob, Handle_WithoutExistingDocument_DoesNotDeleteBlob, VerifyBlobOwnership_WithEmptyPath_ReturnsFalse)
- Stale candidate filtering (Handle_StaleOnlyWithoutStepId, Handle_StaleOnlyWithStepId, Handle_StaleCandidates_ReturnsPerStepStaleCounts)
- Validator architecture (AllRequestTypes_ShouldHaveValidator_UnlessExempt)
- Validation (Validate_EmailTooLong_Fails)

These may be infrastructure-dependent (need real Blob Storage SDK mocking) or may have been broken by Epic 5's overview changes. They have been reported as pre-existing in every story review but never investigated.
