# Evidence Bundle: Epic 2 Retrospective (Stories 2.1-2.5)

**Run ID:** 2026-02-14T013000Z
**Type:** Epic retro (covers all 5 stories in Epic 2, validates deferred items from Epic 1 retros)
**Scope:** Stories 2.1-2.5 — Recruitment & Team Setup

---

## 1. Scope

### Story 2.1: Create Recruitment with Workflow Steps
- **ACs:** Dialog display, workflow step customization, successful creation (201), new candidate default placement, title validation, step name uniqueness & contiguous order
- **FRs:** FR4, FR5, FR59, FR62
- **Status:** Done (fix cycle — C1, I1-I3 fixed)

### Story 2.2: Recruitment List & Navigation
- **ACs:** Recruitment list display, empty state, click-to-navigate, breadcrumb display, recruitment selector dropdown, access control (403)
- **FRs:** FR6, FR13, FR60
- **Status:** Done (clean pass)

### Story 2.3: Edit Recruitment & Manage Workflow Steps
- **ACs:** Edit recruitment details, add workflow step, remove step (no outcomes), block removal (has outcomes), reorder steps, closed recruitment read-only
- **FRs:** FR11, FR12
- **Status:** Done (fix cycle — C1 security, I1 fixed)

### Story 2.4: Team Membership Management
- **ACs:** View member list, directory search, add member, creator is permanent, remove non-creator, cannot remove creator, access revocation visible
- **FRs:** FR56, FR57, FR58, FR61
- **Status:** Done (fix cycle — C1 exception mapping fixed)

### Story 2.5: Close Recruitment & Read-Only View
- **ACs:** Close confirmation dialog, successful close, read-only mode enforcement, closed recruitment data visibility, API mutation rejection, visual distinction in list
- **FRs:** FR7, FR8
- **Status:** Done (clean pass)

---

## 1b. Previous Retros

### Retro 1 (2026-02-13T002800Z) — Epic 1 Stories 1.1-1.3

**Applied actions (all verified):**
- A1: Remove AddDefaultIdentity (security_hardening P0) — Applied
- A2: Document Fluent API for domain events (docs_update P0) — Applied
- A3: Add template cleanup checklist (docs_update P1) — Applied
- A4: Document middleware pipeline ordering (docs_update P1) — Applied
- A5: Enforce Dev Agent Record completion (process_change P1) — Applied
- A7: Promote anti-patterns to permanent (guideline_gap P1) — Applied

**Deferred items from Retro 1:**
- A6: Consolidate IUser/ICurrentUserService — **RESOLVED** in Epic 2 deferred items commit (2d91064). ICurrentUserService deleted, TenantContext uses IUser.
- A8: Fix Guid.ToString() type mismatch — **RESOLVED** in commit 2d91064. ITenantContext now has `UserGuid` property (Guid? type).
- A9: Domain event payload verification tests — **RESOLVED** in commit 2d91064. Tests now verify event payloads.
- A10: CORS policy documentation — **RESOLVED** in commit 2d91064. Documented in patterns-backend.md.

### Retro 2 (2026-02-13T220300Z) — Epic 1 Stories 1.4-1.5

**Applied actions:**
- B1: Promote 3 anti-patterns from pending to permanent — Applied
- B2: Document ProtectedRoute auth guard pattern — Applied
- B3: Document Ghost button deferral — Applied
- B4: Add prefers-reduced-motion requirement — Applied
- B5: Document useAppToast naming — Applied
- B6: Remove dead @custom-variant dark — Applied

**Deferred items from Retro 2:**
- B7: Code coverage thresholds — **RESOLVED** (sprint-status: done)
- B8: ProtectedRoute edge case tests — **RESOLVED** (sprint-status: done)
- B9: CSP and security headers — **RESOLVED** (sprint-status: done, SecurityHeadersMiddleware added)
- B10: MSAL production auth — **RESOLVED** (sprint-status: done, closed as deployment-specific)
- B11: Bundle size budget tracking — **RESOLVED** (sprint-status: done)
- A6 (carried): Consolidate IUser/ICurrentUserService — **RESOLVED** (see above)
- A8 (carried): Fix Guid.ToString() — **RESOLVED** (see above)
- A9 (carried): Domain event payload tests — **RESOLVED** (see above)
- A10 (carried): CORS documentation — **RESOLVED** (see above)

**Assessment:** ALL deferred items from both previous retros have been resolved in Epic 2. No carried items remain.

---

## 2. Git Summary

**Commit range:** 9e4913f..HEAD (42 commits on branch epic2/recruitment-team-setup)
**Files changed:** 133
**Lines:** +10,173 / -192

