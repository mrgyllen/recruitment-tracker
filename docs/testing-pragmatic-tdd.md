# Testing & Pragmatic TDD Policy

This is the canonical testing policy for recruitment-tracker. All stories, agents, and reviews reference this document.

## Pragmatic TDD

This project follows **Pragmatic TDD** — not strict red-green-refactor for every line, but a disciplined, risk-aware approach to testing.

### Modes

Before coding any task, the developer declares which testing mode applies and why:

| Mode | When to Use | Expectation |
|------|------------|-------------|
| **Test-First** | Clear/stable behavior, business rules, domain logic, validation | Write failing test → implement → green → refactor |
| **Spike / Prototype** | High uncertainty, exploring APIs, unvalidated data formats, new libraries | Code first to learn; add tests before merge |
| **Characterization** | Unclear or legacy behavior, third-party integration quirks | Write tests that capture observed behavior before changing it |

### Principles

1. **Prefer test-first** for clear/stable behavior and business rules
2. **Allow short spikes/prototypes** when uncertainty is high — add tests before merge
3. **Use characterization tests** when behavior is unclear or legacy
4. **Prioritize tests for high-risk paths and regressions** — avoid low-value micro-tests
5. **Refactor with safety:** tests (preferred) or explicit alternative verification noted in the story

### Test Pyramid

Five layers, from fastest/most-numerous to slowest/fewest. See [`testing-standards.md`](./../_bmad-output/planning-artifacts/architecture/testing-standards.md#test-pyramid-layers) for full details, the layer selection decision tree, and the handler functional test coverage rule.

| Layer | Volume | What It Proves |
|-------|--------|----------------|
| **Unit** | Majority | Domain rules, workflow transitions, validations, handler logic (mocked infra) |
| **Contract** | Per-endpoint | Frontend DTO ↔ Backend DTO structural alignment |
| **Integration** | Security-critical | EF Core mappings, query filters, tenant isolation against real SQL |
| **Functional** | Per-complex-handler | Full HTTP pipeline (routing → auth → MediatR → EF Core → SQL) |
| **E2E** | Few | Browser-based user journeys — only when lower tests can't cover the risk |

**Key rule:** Any handler using `.Include()`, `.Select()`, `.GroupBy()`, or `.Where()` with navigation properties MUST have a functional test against real SQL. Unit tests with mocked `IApplicationDbContext` silently pass LINQ expressions that fail at runtime.

### Definition of Done (Testing)

- Domain/application changes have test coverage including at least one negative case (invalid input and/or unauthorized access)
- Integration tests cover authz for sensitive endpoints and at least one end-to-end workflow
- All tests run in CI and pass
- Test data must not contain real PII
- Developer declared testing mode (test-first / spike / characterization) per task and documented the rationale
- Before finishing: list tests added and what risk they cover

### E2E Decomposition

E2E scenarios are defined upfront from PRD user journeys and documented in [`docs/e2e-scenarios.md`](./e2e-scenarios.md). Each scenario is decomposed into lower-level tests (unit, contract, integration, functional) that collectively cover the risk. Automated browser-based E2E tests are added only when no combination of lower tests suffices.

See [`testing-standards.md` — E2E Decomposition Method](./../_bmad-output/planning-artifacts/architecture/testing-standards.md#e2e-decomposition-method) for the full methodology, registry format, and the four criteria for adding E2E tests.

### Test Layer Declaration

Implementation plans now require a **Test Layer Map** — a table mapping each handler/component to required test layers (Unit, Contract, Functional) with a "Why" column citing the decision tree from `testing-standards.md`.

- **TDD mode** (test-first / spike / characterization) declares **HOW** to write tests
- **Test layer** (unit / contract / integration / functional / E2E) declares **WHERE** to write them

Both are required. A plan that declares TDD mode but omits the test layer map is incomplete.

See [`.claude/process/team-workflow.md`](./../.claude/process/team-workflow.md) Step 1 for the full requirement.

### References

- Story template: `_bmad/bmm/workflows/4-implementation/create-story/template.md`
- DoD checklist: `_bmad/bmm/workflows/4-implementation/dev-story/checklist.md`
- Code review checklist: `_bmad/bmm/workflows/4-implementation/code-review/checklist.md`
- Agent customization: `_bmad/_config/agents/bmm-dev.customize.yaml`
