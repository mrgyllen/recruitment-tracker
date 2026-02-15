# ADR-001: Test Pyramid and E2E Decomposition

## Status

Accepted

## Context

After 7 epics and 425+ green unit tests, the first live integration test against real SQL Server (2026-02-15) revealed an EF Core query bug in `GetRecruitmentOverviewQueryHandler`. The `.Select()` projection referenced navigation properties that weren't loaded, producing a valid C# expression that NSubstitute happily evaluated in-memory but that EF Core's LINQ-to-SQL translator could not translate.

All unit tests passed because they mock `IApplicationDbContext` with NSubstitute, which evaluates LINQ expressions against in-memory collections — bypassing LINQ-to-SQL translation, global query filters, FK constraints, and SQL-specific behavior entirely.

Three consecutive blocked demos (Epics 4, 5, 7) were symptoms of the same root cause: the project had no guidance on when to use functional tests (WebApplicationFactory + Testcontainers) versus unit tests (NSubstitute mocks). The existing test infrastructure was adequate — `Application.FunctionalTests` and `Infrastructure.IntegrationTests` both use Testcontainers — but no decision tree or policy existed to direct developers (or agents) to the correct test layer.

The PRD contained zero testing NFRs, so BMAD epics inherited no testing requirements. The team-workflow story checklist asked for TDD mode (test-first vs. spike vs. characterization) but not test layer (unit vs. integration vs. functional). The result: every handler got unit tests with mocked `IApplicationDbContext`, and none got functional tests against real SQL.

## Decision

### 1. Five-Layer Test Pyramid

Define five test layers with clear ownership, speed expectations, and selection criteria:

| Layer | Project | Speed | What It Proves |
|-------|---------|-------|----------------|
| Unit | `Domain.UnitTests`, `Application.UnitTests` | < 1s each | Business logic, validation, domain rules in isolation |
| Contract | `Application.UnitTests/Contracts/` | < 1s each | Frontend-backend DTO structural alignment |
| Integration | `Infrastructure.IntegrationTests` | 2-5s each | EF Core mappings, query filters, Testcontainers SQL |
| Functional | `Application.FunctionalTests` | 3-10s each | Full HTTP pipeline via WebApplicationFactory + real SQL |
| E2E | `Web.AcceptanceTests` (SpecFlow/Playwright) | 10-30s each | Browser-based user journeys (used sparingly) |

### 2. Handler Functional Test Coverage Rule

Any query or command handler using EF Core LINQ features that cannot be fully validated in-memory MUST have at least one functional test running through `WebApplicationFactory` + Testcontainers against real SQL Server. Specifically, handlers using `.Include()`, `.Where()` with navigation properties, `.Select()` projections, `.GroupBy()`, or complex LINQ expressions require functional tests.

### 3. E2E Decomposition Method

E2E scenarios are defined upfront from user journeys (documented in `docs/e2e-scenarios.md`) but decomposed into lower-level tests that collectively cover the risk. Automated browser-based E2E tests are added only when no combination of unit + integration + functional tests can cover the scenario.

### 4. Contract Tests

Structural DTO verification tests in `Application.UnitTests/Contracts/` verify that backend response DTOs match expected property names and types. Required for every endpoint that has a corresponding MSW handler in the frontend.

### 5. Testing NFRs

Seven new NFRs (NFR41-NFR47) added to the PRD ensure future BMAD epics inherit testing requirements.

### 6. Test Layer Declaration in Story Workflow

Implementation plans now require a "Test Layer Map" table mapping each handler/component to required test layers with justification. TDD mode declares HOW to write tests; test layer declares WHERE to write them.

## Consequences

**Positive:**
- EF Core LINQ-to-SQL translation bugs caught before demo, not during
- Agents have a deterministic decision tree for selecting test layers
- E2E scenarios documented and traceable to lower-level test coverage
- PRD NFRs propagate testing requirements to all future epics
- Post-deployment smoke tests verify auth enforcement after each CD deployment

**Negative:**
- Functional tests are slower (3-10s vs. <1s for unit tests)
- CI pipeline time may increase as functional test count grows (mitigated by Testcontainers container reuse)
- Developers must maintain the E2E scenario registry alongside test code
- Contract tests add maintenance burden when DTOs change (mitigated by keeping tests structural, not snapshot-based)

## Shards Updated

- `testing-standards.md` — Sections: Test Pyramid Layers, E2E Decomposition Method, Contract Tests, Post-Deployment Smoke Tests
- `../../../docs/testing-pragmatic-tdd.md` — Sections: Test Pyramid (expanded), E2E Decomposition, Test Layer Declaration
- `../../../_bmad-output/planning-artifacts/prd.md` — NFR41-NFR47
- `../../../.claude/process/team-workflow.md` — Test Layer Map requirement, Post-Epic Integration Verification phase
- `../../../.github/workflows/ci.yml` — Clarifying comment on test layers
- `../../../.github/workflows/cd.yml` — Post-deployment auth smoke test
- `index.md` (architecture) — Updated TOC and routing table