### Commits
```
3924d83 chore(2.5): fix import ordering and mark story 2.5 done
b17e5f2 feat(2.5): wire close button and dialog into RecruitmentPage
d9b126e feat(2.5): add CloseRecruitmentDialog with confirmation and tests
f75cc05 feat(2.5): add close API client, mutation hook, and MSW handler
257483c chore(2.5): install shadcn/ui AlertDialog component
4da3cd1 feat(2.5): add POST /api/recruitments/{id}/close endpoint
8098863 feat(2.5): add CloseRecruitment command, validator, handler + tests
c0c7a6c test(2.5): add domain tests for AddMember/RemoveMember closed guard
b1a608e fix(story-2.4): map domain rule violations to Problem Details 400
8346c4c chore(2.4): fix import ordering and mark story 2.4 done
defda96 feat(2.4): wire MemberList into RecruitmentPage
b9b226d feat(2.4): add InviteMemberDialog tests and fix DialogDescription warning
1d49d67 feat(2.4): add MemberList component with creator badge, remove action, and InviteMemberDialog
ae0c75d feat(2.4): add MSW handlers for team membership endpoints
62ec684 feat(2.4): add team API client, types, TanStack Query hooks, and useDebounce
a0f9c61 feat(2.4): add TeamEndpoints with GET/POST/DELETE members and directory search
140a785 feat(2.4): add RemoveMember command with handler, validator, and tests
78c658f feat(2.4): add AddMember command with handler, validator, and tests
dd35b3a feat(2.4): add GetMembers query with ITenantContext membership check
a67a728 feat(2.4): add SearchDirectory query with handler, validator, and tests
b2022cd feat(2.4): add IDirectoryService interface with dev stub and Entra ID placeholder
633fd35 feat(2.4): add DisplayName property to RecruitmentMember entity
e4a8eef docs(2.4): add team membership management implementation plan
f1dee45 fix(story-2.3): add ITenantContext membership checks to command handlers
36dec06 chore: update sprint status + dev agent record for Story 2.3
de81333 style: fix ESLint import order warnings in Story 2.3 files
7920128 feat(story-2.3): RecruitmentPage integration with edit form + step editor
919d6e5 feat(story-2.3): WorkflowStepEditor edit mode with API mutations
3535297 feat(story-2.3): EditRecruitmentForm component with tests
dcabd08 feat(story-2.3): MSW handlers for edit + step management endpoints
0aee2f2 feat(web): add API types, client methods, and mutation hooks for Story 2.3
ec34e90 feat(story-2.3): exception mappings + API endpoints for edit/step management
8954cca feat(story-2.3): AddWorkflowStep, RemoveWorkflowStep, ReorderWorkflowSteps commands
2651fcf feat(story-2.3): UpdateRecruitment command + handler + validator
166ee82 feat(story-2.3): domain UpdateDetails + ReorderSteps methods
57af0ee chore: update sprint status - Story 2.2 done
a7bc414 feat(story-2.2): Recruitment List & Navigation (full-stack)
19e7233 fix(story-2.1): address review findings (C1, I1, I2, I3)
0e54d31 chore: update sprint-status for Story 2.1 done + deferred items resolved
3cbac5f feat(story-2.1): Create Recruitment with Workflow Steps (full-stack)
2d91064 Resolve Epic 1 deferred items (A6, A8, A9, A10, B7-B11)
```

### Diffstat Summary
- Backend: ~70 new/modified files (domain, application, infrastructure, web, tests)
- Frontend: ~40 new/modified files (features, hooks, API clients, components, tests, mocks)
- Docs/Config: ~20 files (story files, plans, sprint-status, ci.yml, architecture)

---

## 3. Quality Signals

### Test Results
- **Backend:** 20+ new test files with comprehensive handler, validator, and domain tests
- **Frontend:** 10+ new test files covering CreateRecruitmentForm, EditRecruitmentForm, WorkflowStepEditor, RecruitmentList, RecruitmentSelector, RecruitmentPage, MemberList, InviteMemberDialog, CloseRecruitmentDialog, routes
- **Test execution evidence:** Not captured at CI level (local runs implied by verification-before-completion)

### Build Results
- CI pipeline runs on push (`.github/workflows/ci.yml` updated)
- No build failures evident in commit history (clean linear progress)

### Code Coverage
- Not captured (coverage thresholds were resolved as B7 deferred item but no evidence of actual metrics)

---

## 4. Review Findings

