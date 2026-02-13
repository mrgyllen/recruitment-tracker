# Retrospective: Epic 1 Stories 1.1-1.3
**Run ID:** 2026-02-13T002800Z
**Scope:** Project Scaffolding, SSO Authentication, Core Data Model & Tenant Isolation
**Lenses:** Delivery, QA, Architecture, Docs, Security

---

## Wins

1. **DDD aggregate boundaries correctly enforced** — All child entities use internal factory methods, private constructors. Cross-aggregate references are IDs only. Zero bypasses observed. (Architecture, Delivery)

2. **Defense-in-depth tenant isolation** — Global query filter with three-tier access model (service/recruitment/user). 8 Testcontainers integration tests cover all mandatory security scenarios. (Security, QA, Architecture)

3. **Anti-pattern hooks catching violations** — Hooks caught AutoMapper, Shouldly, [NotMapped]. 4 patterns added to pending.txt. Self-improving system. (Delivery, Docs)

4. **Architecture docs matched implementation** — 8 entities, 3 aggregates, testing stack, project structure, dev auth — all implemented as documented. Low spec-to-code drift. (Docs, Architecture)

5. **Review process caught critical bugs** — Missing creator-removal invariant, missing security tests, non-JSON error crash — all caught and fixed before approval. (QA, Security)

---

## Action Items

### P0 — Apply Immediately

| ID | Type | Title | Status |
|----|------|-------|--------|
| A1 | security_hardening | Remove AddDefaultIdentity (3 lenses flagged) | Apply now |
| A2 | docs_update | Document Fluent API for domain event collections | Apply now |

### P1 — Apply Now (Low-Risk Edits)

| ID | Type | Title | Status |
|----|------|-------|--------|
| A3 | docs_update | Add template cleanup checklist | Apply now |
| A4 | docs_update | Document middleware pipeline ordering | Apply now |
| A5 | process_change | Enforce Dev Agent Record completion | Apply now |
| A6 | architecture_alignment | Consolidate IUser/ICurrentUserService | Defer (code change) |
| A7 | guideline_gap | Promote anti-patterns to permanent | Apply now |

### P2 — Deferred to Next Sprint

| ID | Type | Title |
|----|------|-------|
| A8 | refactor | Fix Guid.ToString() type mismatch in query filter |
| A9 | test_gap | Add domain event payload verification tests |
| A10 | docs_update | Document CORS policy and same-origin assumption |

---

## Recurring Patterns

| Pattern | Stories | Root Cause | Action |
|---------|---------|------------|--------|
| Template cleanup findings | 1.1, 1.2, 1.3 | No cleanup checklist for Jason Taylor template | A3: Add checklist |
| Empty Dev Agent Record | 1.2, 1.3 | Section purpose undocumented | A5: Document + enforce |
| Data annotation in domain | 1.3 | Docs say "no annotations" but don't show alternative | A2: Add Fluent API example |

---

## Missing Evidence / Instrumentation Gaps

- CI test execution logs (tests compile but no evidence of execution in CI)
- Code coverage reports and baselines
- Build/test timing metrics
- Domain event dispatch telemetry
- Anti-pattern hook effectiveness metrics (caught by hook vs manual review)

---

## Deferred to Next Sprint

- **A6:** Consolidate IUser/ICurrentUserService — requires touching 6 files across layers, better as dedicated refactoring task
- **A8:** Fix Guid.ToString() type mismatch — performance concern, not blocking correctness
- **A9:** Domain event payload verification — tests exist but don't verify event data
- **A10:** CORS policy documentation — same-origin deployment works, document the assumption
