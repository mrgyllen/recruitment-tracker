---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/architecture/infrastructure.md
context: Adding Epic 7 for new Deployment & Infrastructure NFRs (NFR35-NFR40)
existingEpics: [1, 2, 3, 4, 5, 6]
decisions:
  - azd over raw Bicep+GitHub Actions — existing scaffolding covers ~70% of needs
  - Direct App Service publish over Docker container on Azure — simpler, no container registry overhead
  - NFR38 amended to reflect actual intent
  - Defer Static Web Apps — frontend deployment is a separate concern
  - Separate CI/CD workflows — ci.yml (PR checks) + cd.yml (push-to-main deploy)
  - Manual promotion from staging to production — no auto-promote complexity
  - Leave unused infra/core/ modules in place — zero risk, zero value in cleanup
  - Personal test deployment constraint — VS Pro subscription ~$50/month, cost-effective tiers only
---

# recruitment-tracker - Epic Breakdown

## Overview

This document provides the epic and story breakdown for the new Deployment & Infrastructure NFRs (NFR35-NFR40) added to the recruitment-tracker PRD. These requirements cover CI/CD automation, infrastructure-as-code, staging environments, and health monitoring.

## Requirements Inventory

### Functional Requirements

None. NFR35-NFR40 are purely infrastructure/deployment concerns with no associated functional requirements.

### NonFunctional Requirements

- **NFR35:** Automated CI/CD pipeline builds, tests, and deploys on PR merge to main. Full pipeline completes within 10 minutes. PRs require passing build and test suite as merge gate.
- **NFR36:** A staging environment mirrors production configuration for end-to-end verification before production deployment.
- **NFR37:** All Azure infrastructure defined declaratively using Bicep templates. Full environment reproducible from source control within 30 minutes.
- **NFR38 (amended):** API deployed to Azure App Service using azd deployment pipeline. Docker Compose retained for local development environment.
- **NFR39:** EF Core database migrations execute automatically during deployment. Rollback strategy is deploy-previous-version, not reverse-migration.
- **NFR40:** Health check endpoints: `/health` (liveness — process alive) and `/ready` (readiness — database connected, dependencies available) enable Azure load balancer integration and automatic container restart on failure.

### Additional Requirements