### Story 2.1 (fix cycle)
- **C1:** Dev Agent Record empty — FIXED (commit 19e7233)
- **I1:** JobRequisitionId silently dropped — FIXED (extended Create factory with optional param)
- **I2:** Missing contiguous step order validation — FIXED (added to validator)
- **I3:** Raw button instead of shared component — FIXED (switched to ActionButton)
- **M1:** Duplicate `generateStepId` function (crypto.randomUUID in form + separate file)
- **M2:** Mutable default steps array (workflowDefaults.ts uses function, not const)
- **M3:** Eager loading pattern in GetRecruitmentById (Include chain)
- **M4:** "Loading..." text instead of SkeletonLoader
- **M5:** Implicit auth dependency in CreateRecruitmentPage

### Story 2.2 (clean pass)
- **M1:** Duplicate `useRecruitments` hook (hooks/useRecruitments.ts created but similar to inline query)
- **M2:** Badge vs StatusBadge inconsistency (using shadcn Badge, not custom StatusBadge)
- **M3:** Empty string fallback in descriptions
- **M4:** Eager loading in GetRecruitmentsQueryHandler

### Story 2.3 (fix cycle)
- **C1:** ALL 4 command handlers missing ITenantContext membership checks — SECURITY — FIXED (commit f1dee45)
- **I1:** Missing test for RemoveWorkflowStep with outcomes — FIXED
- **M1:** 200 vs 201 for step creation — FIXED (AddWorkflowStep returns 201 Created)
- **M2:** Unnecessary Include in handler — FIXED
- **M3:** Read-only inputs UX (inputs not visually disabled when recruitment is closed)
- **M4:** Status field not shown on RecruitmentPage
- **M5:** Query keys consistency (false alarm — keys are consistent)

### Story 2.4 (fix cycle)
- **C1:** InvalidOperationException not mapped in CustomExceptionHandler — FIXED (added DomainRuleViolationException, commit b1a608e)
- **I1:** TeamEndpoints inconsistent registration pattern (static class with MapTeamEndpoints vs EndpointGroupBase)
- **I2:** SearchDirectory URL implies recruitment scoping but recruitmentId is unused in handler
- **M1:** Missing closed recruitment handler tests for AddMember/RemoveMember
- **M2:** Status field display (minor, deferred)
- **M3:** Empty state for member list

### Story 2.5 (clean pass)
- **M1:** CloseRecruitment endpoint returns `Results.Ok()` instead of `Results.NoContent()` (200 vs 204)
- **M2:** Dev Agent Record status field

---

## 5. Fix Cycle Analysis

| Story | Outcome | Fix Commits | Root Cause |
|-------|---------|-------------|------------|
| 2.1 | Fix cycle (1 round) | 19e7233 | Missing JobRequisitionId param in factory, missing step order validation, empty Dev Agent Record |
| 2.2 | Clean pass | — | — |
| 2.3 | Fix cycle (1 round) | f1dee45 | ALL command handlers missing ITenantContext membership verification (security) |
| 2.4 | Fix cycle (1 round) | b1a608e | Missing exception mapping for DomainRuleViolationException |
| 2.5 | Clean pass | — | — |

**Fix cycle rate:** 3/5 stories (60%) required fix cycles. 2/5 stories (40%) passed clean.
**Security finding rate:** 1/5 stories had critical security finding (Story 2.3 — all 4 command handlers missing authorization).
**Pattern:** Fix cycles occur on first story of a new domain area (2.1 = first recruitment, 2.3 = first edit/mutation, 2.4 = first team feature).

---

## 6. Anti-Patterns Discovered

### anti-patterns-pending.txt
Currently empty (header only). Previous sprint's pending items were promoted in Retro 2.

### anti-patterns.txt (permanent)
Contains 18 entries covering: test framework, template cleanup, frontend patterns, domain model rules.

---

## 7. Guideline References

- `_bmad-output/planning-artifacts/architecture.md` — Core decisions, aggregate boundaries
- `_bmad-output/planning-artifacts/architecture/patterns-backend.md` — Backend patterns
- `_bmad-output/planning-artifacts/architecture/api-patterns.md` — Minimal API patterns
- `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` — Frontend patterns
- `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` — Component structure
- `_bmad-output/planning-artifacts/architecture/testing-standards.md` — Test patterns
- `.claude/process/team-workflow.md` — Development process
- `docs/testing-pragmatic-tdd.md` — Testing policy

---

## 8. Sprint-Status Snapshot

```yaml
epic-2: in-progress
2-1-create-recruitment-with-workflow-steps: done
2-2-recruitment-list-navigation: done
2-3-edit-recruitment-manage-workflow-steps: done
2-4-team-membership-management: done
2-5-close-recruitment-read-only-view: done
epic-2-retrospective: optional

# All Epic 1 deferred items: done
epic-1-deferred-coverage: done
epic-1-deferred-protectedroute-tests: done
epic-1-deferred-csp-headers: done
epic-1-deferred-msal-auth: done
epic-1-deferred-bundle-budget: done
```
