# Epic 7: Deployment & Infrastructure Automation

The application can be deployed to Azure through an automated azd pipeline with infrastructure defined as Bicep templates. Changes are verified in a staging environment before reaching production. Health monitoring enables automatic container restart on failure.

**NFRs covered:** NFR35, NFR36, NFR37, NFR38 (amended), NFR39, NFR40

**Key decisions:**
- azd over raw Bicep+GHA — existing scaffolding covers ~70% of needs
- Direct App Service publish over Docker on Azure — simpler, no container registry overhead
- NFR38 amended: "API deployed to Azure App Service using azd deployment pipeline. Docker Compose retained for local development environment."
- Static Web Apps deferred — frontend deployment is a separate concern
- Separate workflows: `ci.yml` (PR checks, existing) + `cd.yml` (push-to-main deploy, new)
- Manual promotion from staging to production
- Personal test deployment: VS Pro subscription ~$50/month, cost-effective tiers, include `azd down` teardown

**Dependencies on existing work:** Builds on Epic 1's CI pipeline and Docker Compose. No dependencies on Epics 2-6.

## Story 7.1: Health Check Endpoints

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

## Story 7.2: Azure Infrastructure as Code

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

## Story 7.3: CI/CD Pipeline with Auto-Migration

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

## Story 7.4: Staging Environment

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