- **Hosting model (Architecture Decision #4):** Azure App Service (API, Always On) + Static Web Apps (frontend) + Azure SQL + Azure Blob Storage + Key Vault + Application Insights
- **Existing azd scaffolding:** `api/infra/main.bicep` (App Service, SQL, Key Vault, Monitoring), `api/infra/core/` (40+ reusable Bicep modules), `api/.github/workflows/azure-dev.yml` (federated auth deployment), `api/azure.yaml` (service definition)
- **Existing CI pipeline:** `.github/workflows/ci.yml` builds and tests both `api/` and `web/` — remains unchanged as PR merge gate
- **Existing Docker Compose:** `docker-compose.yml` — local dev with SQL Server + API (retained, not used for Azure deployment)
- **Config pattern (Architecture Decision #10):** `appsettings.json` layering + Azure Key Vault for secrets
- **Migration strategy:** EF Core migrations from first deployment, deploy-previous-version as rollback (not reverse-migration)
- **Configurable deployment values:** GDPR retention period, stale step threshold, SAS token validity, XLSX column mapping
- **Scaffolding gaps to address:** Add Blob Storage module to main.bicep, update runtime 9.0→10.0, fix ASPNETCORE_ENVIRONMENT to Production, rename azure.yaml from template default
- **Deployment constraint:** Personal test deployment using Visual Studio Professional Subscription (~$50 USD/month Azure credit). Use cost-effective tiers (B1 App Service, Basic SQL). Single test environment. Include `azd down` teardown instructions. Prioritize "verify it works" over "production-ready."

### NFR Coverage Map

```
NFR35: Epic 7 — CI/CD pipeline via azd (build, test, deploy on merge)
NFR36: Epic 7 — Staging environment via azd env
NFR37: Epic 7 — Bicep IaC (adapt existing scaffolding + add missing modules)
NFR38: Epic 7 — App Service deployment via azd (Docker Compose for local dev only)
NFR39: Epic 7 — EF Core auto-migration on deploy
NFR40: Epic 7 — Health check endpoints (/health, /ready)
```

## Epic List

### Epic 7: Deployment & Infrastructure Automation

The application can be deployed to Azure through an automated azd pipeline with infrastructure defined as Bicep templates. Changes are verified in a staging environment before reaching production. Health monitoring enables automatic container restart on failure.

**NFRs covered:** NFR35, NFR36, NFR37, NFR38 (amended), NFR39, NFR40

**Implementation scope:** ASP.NET Core health check endpoints (`/health` liveness, `/ready` readiness with DB probe), adapt existing azd Bicep scaffolding (add Blob Storage module, update runtime to .NET 10), new `cd.yml` GitHub Actions workflow for push-to-main deployment via azd, EF Core auto-migration on startup, staging environment via azd env with manual promotion to production.

**Key decisions:**
- azd over raw Bicep+GHA — existing scaffolding covers ~70% of needs
- Direct publish over Docker on Azure — simpler, no container registry overhead
- NFR38 amended: "API deployed to Azure App Service using azd deployment pipeline. Docker Compose retained for local development environment."
- Static Web Apps deferred — frontend deployment is a separate concern
- Separate workflows: `ci.yml` (PR checks, existing) + `cd.yml` (push-to-main deploy, new)
- Manual promotion from staging to production
- Personal test deployment: VS Pro subscription ~$50/month, cost-effective tiers, include `azd down` teardown

**Dependencies on existing work:** Builds on Epic 1's CI pipeline and Docker Compose. No dependencies on Epics 2-6.

---

## Epic 7: Deployment & Infrastructure Automation

### Story 7.1: Health Check Endpoints

As a **platform operator**,
I want the API to expose health check endpoints,
So that Azure App Service can detect failures and automatically restart unhealthy instances.

**NFR covered:** NFR40

**Acceptance Criteria:**

**Given** the API is running
**When** a GET request is sent to `/health`
**Then** a 200 OK response is returned confirming the process is alive (liveness check)

**Given** the API is running and the database is connected
**When** a GET request is sent to `/ready`
**Then** a 200 OK response is returned confirming all dependencies are available (readiness check)

**Given** the API is running but the database is unreachable
**When** a GET request is sent to `/ready`
**Then** a 503 Service Unavailable response is returned indicating the service is not ready

**Given** the health check endpoints are registered
**When** the authentication middleware processes requests to `/health` or `/ready`
**Then** both endpoints are excluded from authentication and respond without requiring a valid JWT token

**Given** the health check endpoints exist
**When** the application starts in any environment (local, staging, production)
**Then** both endpoints respond correctly regardless of environment configuration

**Implementation notes:**
- Use ASP.NET Core `Microsoft.Extensions.Diagnostics.HealthChecks`
- `/health` — basic liveness (process alive, no dependency checks)
- `/ready` — readiness with custom `DbHealthCheck` implementing `IHealthCheck` for SQL connectivity
- Register separate health check endpoints with different check sets via `MapHealthChecks`
- Explicitly exclude from auth middleware (`AllowAnonymous` or pipeline ordering)
- The existing azd scaffolding already configures `healthCheckPath: '/health'` in `services/web.bicep` — the App Service probe will align with this endpoint

---

### Story 7.2: Azure Infrastructure as Code

As a **platform operator**,
I want all Azure infrastructure defined declaratively in Bicep templates,
So that I can reproduce the full environment from source control within 30 minutes.

**NFRs covered:** NFR37, NFR38 (amended)

**Acceptance Criteria:**

**Given** the existing azd scaffolding in `api/infra/`
**When** `azure.yaml` is updated
**Then** the project name is `recruitment-tracker` (not `clean-architecture-azd`) and the service definition reflects the actual API project

**Given** the existing `main.bicep` provisions App Service, SQL, Key Vault, and Monitoring
**When** the Bicep templates are updated
**Then** Azure Blob Storage is provisioned for document storage
**And** the App Service runtime version is `10.0` (not `9.0`)
**And** `ASPNETCORE_ENVIRONMENT` is set to `Production` (not `Development`)

**Given** the deployment targets a Visual Studio Professional Subscription (~$50/month credit)
**When** resource SKUs are configured
**Then** App Service uses B1 tier, Azure SQL uses Basic tier, and all other resources use the lowest cost-effective tier
**And** SKU values are parameterized in Bicep so they can be overridden for different environments

**Given** the complete Bicep templates
**When** `azd provision` is run against a clean Azure subscription
**Then** all required resources are created: App Service, Azure SQL, Blob Storage, Key Vault, Application Insights
**And** the environment is fully functional within 30 minutes

**Given** a deployed environment is no longer needed
**When** `azd down` is executed
**Then** all provisioned resources are removed and the Azure subscription incurs no further charges
**And** teardown instructions are documented in the project README or a deployment guide

**Given** the infrastructure templates
**When** a developer reviews the Bicep files
**Then** all resources are defined declaratively with no manual Azure portal configuration required

**Implementation notes:**
- Adapt existing `api/infra/main.bicep` — don't rewrite from scratch
- Add `core/storage/storage-account.bicep` module reference to `main.bicep`
- Leave unused `infra/core/` modules in place — they are inert and cost nothing
- Static Web Apps deferred to a future story — this story covers API infrastructure only
- Key Vault references for connection strings (SQL connection string, Blob Storage connection string)
- Parameterize SKUs with defaults appropriate for test deployment (B1, Basic)

---

### Story 7.3: CI/CD Pipeline with Auto-Migration

As a **platform operator**,
I want code merged to main to be automatically built, tested, and deployed,
So that I can ship changes reliably without manual deployment steps.

**NFRs covered:** NFR35, NFR39

**Acceptance Criteria:**

**Given** a PR is opened against main
**When** the CI pipeline runs (`.github/workflows/ci.yml`, existing)
**Then** the build and test suite must pass as a merge gate (existing behavior, verified still works)

**Given** a PR is merged to main
**When** the CD pipeline triggers (`.github/workflows/cd.yml`, new)
**Then** the application is deployed to the Azure environment via `azd deploy`
**And** the full pipeline (build + test + deploy) completes within 10 minutes

**Given** the CD pipeline is a separate workflow from CI
**When** both workflows are reviewed
**Then** `ci.yml` handles PR checks (build + test for both `api/` and `web/`) and remains unchanged
**And** `cd.yml` handles push-to-main deployment via azd with federated credentials (OIDC, no stored secrets)

**Given** the application is being deployed
**When** the API starts up in the deployed environment
**Then** EF Core database migrations execute automatically on startup before the application accepts traffic
**And** this behavior is limited to single-instance deployments (current scale)

**Given** a deployment introduces a breaking change
**When** rollback is needed
**Then** the rollback strategy is to re-run the CD pipeline for the previous commit (deploy-previous-version, not reverse-migration)

**Given** the existing `api/.github/workflows/azure-dev.yml`
**When** the CD pipeline is created
**Then** the new `cd.yml` is placed at `.github/workflows/cd.yml` (repo root, alongside `ci.yml`)
**And** the template's `api/.github/workflows/azure-dev.yml` is removed or superseded

**Implementation notes:**
- Create new `.github/workflows/cd.yml` — don't modify the existing `ci.yml`
- Auto-migration: `db.Database.Migrate()` in `Program.cs` startup, guarded by configuration flag or environment check
- Federated credentials (OIDC) via `azd auth login --federated-credential-provider github` — matches existing template pattern
- Pipeline needs: checkout → setup-dotnet → setup-node → build api → test api → build web → test web → azd auth → azd deploy
- Remove or ignore `api/.github/workflows/azure-dev.yml` after creating repo-level `cd.yml`

---

### Story 7.4: Staging Environment

As a **platform operator**,
I want a staging environment that mirrors production configuration,
So that I can verify changes end-to-end before they reach production.

**NFR covered:** NFR36

**Acceptance Criteria:**

**Given** the Bicep templates from Story 7.2
**When** a staging environment is provisioned via `azd env new staging` and `azd provision`
**Then** all resources are created mirroring the production resource types (App Service, SQL, Blob Storage, Key Vault, Application Insights)
**And** staging uses its own resource group, database, and storage (fully isolated from production)

**Given** the staging environment exists
**When** the CD pipeline is updated to target staging
**Then** code merged to main deploys to the staging environment automatically

**Given** the staging environment is running
**When** the health check endpoints are probed
**Then** `/health` and `/ready` return healthy status confirming the staging deployment is functional

**Given** staging and production environments
**When** promotion to production is needed
**Then** production deployment is triggered manually (re-run CD pipeline targeting production environment)
**And** no automatic promotion from staging to production occurs

**Given** staging and production environments
**When** environment-specific configuration is reviewed
**Then** each environment uses its own Key Vault, connection strings, and app settings
**And** no production secrets are accessible from staging

**Given** the test deployment constraint (~$50/month Azure credit)
**When** staging is not actively being used for verification
**Then** the staging environment can be torn down via `azd down` to conserve credits
**And** re-provisioned when needed via `azd provision`

**Given** the test environment is running (staging or production)
**When** the user runs `azd down`
**Then** all Azure resources in that environment are destroyed
**And** monthly cost drops to zero for that environment
**And** no orphaned resources remain incurring charges

**Implementation notes:**
- `azd` natively supports multiple environments via `azd env` — staging is a named environment with its own `.env` file
- Staging SKUs can match production (both B1/Basic for test deployment) or use even smaller tiers
- Update `cd.yml` to deploy to staging environment by default
- Manual promotion = manually trigger `cd.yml` targeting production environment (workflow_dispatch with environment input)
- Document the staging workflow: provision → deploy → verify → promote or teardown

---

## Epic 7 Summary

| Story | Title | NFRs | Scope |
|-------|-------|------|-------|
| 7.1 | Health Check Endpoints | NFR40 | Small — code only |
| 7.2 | Azure Infrastructure as Code | NFR37, NFR38 | Medium — Bicep adaptation |
| 7.3 | CI/CD Pipeline with Auto-Migration | NFR35, NFR39 | Medium — pipeline + code |
| 7.4 | Staging Environment | NFR36 | Medium — env provisioning |

**Dependency flow:** 7.1 → 7.2 → 7.3 → 7.4 (each builds on previous, none depends on future stories)

**All NFRs covered:** NFR35 ✅ NFR36 ✅ NFR37 ✅ NFR38 ✅ NFR39 ✅ NFR40 ✅
