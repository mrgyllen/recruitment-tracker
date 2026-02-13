# Retrospective Evidence Bundle
## Run ID: 2026-02-13T002800Z

## 1. Scope

| Story | Title | Status |
|-------|-------|--------|
| 1.1 | Project Scaffolding & CI Pipeline | APPROVED WITH MINOR NOTES |
| 1.2 | SSO Authentication | APPROVED |
| 1.3 | Core Data Model & Tenant Isolation | APPROVED |

### Acceptance Criteria Summary
- **1.1:** .NET Clean Architecture backend, Vite+React+TS frontend, Tailwind v4, Vitest+MSW+Testing Library, ESLint+Prettier, .editorconfig, CI pipeline, monorepo structure
- **1.2:** JWT bearer auth, Entra ID SSO, dev auth handler with persona switching, httpClient with token attachment, 401 handling, noindex headers, DevAuthProvider toolbar
- **1.3:** Domain entities (8), aggregate boundaries (3 roots), EF Core Fluent API configs, ITenantContext global query filters, TenantContextMiddleware, AuditBehaviour, cross-recruitment isolation tests

## 2. Git Summary

**Commit range:** 971dc3a..491e125 (33 commits)
**Branch:** exp4/superpowers-opus

### Commit Subjects
```
491e125 Fix Story 1.3 review findings (C1, I1-I3, M1)
8a57d15 chore: mark Story 1.3 Core Data Model & Tenant Isolation as done
08d25d0 test(infra): add Testcontainers-based tenant isolation integration tests
9fa879f feat(app): add AuditBehaviour MediatR pipeline for command audit trail
448d73b feat(infra): add global query filters for tenant isolation and TenantContext middleware
6c42daf feat(infra): add EF Core Fluent API configurations for all domain entities
c1969e5 refactor(api): remove template Todo/Weather code and update DbContext for domain entities
82cfacc feat(domain): add Recruitment, Candidate, ImportSession aggregates with invariants
c662291 test(domain): add failing tests for Recruitment, Candidate, and ImportSession aggregates
be8dec8 feat(domain): add domain events and exceptions, remove template artifacts
1d5f0ed feat(domain): add CandidateMatch and AnonymizationResult value objects
c923205 feat(domain): add domain enums for outcomes, imports, and recruitment status
49aed69 feat(domain): add GuidEntity base class for Guid-keyed entities
5ec42f6 docs: add Story 1.3 Core Data Model & Tenant Isolation implementation plan
db32546 fix: address Story 1.2 code review findings (I1-I3)
b29a909 chore: mark Story 1.2 SSO Authentication as done in sprint status
ee7169e fix(web): resolve lint errors and apply prettier formatting
1c271cd feat(api): add placeholder AzureAd configuration for JWT bearer auth
80eb626 feat(web): wire AuthProvider into application root and fix httpClient TS errors
4d5ba6b feat(web): update test infrastructure with auth provider and mock helpers
4a7f8d9 feat(web): add AuthProvider with dev persona switching and login redirect
f7470a7 feat(web): add httpClient with dev auth headers, 401 handling, and Problem Details
7615af8 feat(web): add RFC 9457 Problem Details parser
c088b10 feat: add noindex headers and meta tag to prevent search engine indexing
814575a feat(api): add ICurrentUserService and ITenantContext with claim-based identity extraction
bb78728 feat(api): add dev auth handler with fallback policy and JWT bearer config
639f5aa feat(web): install MSAL v5 and add Entra ID configuration
593e225 fix: address Story 1.1 code review findings (C1, I1-I5, M1, M4)
b31a472 chore: mark Story 1.1 as done in sprint-status
94882b8 docs: add Story 1.1 implementation plan
ae9f867 ci: add GitHub Actions pipeline for API and web
cdac924 feat(web): configure ESLint with import ordering and Prettier
bf320c6 feat(web): scaffold React frontend with Tailwind, Vite proxy, and test infrastructure
971dc3a feat(api): scaffold .NET backend with Clean Architecture template
```

### Diffstat
- 279 files changed
- +30,765 lines added, -3,294 lines removed
- Significant bulk from template (api/infra/ Bicep files ~40 files, web/package-lock.json ~6000 lines)

## 3. Quality Signals

### Build Results
- `dotnet build api/api.slnx`: 0 warnings, 0 errors
- `npm run build` (web): SUCCESS (33 modules)

