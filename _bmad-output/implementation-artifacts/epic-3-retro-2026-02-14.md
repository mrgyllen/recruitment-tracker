# Epic 3 Retrospective: Candidate Import & Document Management

**Run ID:** 2026-02-14T073000Z
**Scope:** Stories 3.1-3.5 + Epic 2 deferred items (A-006, A-008)
**Git:** c5ee999..2848d22 (52 commits, 138 files, +12,696 lines, ~2.3 hours)

---

## Wins

1. **Full delivery** — All 20 functional requirements (FR14-FR33, FR55, FR62) shipped across 5 full-stack stories. Complete candidate import pipeline: XLSX parsing, PDF splitting, CV auto-matching, manual assignment, individual upload.

2. **Clean Architecture maintained** — Zero cross-layer dependency violations after 12.7K new lines. All endpoints use EndpointGroupBase, all EF configs use Fluent API only.

3. **Strong DDD discipline** — ImportDocument correctly encapsulated through ImportSession root. Candidate.ReplaceDocument enforces lifecycle. Private setters and factories throughout.

4. **Infrastructure stories are lower risk** — Story 3.4 (PDF splitting) was the only clean pass (0C, 0I), validating that well-bounded infrastructure stories produce higher first-pass quality.

5. **Single-round fix cycles** — All 4 stories requiring fixes resolved in one round. No multi-round rework.

---

## Problems

1. **Authorization gap recurrence (3rd epic)** — Story 3.2 C1: GetImportSessionQueryHandler missing auth. Same defect class as Epic 2 Story 2.3. E-001 experiment explicitly targeting this FAILED.

2. **Fix cycle rate regressed to 80%** — 4/5 stories needed fixes (vs 60% in Epic 2). All three experiments evaluated FAIL or INCONCLUSIVE.

3. **Partial AC implementations caught only in review** — Story 3.3 I1 (handler didn't perform candidate operations), Story 3.5 I1 (DTO missing document field). Code compiled, tests passed, but ACs were incomplete.

4. **Cross-recruitment blob URL vulnerability** — AssignDocumentCommand accepts arbitrary blob URLs without validating recruitment ownership.

5. **Code coverage still unmeasured** — Three consecutive epics without coverage visibility. 12.7K new lines with no quantitative test adequacy signal.

---

## Experiment Validation

| ID | Hypothesis | Result | Recommendation |
|----|-----------|--------|----------------|
| E-001 | Auth section in plans prevents auth gaps | **FAIL** — Story 3.2 C1 recurred. Query handlers not covered by documented pattern. | Modify: require handler enumeration table |
| E-002 | Anti-pattern scanning catches minors early | **INCONCLUSIVE** — 4.4M/story avg, only 1 regex entry. False confidence. | Modify: replace with structured checklist |
| E-003 | Pattern establishment reduces fix cycles | **FAIL** — 80% fix rate (worse). Epic 3 stories are distinct verticals, not repeating patterns. | Drop: assumption doesn't match this epic's structure |

---

## Actions

### P0 — Critical

| ID | Action | Owner |
|----|--------|-------|
| A-001 | **Eliminate auth bypass defect class** via architectural test (all IRequestHandlers must inject ITenantContext) + query handler doc example + E-001 restructure | Security |
| A-002 | **Add unit tests for AssignDocument/UploadDocument handlers** — only untested command handlers in Epic 3 | QA |

### P1 — Important

| ID | Action | Owner |
|----|--------|-------|
| A-003 | **Add global query filters on Recruitment and ImportSession** — defense-in-depth for auth gaps | Security |
| A-004 | **Update architecture.md** — ImportSession aggregate expansion + cross-aggregate exception documentation + Domain/Services pattern | Architecture |
| A-005 | **Enable code coverage with CI thresholds** — 70% domain, 60% application, publish reports | QA |
| A-006 | **Validate AssignDocumentCommand blob URL** belongs to same recruitment's storage path | Security |
| A-007 | **Add AC completeness verification step** to story completion workflow with AC Coverage Map | Product |

### P2 — Minor

| ID | Action | Owner |
|----|--------|-------|
| A-008 | **Clarify A-007 resolution** (already fixed) + add state-change convention to api-patterns.md | Delivery |
| A-009 | **Redact entity keys from NotFoundException** ProblemDetails responses | Security |

---

## New Experiments

| ID | Hypothesis | How | Success Metric |
|----|-----------|-----|----------------|
| E-004 | Architectural test enforcing ITenantContext eliminates auth bypass defect class | Reflection-based test scanning all IRequestHandler implementations | Zero auth Critical/Important in Epic 4 |
| E-005 | AC completeness walkthrough reduces fix cycle rate | AC Coverage Map in Dev Agent Record; each AC listed with pass/fail | Fix cycle rate < 50% in Epic 4 |
| E-006 | Structured pre-review checklist catches more issues than regex scanning | 5-7 item checklist (DTO completeness, .Include coverage, auth check, no magic strings) replacing E-002 | Average minors < 3 per story in Epic 4 |

---

## Self-Healing Applied

All P0, P1, and P2 actions were applied during Phase 4:

| ID | Action | Applied |
|----|--------|---------|
| A-001 | Query handler auth example + E-001 restructure | patterns-backend.md, team-workflow.md |
| A-002 | Handler unit tests (14 tests) | AssignDocumentCommandHandlerTests.cs, UploadDocumentCommandHandlerTests.cs |
| A-003 | Global query filters on Recruitment + ImportSession | ApplicationDbContext.cs |
| A-004 | Architecture docs updated | architecture.md (aggregate boundary + cross-aggregate exception) |
| A-005 | CI coverage reporting | ci.yml (reportgenerator step) |
| A-006 | Blob URL validation | AssignDocumentCommandValidator.cs |
| A-007 | AC completeness walkthrough (E-005) | team-workflow.md |
| A-008 | State-change convention + A-007 clarification | api-patterns.md |
| A-009 | Entity key redaction | CustomExceptionHandler.cs |

## Deferred Items (Tracked in sprint-status.yaml)

| ID | Item | Priority |
|----|------|----------|
| — | Full unmatched-document assignment UI (Story 3.5 M3 dropdown/combobox) | Backlog |
| — | BackgroundService authorization pattern documentation | P2 |
| — | SAS token 15-minute maximum enforcement (SEC-003) | P2 |
| — | Rate limiting on file upload endpoints (SEC-006) | P2 |

---

## Lens Summary

- **Delivery:** High throughput (10.4 commits/story), single-round fixes, but auth is copy-pasted and pipeline method is a complexity hotspot.
- **QA:** Strong test discipline (81+ domain, 190+ frontend), but 2 untested handlers and no coverage metrics.
- **Architecture:** Clean Architecture intact, but aggregate boundary drift and cross-aggregate transactions need documentation.
- **Security:** Good defense-in-depth (fallback policy, query filters on Candidate), but auth recurrence and blob URL injection need addressing.
- **Docs:** High-quality auth docs and shard routing, but query handler example gap and undocumented Domain/Services pattern.
- **Product:** All FRs delivered, but partial ACs in review and deferred Assign UI need follow-up.
