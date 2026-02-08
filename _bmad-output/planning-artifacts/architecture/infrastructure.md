# Infrastructure & Deployment

_Extracted from the Architecture Decision Document. The core document ([architecture.md](../architecture.md)) contains core architectural decisions._

## Hosting

- **API:** Azure App Service (Always On, for background services)
- **Frontend:** Azure Static Web Apps (built-in CDN, CI/CD from GitHub)
- **Database:** Azure SQL
- **Storage:** Azure Blob Storage
- **Secrets:** Azure Key Vault (App Service native references)
- **Telemetry:** Application Insights (via Aspire)

## CI/CD

Single GitHub Actions pipeline. Builds and tests both `api/` and `web/` on every PR. Path-filtered optimization deferred until CI becomes a bottleneck.

## Environment Configuration

`appsettings.json` layering for non-secret config. Azure Key Vault for secrets (connection strings, client secrets). App Service settings for deployment-specific overrides.

## Background Processing

- **Import pipeline:** `IHostedService` + `Channel<T>` (in-process)
- **GDPR anonymization:** `IHostedService` with daily timer
- Both run within App Service process (Always On)
- Both use `ITenantContext` with appropriate service context (not HTTP user context)

## Decision Impact: Implementation Sequence

1. Project scaffolding (template init, monorepo structure, CI pipeline, linting, test infrastructure)
2. Authentication (Entra ID + OIDC via MSAL) — gates everything
3. Data model + EF Core setup (`ITenantContext`, global query filters, migration strategy)
4. **Cross-recruitment isolation integration tests** (mandatory, before feature tests)
5. **Shared frontend components** (StatusBadge, ActionButton, EmptyState, Toast, SkeletonLoader, ErrorBoundary) — must exist before any feature UI
6. Recruitment CRUD (first end-to-end vertical slice)
7. Team membership management
8. Candidate import pipeline (async processing via `IHostedService` + `Channel<T>`)
9. Document storage + SAS token generation (batch-capable)
10. Batch screening UX (frontend-heavy)
11. Overview dashboard (computed `GROUP BY` query)
12. Audit trail
13. GDPR retention job (daily timer, `ITenantContext.IsServiceContext`)

## Cross-Component Dependencies

- `ITenantContext` + global query filters depend on auth (need current user identity)
- Import pipeline depends on data model + document storage + `ITenantContext.RecruitmentId`
- Screening UX depends on candidate list API + batch SAS URLs
- Overview depends on computed query (no write-path dependencies — simplest path)
- GDPR job depends on anonymization schema design + `ITenantContext.IsServiceContext`