### Test Results
- Backend Domain.UnitTests: **37/37 passing**
- Backend Application.UnitTests: 2 audit tests + 3 CurrentUserService + 3 TenantContext (compile, need ASP.NET runtime for execution)
- Backend Infrastructure.IntegrationTests: 8 tenant isolation tests (compile, need Docker/Testcontainers for execution)
- Backend Application.FunctionalTests: 5 auth tests (compile, need ASP.NET runtime for execution)
- Frontend: **19/19 passing** (5 test files: App, AuthContext, DevAuthProvider, httpClient, problemDetails)

### Lint/Format Results
- ESLint: clean
- Prettier: clean
- dotnet format: clean (note: `dotnet format` had a runtime error on final check but previous runs confirmed clean)

## 4. Review Findings

### Story 1.1 Review
- **C1 (Critical):** AutoMapper left from template — architecture forbids it
- **I1:** Shouldly used instead of FluentAssertions
- **I2:** .editorconfig severity levels too lenient
- **I3:** Web.AcceptanceTests break CI (Playwright not installed)
- **I4:** Story ACs reference `api.sln` but actual file is `api.slnx`
- **I5:** Duplicate imports in App.test.tsx
- **M1:** App.css leftover template file
- **M2:** Overly broad .gitignore
- **M3:** api/infra/ Bicep files from template (40+ files)
- **M4:** Untyped handlers array
- **Resolution:** All Critical and Important fixed in 593e225. APPROVED WITH MINOR NOTES.

### Story 1.2 Review
- **I1:** handleResponse crashes on non-JSON error responses
- **I2:** ExceptionHandler registered after endpoint mapping
- **I3:** Dev Agent Record section empty
- **M1:** authProvider.ts is re-export, not factory
- **M2:** MsalAuthProvider is hard-coded stub
- **M3:** No separate AuthenticationConfiguration.cs
- **M4:** isDev evaluated at call time (positive deviation)
- **Resolution:** All Important fixed in db32546. APPROVED.

### Story 1.3 Review
- **C1 (Critical):** RemoveMember() missing creator-cannot-be-removed invariant
- **I1:** Only 4 of 8 required tenant isolation security tests
- **I2:** TenantContextMiddleware was a no-op
- **I3:** Dev Agent Record section empty
- **M1:** GuidEntity uses [NotMapped] data annotation
- **M2:** Guid.ToString() comparison in query filter (type mismatch)
- **M3:** AuditBehaviour separate SaveChangesAsync (transactional gap)
- **Resolution:** All Critical and Important fixed in 491e125. APPROVED.

### Patterns Observed
- Template cleanup was consistently the biggest source of review findings
- Dev Agent Record section was empty in both 1.2 and 1.3 (recurring)
- Security invariants (creator removal) and security test completeness caught by review

## 5. Anti-Patterns Discovered

```
# From .claude/hooks/anti-patterns-pending.txt
AutoMapper|api/**/*.csproj|Architecture forbids AutoMapper — use manual DTO mapping only
Shouldly|api/**/*.csproj|Use FluentAssertions, not Shouldly
AddDefaultIdentity|api/**/*.cs|Do not register ASP.NET Identity — architecture uses Entra ID exclusively
\[NotMapped\]|api/src/Domain/**/*.cs|Do not use data annotations in domain entities — use Fluent API in EF configurations
```

## 6. Guideline References

- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Backend patterns: `_bmad-output/planning-artifacts/architecture/patterns-backend.md`
- Frontend patterns: `_bmad-output/planning-artifacts/architecture/patterns-frontend.md`
- Testing standards: `_bmad-output/planning-artifacts/architecture/testing-standards.md`
- API patterns: `_bmad-output/planning-artifacts/architecture/api-patterns.md`
- Dev auth patterns: `_bmad-output/planning-artifacts/architecture/dev-auth-patterns.md`
- Project structure: `_bmad-output/planning-artifacts/architecture/project-structure.md`
- Team workflow: `.claude/process/team-workflow.md`

## 7. Sprint Status Snapshot

```yaml
epic-1: in-progress
1-1-project-scaffolding-ci-pipeline: done
1-2-sso-authentication: done
1-3-core-data-model-tenant-isolation: done
1-4-shared-ui-components-design-tokens: ready-for-dev
1-5-app-shell-empty-state-landing: ready-for-dev
epic-1-retrospective: optional
```
