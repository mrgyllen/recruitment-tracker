# E2E Scenario Registry

_Tracks user journey scenarios, their risk, and which test layer covers them. See [`testing-standards.md` — E2E Decomposition Method](../_bmad-output/planning-artifacts/architecture/testing-standards.md#e2e-decomposition-method) for the methodology._

_Created per [ADR-001](../_bmad-output/planning-artifacts/architecture/adrs/ADR-001-test-pyramid-e2e-decomposition.md)._

## How to Read This Registry

- **Scenario:** A user-visible behavior from a PRD user journey
- **Risk:** What could go wrong (the failure mode this scenario guards against)
- **Covered By:** Which existing test(s) cover this risk, with test layer prefix
- **Gap?:** `No` = fully covered, `Partial` = partially covered (details in parentheses), `Yes` = needs E2E test

## J0: First Five Minutes (Onboarding)

| Scenario | Risk | Covered By | Gap? |
|----------|------|-----------|------|
| SSO login redirects to Entra ID | Auth misconfiguration | Functional: `Application.FunctionalTests` auth middleware (dev auth bypass in test) | No |
| Unauthenticated request returns 401 | Anonymous access to data | Functional: `TenantIsolationTests`; CD smoke test: auth enforcement check | No |
| Empty state shows create recruitment prompt | UI regression | Frontend: component tests (when implemented) | Partial (no frontend tests yet) |
| Create recruitment with default workflow steps | Data integrity | Unit: `CreateRecruitmentCommandTests`; Functional: recruitment creation tests | No |
| Upload XLSX creates candidates | Import data integrity | Unit: import handler tests; Functional: import endpoint tests (when implemented) | Partial (functional import tests not yet written) |
| Upload CV bundle matches to candidates | File processing | Unit: PDF splitting logic tests (when implemented) | Partial (Epic 6 scope — not yet implemented) |

## J1: Running a Recruitment (Daily Check)

| Scenario | Risk | Covered By | Gap? |
|----------|------|-----------|------|
| Home screen shows recruitment overview with counts | Query correctness | Unit: `GetRecruitmentOverviewQueryHandler` tests; Functional: overview endpoint tests | Partial (functional test needed per handler coverage rule — uses `.Select()` projection) |
| Pipeline view shows candidates at correct steps | Query filter correctness | Integration: `TenantContextFilterTests`; Functional: candidate list tests | No |
| User sees only their own recruitments | Tenant isolation breach | Integration: `TenantIsolationTests` (8 mandatory scenarios) | No |
| Cross-recruitment data invisible | Data leak | Integration: `TenantIsolationTests` scenario 1 (cross-recruitment isolation) | No |
| Close recruitment locks edits | State guard | Unit: recruitment domain entity tests | No |
| Stale step visual indicator appears | UI regression | Frontend: component tests (when implemented) | Partial (no frontend tests yet) |

## J2: Mid-Process Disruption (Import)

| Scenario | Risk | Covered By | Gap? |
|----------|------|-----------|------|
| Re-import XLSX upserts without overwriting outcomes | Data corruption | Unit: import handler upsert logic tests | Partial (functional test recommended — complex EF Core upsert logic) |
| Import summary shows row-level detail | Response shape | Unit: import handler tests; Contract: import DTO tests (when implemented) | Partial (contract tests not yet written) |
| Add workflow step mid-process preserves candidate progress | State corruption | Unit: `AddStep` domain tests; Functional: workflow modification tests | No |
| Low-confidence match flagged for review | Match logic | Unit: candidate matching tests | No |

## J3: Batch Screening (Lina's Flow)

| Scenario | Risk | Covered By | Gap? |
|----------|------|-----------|------|
| Split-panel layout loads candidate list + CV | UI integration | Frontend: component tests (when implemented) | Partial (no frontend tests yet) |
| PDF viewer renders candidate CV | File access | Unit: SAS token generation tests | Partial (no browser-based PDF rendering test) |
| Record outcome saves and advances to next candidate | Data persistence | Unit: `RecordOutcome` domain tests; Functional: outcome recording tests | No |
| Outcome immediately visible to other team members | Real-time consistency | Functional: concurrent access tests (when implemented) | Partial (no concurrent access tests yet) |
| Filter candidates by step and status | Query correctness | Unit: candidate query handler tests; Functional: filtered list tests | No |

## Summary

| Journey | Total Scenarios | Fully Covered | Partial | Needs E2E |
|---------|----------------|---------------|---------|-----------|
| J0: Onboarding | 6 | 2 | 4 | 0 |
| J1: Daily Check | 6 | 4 | 2 | 0 |
| J2: Import | 4 | 2 | 2 | 0 |
| J3: Screening | 5 | 2 | 3 | 0 |
| **Total** | **21** | **10** | **11** | **0** |

No scenario currently requires a browser-based E2E test. All gaps are addressable by adding lower-level tests (primarily functional tests for complex query handlers and frontend component tests). The "Partial" gaps will be progressively closed as epics implement the corresponding features.
