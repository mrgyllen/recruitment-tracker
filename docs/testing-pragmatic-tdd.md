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

- **Unit tests (majority):** domain rules, workflow transitions, validations, data transformations
- **Integration tests (some):** API endpoints + authorization, persistence behavior, key workflows; include denial paths and validation errors
- **E2E/UI smoke tests (few):** critical happy-path flows only

### Definition of Done (Testing)

- Domain/application changes have test coverage including at least one negative case (invalid input and/or unauthorized access)
- Integration tests cover authz for sensitive endpoints and at least one end-to-end workflow
- All tests run in CI and pass
- Test data must not contain real PII
- Developer declared testing mode (test-first / spike / characterization) per task and documented the rationale
- Before finishing: list tests added and what risk they cover

### References

- Story template: `_bmad/bmm/workflows/4-implementation/create-story/template.md`
- DoD checklist: `_bmad/bmm/workflows/4-implementation/dev-story/checklist.md`
- Code review checklist: `_bmad/bmm/workflows/4-implementation/code-review/checklist.md`
- Agent customization: `_bmad/_config/agents/bmm-dev.customize.yaml`
