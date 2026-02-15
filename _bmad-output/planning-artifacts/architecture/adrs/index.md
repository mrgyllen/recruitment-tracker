# Architecture Decision Records

Decision log for architectural changes made during implementation. ADRs capture the *what* and *why* of changes. The architecture shards reflect the current state and are the single source of truth for agents.

**Rule:** ADR creation and shard updates must ship in the same commit.

## Decision Log

| # | Title | Date | Status | Shards Updated |
|---|-------|------|--------|----------------|
| 001 | [Test Pyramid and E2E Decomposition](ADR-001-test-pyramid-e2e-decomposition.md) | 2026-02-15 | Accepted | `testing-standards.md`, `prd.md`, `team-workflow.md`, `testing-pragmatic-tdd.md`, `ci.yml`, `cd.yml` |

## ADR Template

New ADRs should follow this format (copy to `ADR-NNN-short-title.md`):

```markdown
# ADR-NNN: Title

## Status
Proposed | Accepted | Superseded by ADR-NNN | Deprecated

## Context
What situation or problem prompted this decision?

## Decision
What did we decide?

## Consequences
What follows from this decision? Both positive and negative.

## Shards Updated
- `shard-name.md` — Section: Section Name
```
