# Evidence Bundle: Epic 3 Retrospective (Stories 3.1-3.5)

**Run ID:** 2026-02-14T073000Z
**Type:** Epic retro (covers all 5 stories in Epic 3 + deferred items from Epic 2)
**Scope:** Stories 3.1-3.5 — Candidate Import & Document Management

---

## 1. Scope

### Deferred Items from Epic 2 (resolved first)
- **A-006:** Standardize TeamEndpoints to use EndpointGroupBase — **RESOLVED** (commit 92f64e1)
- **A-008:** Add integration tests for CustomExceptionHandler — **RESOLVED** (commit 92f64e1, 8 test methods)

### Story 3.1: Manual Candidate Management
- **ACs:** Add candidate form, create with validation, duplicate email detection, remove with confirmation, closed recruitment guard
- **FRs:** FR23, FR24, FR62
- **Status:** Done (fix cycle — I1 empty Dev Agent Record, fixed)
- **Commit:** c6fb005 (29 files, +1788 lines)

### Story 3.2: XLSX Import Pipeline
- **ACs:** File upload validation, ImportSession creation, async processing via Channel<T>, XLSX parsing, candidate matching (email/name+phone), idempotent re-import, status polling
- **FRs:** FR14-FR17, FR19-FR22, FR25
- **Status:** Done (fix cycle — C1 missing auth on GetImportSessionQueryHandler, fixed)
- **Commits:** 14 commits ending at b3b580f

### Story 3.3: Import Wizard & Summary UI
- **ACs:** Sheet wizard entry, file upload step, processing/polling, summary with counts, error detail, match review (confirm/reject), discrepancy notice, failure handling, close/refresh
- **FRs:** FR14, FR17, FR18, FR25, FR26, FR55
- **Status:** Done (fix cycle — C1 matchIndex bug, I1 missing candidate operations, both fixed)
- **Commits:** 6 commits ending at 5a09258

### Story 3.4: PDF Bundle Upload & Splitting
- **ACs:** PDF validation, TOC parsing, page boundary detection, individual PDF extraction, blob storage, error handling, partial success, progress reporting, re-upload replacement
- **FRs:** FR28, FR29, FR30
- **Status:** Done (clean pass — 0C, 0I, 5M)
- **Commits:** 9 commits ending at 7a5abeb

### Story 3.5: CV Auto-Match, Manual Assignment & Individual Upload
- **ACs:** Name-based auto-matching, unmatched document display, manual assignment UI, individual upload per candidate, document replacement, closed recruitment guard
- **FRs:** FR31, FR32, FR33
- **Status:** Done (fix cycle — I1 CandidateDetail existingDocument always null, fixed)
- **Commits:** 10 commits ending at d166b26

---

## 1b. Previous Retro

### Retro (2026-02-14T013000Z) — Epic 2 Stories 2.1-2.5

**Applied actions (all verified):**
- A-001: Document command handler authorization pattern in patterns-backend.md — Applied
- A-002: Add authorization verification to review checklist — Applied (via E-001 experiment)
- A-003: Document endpoint registration convention (EndpointGroupBase) — Applied
- A-004: Document transient _stepsWithOutcomes pattern — Applied
- A-005: Add SkeletonLoader requirement to patterns-frontend.md — Applied (anti-pattern added to pending.txt)

**Deferred items from Epic 2 retro:**
- A-006: Standardize TeamEndpoints to use EndpointGroupBase — **RESOLVED** (commit 92f64e1)
- A-007: Fix CloseRecruitment endpoint to return 204 — **NOT RESOLVED** (still returns 200)
- A-008: Add integration tests for CustomExceptionHandler — **RESOLVED** (commit 92f64e1)

**Experiments from Epic 2 retro:**
- E-001: Authorization check requirement in implementation plans → Applied to team-workflow.md
- E-002: Anti-pattern scanning before declaring done → Applied to team-workflow.md
- E-003: Pattern establishment after first-of-kind stories → Applied to team-workflow.md

