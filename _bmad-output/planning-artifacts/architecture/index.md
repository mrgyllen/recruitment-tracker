# Architecture Specification — recruitment-tracker

## Quick Reference: What to Load

| Your Task | Load These |
|-----------|-----------|
| Any backend story | `architecture.md` (core) + `patterns-backend.md` |
| Any frontend story | `architecture.md` (core) + `patterns-frontend.md` |
| Full-stack story | `architecture.md` (core) + `patterns-backend.md` + `patterns-frontend.md` |
| New API endpoint | Add `api-patterns.md` |
| Frontend feature with new views | Add `frontend-architecture.md` |
| Screening/batch processing UI | Add `frontend-architecture.md` (Batch Screening Architecture, PDF Viewing sections) |
| Creating new files/folders | Add `project-structure.md` |
| Any story with tests | Add `testing-standards.md` |
| E2E scenario planning | `architecture.md` (core) + `testing-standards.md` |
| Auth-related work | Add `dev-auth-patterns.md` |
| DevOps/deployment | Add `infrastructure.md` |
| Project scaffolding (Story 1.1 only) | Add `starter-templates.md` |
| Understanding past architectural changes | See `adrs/index.md` |

## Table of Contents

### Core (Always Load)

- [Architecture Decision Document](../architecture.md) — Project context, core decisions, data architecture (aggregates, ubiquitous language, ITenantContext), authentication & security, enforcement guidelines

### Topic Shards (Load as Needed)

- [Starter Templates](./starter-templates.md) — Template evaluation, initialization commands, Vite/React setup details
  - [Primary Technology Domain](./starter-templates.md#primary-technology-domain)
  - [Selected Backend Starter](./starter-templates.md#selected-backend-starter-jason-taylor-clean-architecture)
  - [Selected Frontend Starter](./starter-templates.md#selected-frontend-starter-official-vite--react--typescript)
  - [Development Workflow](./starter-templates.md#development-workflow)
  - [CI/CD Pipeline Strategy](./starter-templates.md#cicd-pipeline-strategy)
- [Development Auth Patterns](./dev-auth-patterns.md) — Dev auth bypass, personas, frontend/backend dual-path auth
  - [Frontend Dev Auth Mode](./dev-auth-patterns.md#frontend--dev-auth-mode)
  - [Backend Dev Auth Handler](./dev-auth-patterns.md#backend--dev-auth-handler)
  - [Preconfigured Personas](./dev-auth-patterns.md#preconfigured-personas)
- [API Patterns](./api-patterns.md) — Endpoint patterns, async operations, error responses, API response formats
  - [Key Endpoint Patterns](./api-patterns.md#key-endpoint-patterns)
  - [Async Operations](./api-patterns.md#async-operations)
  - [Error Responses](./api-patterns.md#error-responses)
  - [API Response Formats](./api-patterns.md#api-response-formats)
  - [Data Formats](./api-patterns.md#data-formats)
- [Frontend Architecture](./frontend-architecture.md) — Framework decisions, state management, folder structure, httpClient.ts, batch screening, PDF viewing
  - [Framework & Libraries](./frontend-architecture.md#framework--libraries)
  - [State Management](./frontend-architecture.md#state-management)
  - [Folder Structure](./frontend-architecture.md#folder-structure)
  - [HTTP Client Foundation](./frontend-architecture.md#http-client-foundation-pattern)
  - [Batch Screening Architecture](./frontend-architecture.md#batch-screening-architecture)
  - [PDF Viewing](./frontend-architecture.md#pdf-viewing)
- [Patterns — Backend](./patterns-backend.md) — C# naming, project structure, DTO mapping, error handling, test conventions, MediatR events, audit events
  - [Naming Patterns](./patterns-backend.md#naming-patterns)
  - [Structure Patterns](./patterns-backend.md#structure-patterns)
  - [DTO Mapping](./patterns-backend.md#dto-mapping)
  - [Error Handling](./patterns-backend.md#error-handling)
  - [Test Conventions](./patterns-backend.md#test-conventions)
  - [MediatR Domain Events](./patterns-backend.md#mediatr-domain-events)
  - [Audit Events](./patterns-backend.md#audit-events)
- [Patterns — Frontend](./patterns-frontend.md) — TypeScript/React naming, component structure, loading states, empty states, validation, UI consistency rules
  - [Naming Patterns](./patterns-frontend.md#naming-patterns)
  - [Component Structure](./patterns-frontend.md#component-structure)
  - [Loading States](./patterns-frontend.md#loading-states)
  - [Empty State Pattern](./patterns-frontend.md#empty-state-pattern)
  - [Validation Timing](./patterns-frontend.md#validation-timing)
  - [UI Consistency Rules](./patterns-frontend.md#ui-consistency-rules)
- [Project Structure](./project-structure.md) — Complete directory tree, architectural boundaries, requirements-to-structure mapping, integration points, file organization
  - [Complete Directory Structure](./project-structure.md#complete-project-directory-structure)
  - [Architectural Boundaries](./project-structure.md#architectural-boundaries)
  - [Requirements to Structure Mapping](./project-structure.md#requirements-to-structure-mapping)
  - [Integration Points](./project-structure.md#integration-points)
  - [File Organization Patterns](./project-structure.md#file-organization-patterns)
  - [Development Workflow Integration](./project-structure.md#development-workflow-integration)
- [Infrastructure](./infrastructure.md) — Hosting, CI/CD, environment config, background processing, deployment
  - [Hosting](./infrastructure.md#hosting)
  - [CI/CD](./infrastructure.md#cicd)
  - [Environment Configuration](./infrastructure.md#environment-configuration)
  - [Background Processing](./infrastructure.md#background-processing)
- [Testing Standards](./testing-standards.md) — Test frameworks (NUnit, NSubstitute, Vitest, MSW), naming conventions, Testcontainers, mandatory security tests, pragmatic TDD modes, test pyramid, E2E decomposition, contract tests, post-deployment smoke tests
  - [Test Frameworks](./testing-standards.md#test-frameworks)
  - [Test Naming Convention](./testing-standards.md#test-naming-convention)
  - [Integration Tests with Testcontainers](./testing-standards.md#integration-tests-with-testcontainers)
  - [Mandatory Security Test Scenarios](./testing-standards.md#mandatory-security-test-scenarios)
  - [Pragmatic TDD Modes](./testing-standards.md#pragmatic-tdd-modes)
  - [Test Pyramid Layers](./testing-standards.md#test-pyramid-layers) _(ADR-001)_
  - [E2E Decomposition Method](./testing-standards.md#e2e-decomposition-method) _(ADR-001)_
  - [Contract Tests](./testing-standards.md#contract-tests) _(ADR-001)_
  - [Post-Deployment Smoke Tests](./testing-standards.md#post-deployment-smoke-tests) _(ADR-001)_
- [Validation Results](./validation-results.md) — Coherence validation, requirements coverage, gap analysis, completeness checklist, readiness assessment (historical reference)

### Architecture Decision Records

- [ADR Index](./adrs/index.md) — Decision log for architectural changes made during implementation