---

## 2. Git Summary

**Commit range:** c5ee999..2848d22 (52 commits on branch epic3/candidate-import-documents)
**Files changed:** 138
**Lines:** +12,696 / -46
**Elapsed time:** 2h18m (08:30-10:48 UTC+1, 2026-02-14)

### Commits
```
2848d22 chore(3.5): mark story done + mini-retro anti-patterns
c9ed107 fix(3.5): pass existing document to DocumentUpload in CandidateDetail
d166b26 docs(3.5): fill Dev Agent Record and mark story done
c07c68f feat(3.5): add unmatched document assignment UI to ImportSummary
699c2cf feat(3.5): add DocumentUpload component, CandidateDetail page, and candidate name links
3e627c2 feat(3.5): add frontend API types, client methods, and MSW handlers for document endpoints
1ad89af feat(3.5): add document API endpoints + integrate auto-matching into pipeline
187a10e feat(3.5): add AssignDocument and UploadDocument commands with handlers + validation
511433f feat(3.5): add DocumentMatchingEngine with auto-match logic + tests
8e482e6 feat(3.5): add ImportDocument match status methods + ImportSession coordination
c5b6cc8 feat(3.5): add Candidate.ReplaceDocument domain method + tests
3dc4199 feat(3.5): add NameNormalizer with diacritics stripping + unit tests
bf00a5e chore(3.4): mark story done + mini-retro anti-patterns
7a5abeb docs(3.4): fill Dev Agent Record, mark story done
7b85ff2 feat(3.4): wire PDF bundle into import pipeline and extend DTOs
998234a feat(3.4): add DI registration and ProcessPdfBundleCommand handler
e852327 feat(3.4): implement PdfSplitterService and BlobStorageService
bfb4874 feat(3.4): add PDF split interfaces, value objects, and NuGet packages
4c0a1dd feat(3.4): extend ImportSession with PDF progress fields and ImportDocument collection
1bdd20a feat(3.4): add ImportDocument child entity with match status tracking
9157fa9 feat(3.4): extend CandidateDocument with WorkdayCandidateId and DocumentSource
f5856e1 feat(3.4): add DocumentSource and ImportDocumentMatchStatus enums
693311c chore(3.3): mark story done + mini-retro anti-patterns
a3c50cc fix(3.3): resolve C1 matchIndex bug and I1 missing candidate operations
5a09258 docs(3.3): fill Dev Agent Record, mark story done
fa2ba4d feat(3.3): add ImportWizard container and wire into CandidateList
9bea3e5 feat(3.3): add import wizard UI components with tests
9a8161c feat(3.3): add import API client, types, MSW handlers, and query hooks
b2642e8 feat(3.3): add ResolveMatchConflict command, endpoint, and tests
03de360 feat(3.3): extend ImportSession with ConfirmMatch/RejectMatch domain methods
4f7375a chore(3.2): mark story done + mini-retro anti-patterns
340e888 fix(3.2): add authorization check to GetImportSessionQueryHandler
2b5ec2e chore: correct story 3.2 status to review (pending approval)
b3b580f docs(3.2): fill Dev Agent Record, update sprint status, add implementation plan
24b61ec feat(3.2): register import services, Channel, update EF config and column mapping
bd48fcd feat(3.2): add ImportEndpoints and ImportSessionEndpoints
35e9c68 feat(3.2): add ImportPipelineHostedService (BackgroundService + Channel)
679a19f feat(3.2): add ICandidateMatchingEngine implementation
f373853 feat(3.2): add IXlsxParser ClosedXML implementation with configurable column mapping
47dc7af feat(3.2): add GetImportSessionQuery with handler and DTO
fe29b22 feat(3.2): add StartImportCommand with validator and handler
1ecc275 feat(3.2): add ParsedCandidateRow, IXlsxParser, ICandidateMatchingEngine, ImportRequest
b9afb36 feat(3.2): add Candidate.UpdateProfile() method
c3fd987 feat(3.2): extend ImportSession with SourceFileName, row results, and detailed counts
7c089f9 feat(3.2): add ImportRowResult value object and ImportRowAction enum
3c37646 chore(3.1): mark story done + mini-retro anti-patterns
024d1d6 docs(3.1): fill Dev Agent Record section per review finding I1
906b14a chore: correct story 3.1 status to review (pending approval)
9663ba0 chore: mark story 3.1 as done in sprint status
c6fb005 feat(3.1): implement manual candidate management (full stack)
4eba1ba chore: mark Epic 2 deferred items A-006 and A-008 as done
92f64e1 fix(epic2-deferred): resolve A-006 (TeamEndpoints) and A-008 (exception handler tests)
```

### Diffstat Summary
- Backend domain: ~25 new/modified files (entities, value objects, enums, services)
- Backend application: ~35 new/modified files (commands, queries, handlers, validators, DTOs)
- Backend infrastructure: ~15 new/modified files (services, EF config, DI)
- Backend web: ~8 new/modified files (endpoints)
- Backend tests: ~25 new/modified files (unit tests)
- Frontend: ~30 new/modified files (features, hooks, API clients, components, tests, mocks)

---

## 3. Quality Signals

### Test Results
- **Domain tests:** 81+ tests pass (49 in 3.1, expanded through 3.2-3.5)
- **Application.UnitTests:** Build successfully but cannot execute locally (ASP.NET Core 10 runtime not installed — pre-existing environment limitation, not caused by this epic)
- **Frontend tests:** 190+ tests pass across 33+ files (zero regressions at each story checkpoint)
- **Test execution evidence:** Local runs verified at each story via verification-before-completion skill

### Build Results
- 0 errors, 0 warnings at each story completion checkpoint
- CI pipeline runs on push (`.github/workflows/ci.yml`)

### Code Coverage
- Not measured (pre-existing gap from Epic 2, coverage thresholds configured but no actual metrics captured)

---

## 4. Review Findings

### Story 3.1 (fix cycle — 1 round)
- **I1:** Dev Agent Record section empty — FIXED (commit 024d1d6)
- **M1:** Missing query validator for GetCandidatesQuery (situational — no user input to validate)
- **M2:** Inline closed-check vs EnsureNotClosed() (cross-aggregate case — Candidate doesn't own Recruitment)
- **M3:** No query validator tests (connected to M1)
- **M4:** record vs class on commands (spec prescribed records)

### Story 3.2 (fix cycle — 1 round)
- **C1 (CRITICAL):** GetImportSessionQueryHandler missing ITenantContext membership verification — SECURITY — FIXED (commit 340e888)
- **M1:** ImportPipelineHostedService bypasses MediatR (accepted — background service, no HTTP context)
- **M2:** OwnsMany + ToJson() for row results (design choice, not a defect)
- **M3:** No EF migration (no database in dev environment)
- **M4:** Duplicate Guid.NewGuid() in tests
- **M5:** Channel<T> unbounded (acceptable for current scale)

### Story 3.3 (fix cycle — 1 round)
- **C1 (CRITICAL):** matchIndex bug — MatchReviewStep used filtered array index instead of original full-array index when calling backend — FIXED (commit a3c50cc, computed originalIndex via .reduce())
- **I1 (IMPORTANT):** ResolveMatchConflictCommandHandler only updated ImportRowResult.Resolution string without performing actual Candidate operations (AC7: UpdateProfile on confirm, AC8: Create on reject) — FIXED (commit a3c50cc)
- **M1:** ImportRowResult changed from sealed record to sealed class (needed mutable state)
- **M2:** useEffect for state transitions (corrected from inline setState)
- **M3:** useImportSession enabled for all non-upload steps (plan only had 'processing')
- **M4:** Grammar fix in MatchReviewStep ("needs" vs "need")

### Story 3.4 (clean pass — 0C, 0I)
- **M1:** PdfPig package renamed from UglyToad.PdfPig (NuGet ID changed)
- **M2:** BookmarkNode abstract type cast required (OfType<DocumentBookmarkNode>())
- **M3:** No re-upload cleanup deferred to Story 3.5
- **M4:** Progress<T> async callback pattern (implementation detail)
- **M5:** Blob overwrite behavior (acceptable for current use)

### Story 3.5 (fix cycle — 1 round)
- **I1 (IMPORTANT):** CandidateDetail always passed existingDocument={null} because CandidateDto didn't include document data — FIXED (commit c9ed107, added DocumentDto field, added .Include(c => c.Documents))
- **M1:** NameNormalizer and DocumentMatchingEngine placed in Domain layer (pure functions, zero external deps — pragmatic deviation from strict layers)
- **M2:** No interface for stateless NameNormalizer (static class, not injectable)
- **M3:** Deferred full Assign UI dropdown/combobox (base Assign button only)
- **M4:** List cache invalidation used for detail view (no separate detail query)

---

## 5. Fix Cycle Analysis

| Story | Outcome | Fix Commits | Root Cause |
|-------|---------|-------------|------------|
| 3.1 | Fix cycle (1 round) | 024d1d6 | Empty Dev Agent Record section |
| 3.2 | Fix cycle (1 round) | 340e888 | Missing ITenantContext check on query handler (SECURITY) |
| 3.3 | Fix cycle (1 round) | a3c50cc | C1: array index bug in frontend, I1: handler incomplete (missing candidate operations) |
| 3.4 | Clean pass | — | — |
| 3.5 | Fix cycle (1 round) | c9ed107 | DTO missing document field, query missing .Include() |

**Fix cycle rate:** 4/5 stories (80%) required fix cycles. 1/5 stories (20%) passed clean.
**Critical finding rate:** 2/5 stories had critical findings (3.2 security, 3.3 logic bug).
**Pattern:** Fix cycles span multiple categories — security (3.2), logic bugs (3.3), incomplete DTOs (3.5), doc completeness (3.1).

---

## 6. Experiment Validation

### E-001: Authorization check requirement in implementation plans
- **Hypothesis:** Adding mandatory 'Authorization Check' section to plans will prevent missing ITenantContext checks
- **Success metric:** Zero authorization-related Critical findings in Epic 3 reviews
- **Result:** FAIL — Story 3.2 C1 had GetImportSessionQueryHandler missing auth, same pattern as Epic 2 Story 2.3
- **Evidence:** commit 340e888 added auth check after review caught it; the implementation plan for Story 3.2 did not include an explicit Authorization section
- **Analysis:** Experiment was applied to team-workflow.md but implementation plans were written by Dev Agent who may not have fully incorporated the requirement. The auth gap was on a *query* handler, not a command handler — suggesting the documented pattern focuses on commands but queries are also at risk.

### E-002: Anti-pattern scanning before declaring done
- **Hypothesis:** Scanning against pending.txt entries before review will catch minor findings
- **Success metric:** Minor findings per story drop from 3+ to 1 or fewer
- **Result:** INCONCLUSIVE — Dev Agent reported running anti-pattern scans (e.g., "Anti-pattern scan clean" in 3.1), but minor findings per story: 3.1=4M, 3.2=5M, 3.3=4M, 3.4=5M, 3.5=4M. Average 4.4M per story, vs 3+ target.
- **Evidence:** anti-patterns-pending.txt has only 1 regex entry (Loading text) — most minor findings are contextual and can't be caught by regex patterns
- **Analysis:** The anti-pattern scan runs but the pending.txt file has too few entries. Most minor findings are design/pattern issues, not text-matchable anti-patterns. The scan provides false confidence.

### E-003: Pattern establishment after first-of-kind stories
- **Hypothesis:** Documenting patterns after first story in new domain area reduces fix cycles in subsequent stories
- **Success metric:** Fix cycle rate drops below 40% (from 60% in Epic 2)
- **Result:** FAIL — Fix cycle rate was 80% (4/5 stories), worse than Epic 2's 60% (3/5 stories)
- **Evidence:** Stories 3.1-3.5 review outcomes show only Story 3.4 passed clean
- **Analysis:** The pattern establishment step was added to Story Completion in team-workflow.md but never triggered in practice because each story introduced new domain areas (candidates, import, UI wizard, PDF splitting, CV matching). The experiment assumes a "first of kind" story followed by similar stories, but Epic 3 stories are all distinct verticals. Also, the fix cycle issues in Epic 3 were varied (auth, logic bugs, incomplete DTOs) — not pattern establishment issues.

---

## 7. Anti-Patterns Discovered

### anti-patterns-pending.txt
```
Loading\.\.\.|web/src/features/**/*.tsx|Use SkeletonLoader component instead of plain 'Loading...' text
```

### Mini-retro comments (from anti-patterns-pending.txt):
- Story 3.1: M1-M4 situational, none warrant regex anti-patterns
- Story 3.2: C1 auth gap (E-001 failure), M1-M5 spec deviations, none warrant regex
- Story 3.3: C1 matchIndex + I1 missing ops, M1-M4 style, none warrant regex
- Story 3.4: No new anti-patterns (clean review)
- Story 3.5: I1 existingDocument null, M1-M4 pragmatic deviations, no new regex

### anti-patterns.txt (permanent)
Contains 18+ entries from previous epics covering: test framework, template cleanup, frontend patterns, domain model rules.

---

## 8. Process Observations

### Dev Agent sprint-status premature updates
- Stories 3.1 and 3.2: Dev Agent set sprint-status to `done` before review approval (corrected by Team Lead in commits 906b14a, 2b5ec2e)
- Stories 3.3-3.5: Dev Agent complied after explicit instruction not to update sprint-status

### Review process
- 2-stage review (spec compliance + code quality) via superpowers:code-reviewer subagent
- All fix cycles were 1 round only — no multi-round fix cycles
- Story 3.4 was the only clean pass, suggesting infrastructure-heavy stories (no domain logic, no UI) are lower risk

### Timing
- First commit: 2026-02-14 08:30 (deferred items)
- Last commit: 2026-02-14 10:48 (story 3.5 completion)
- Elapsed: ~2h18m for 52 commits, 138 files, 12.7K lines
- Average: ~10 commits/story, ~27 min/story

---

## 9. Guideline References

- `_bmad-output/planning-artifacts/architecture.md` — Core decisions, aggregate boundaries
- `_bmad-output/planning-artifacts/architecture/patterns-backend.md` — Backend patterns (updated with auth pattern in E-001)
- `_bmad-output/planning-artifacts/architecture/api-patterns.md` — Minimal API patterns
- `_bmad-output/planning-artifacts/architecture/patterns-frontend.md` — Frontend patterns
- `_bmad-output/planning-artifacts/architecture/frontend-architecture.md` — Component structure
- `_bmad-output/planning-artifacts/architecture/testing-standards.md` — Test patterns
- `.claude/process/team-workflow.md` — Development process (updated with E-001, E-002, E-003)
- `docs/testing-pragmatic-tdd.md` — Testing policy

---

## 10. Sprint-Status Snapshot

```yaml
epic-3: in-progress
3-1-manual-candidate-management: done
3-2-xlsx-import-pipeline: done
3-3-import-wizard-summary-ui: done
3-4-pdf-bundle-upload-splitting: done
3-5-cv-auto-match-manual-assignment-individual-upload: done
epic-3-retrospective: optional

# Epic 2 deferred items (resolved at start of this sprint):
epic-2-deferred-team-endpoints: done
epic-2-deferred-exception-integration-tests: done

# Unresolved from Epic 2:
# A-007: CloseRecruitment returns 200 instead of 204 — NOT RESOLVED
```
