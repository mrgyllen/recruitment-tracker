# Story 7.3: CI/CD Pipeline with Auto-Migration

Status: ready-for-dev

## Story

As a **platform operator**,
I want code merged to main to be automatically built, tested, and deployed,
So that I can ship changes reliably without manual deployment steps.

**NFRs covered:** NFR35 (automated deployment pipeline), NFR39 (pipeline completes within 10 minutes)

**Dependencies:**
- Story 7.1 (Health Check Endpoints) must be implemented first -- the deployed application needs `/health` and `/ready` endpoints for App Service health probes
- Story 7.2 (Azure Infrastructure as Code) must be implemented first -- `azd provision` must have been run at least once to create the Azure resources before the first `azd deploy`

**Downstream:**
- Story 7.4 (Staging Environment) will modify `cd.yml` to target the staging environment by default and add manual promotion to production

## Acceptance Criteria

**AC-1: Existing CI pipeline remains unchanged**
**Given** a PR is opened against main
**When** the CI pipeline runs (`.github/workflows/ci.yml`, existing)
**Then** the build and test suite must pass as a merge gate (existing behavior, verified still works)

**AC-2: CD pipeline deploys on merge to main**
**Given** a PR is merged to main
**When** the CD pipeline triggers (`.github/workflows/cd.yml`, new)
**Then** the application is deployed to the Azure environment via `azd deploy`
**And** the full pipeline (build + test + deploy) completes within 10 minutes

**AC-3: Separate CI and CD workflows**
**Given** the CD pipeline is a separate workflow from CI
**When** both workflows are reviewed
**Then** `ci.yml` handles PR checks (build + test for both `api/` and `web/`) and remains unchanged
**And** `cd.yml` handles push-to-main deployment via azd with federated credentials (OIDC, no stored secrets)

**AC-4: Auto-migration on startup**
**Given** the application is being deployed
**When** the API starts up in the deployed environment
**Then** EF Core database migrations execute automatically on startup before the application accepts traffic
**And** this behavior is limited to single-instance deployments (current scale)

**AC-5: Rollback strategy**
**Given** a deployment introduces a breaking change
**When** rollback is needed
**Then** the rollback strategy is to re-run the CD pipeline for the previous commit (deploy-previous-version, not reverse-migration)

**AC-6: Template workflow superseded**
**Given** the existing `api/.github/workflows/azure-dev.yml`
**When** the CD pipeline is created
**Then** the new `cd.yml` is placed at `.github/workflows/cd.yml` (repo root, alongside `ci.yml`)
**And** the template's `api/.github/workflows/azure-dev.yml` is removed

## Tasks / Subtasks

### Task 1: Create CD workflow file (AC-2, AC-3, AC-6)

Create `.github/workflows/cd.yml` at the repo root (alongside existing `ci.yml`). This is the core deliverable.

- [ ] Create `.github/workflows/cd.yml` with the structure detailed in Dev Notes below
- [ ] Trigger: `push` to `main` branch only (not PRs)
- [ ] Also support `workflow_dispatch` for manual re-runs (needed for rollback per AC-5)
- [ ] Set OIDC permissions: `id-token: write`, `contents: read`
- [ ] Define environment variables from GitHub repository variables: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_ENV_NAME`, `AZURE_LOCATION`
- [ ] Single job `deploy` with sequential steps (see workflow structure in Dev Notes)
- [ ] Verify the workflow YAML is valid (no syntax errors)

### Task 2: Implement auto-migration in Program.cs (AC-4)

Add EF Core `Database.Migrate()` call to the API startup in `api/src/Web/Program.cs`.

- [ ] Add auto-migration code block after `var app = builder.Build();` and before the HTTP pipeline configuration
- [ ] Guard the migration behind a non-Development environment check (Development uses `InitialiseDatabaseAsync` which does delete-and-recreate)
- [ ] Use a scoped `ApplicationDbContext` to call `Database.Migrate()`
- [ ] Log migration start and completion for observability
- [ ] Migration runs synchronously before `app.Run()` -- no traffic is accepted until migrations complete
- [ ] See Dev Notes for exact code pattern

### Task 3: Remove template azure-dev.yml (AC-6)

- [ ] Delete `api/.github/workflows/azure-dev.yml`
- [ ] Verify that `api/.github/workflows/` directory is empty after deletion; if so, remove the directory
- [ ] Verify that `api/.github/` directory is empty after deletion; if so, remove the directory

### Task 4: Update CI workflow trigger (AC-1, AC-3)

The existing `ci.yml` triggers on both `pull_request` AND `push` to main. With the new `cd.yml` handling push-to-main deployments, the CI workflow's `push` trigger is redundant (CI runs on PRs as a merge gate; CD runs after merge).

- [ ] **Evaluate** whether to remove the `push: branches: [main]` trigger from `ci.yml`. The CD workflow re-runs build+test before deploying, so removing CI's push trigger avoids duplicate runs on every merge. However, keeping it provides a separate build-only signal.
- [ ] **Recommended:** Remove the `push` trigger from `ci.yml` to avoid running two parallel pipelines (CI + CD) on every push to main. The CD pipeline includes its own build and test steps.
- [ ] If modified, the change is minimal: remove lines 5-6 from `ci.yml` (`push: branches: [main]`)

### Task 5: Verify pipeline end-to-end (AC-1, AC-2)

- [ ] Confirm `ci.yml` still works on PRs (open a test PR or check recent PR runs)
- [ ] Confirm `cd.yml` triggers on push to main and completes successfully
- [ ] Confirm auto-migration runs during deployment startup (check Application Insights or App Service logs)
- [ ] Confirm the `/health` and `/ready` endpoints respond after deployment (requires Story 7.1)
- [ ] Confirm total pipeline time is under 10 minutes (NFR39)

## Dev Notes

### CD Workflow Structure

The workflow follows the pattern from the existing `api/.github/workflows/azure-dev.yml` (which it supersedes) but adds build+test steps before deployment. Here is the target structure:

```yaml
name: CD

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    env:
      AZURE_CLIENT_ID: ${{ vars.AZURE_CLIENT_ID }}
      AZURE_TENANT_ID: ${{ vars.AZURE_TENANT_ID }}
      AZURE_SUBSCRIPTION_ID: ${{ vars.AZURE_SUBSCRIPTION_ID }}
      AZURE_ENV_NAME: ${{ vars.AZURE_ENV_NAME }}
      AZURE_LOCATION: ${{ vars.AZURE_LOCATION }}
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - uses: actions/setup-node@v4
        with:
          node-version: 'lts/*'
          cache: 'npm'
          cache-dependency-path: web/package-lock.json

      # Build and test API
      - run: dotnet restore api/api.slnx
      - run: dotnet build api/api.slnx --no-restore
      - run: dotnet test api/api.slnx --no-build

      # Build and test web
      - run: npm ci
        working-directory: web
      - run: npm run build
        working-directory: web
      - run: npm run test -- --run
        working-directory: web

      # Deploy via azd (azure.yaml is in api/, so azd must run from there)
      - uses: Azure/setup-azd@v2

      - name: Log in with Azure (Federated Credentials)
        working-directory: api
        run: |
          azd auth login \
            --client-id "$AZURE_CLIENT_ID" \
            --federated-credential-provider "github" \
            --tenant-id "$AZURE_TENANT_ID"

      - name: Deploy Application
        working-directory: api
        run: azd deploy --no-prompt
```

**Key differences from the template `azure-dev.yml`:**
1. Placed at `.github/workflows/cd.yml` (repo root) instead of `api/.github/workflows/`
2. Includes build + test steps for both `api/` and `web/` before deploying
3. Uses `azd deploy` only (not `azd provision`) -- infrastructure provisioning is a separate manual step via Story 7.2
4. Uses bash shell for `azd auth login` (the template used `pwsh` with backtick line continuations)
5. References `dotnet-version: '10.0.x'` directly instead of `global-json-file` (the `global.json` is inside `api/`, and the workflow runs from repo root)
6. Does NOT run `dotnet format` or `npm run lint` -- those are CI's job (merge gate). CD only needs build+test to confirm the merged code is deployable.

**azd configuration context:**
- `api/azure.yaml` defines the service: `web` with `project: ./src/Web`, `host: appservice`, `language: csharp`
- `azd deploy` reads this config and deploys the built API to the App Service tagged with `azd-service-name: web`
- The `azd` CLI must run from the `api/` directory, OR you set `AZD_CONFIG_DIR` to `api/`. Since `azure.yaml` is in `api/`, the deploy step needs a `working-directory: api` or the workflow must account for this path.

**IMPORTANT: `azd` working directory.** The `azure.yaml` file lives in `api/`, not at repo root. The `azd deploy` command must be run from the `api/` directory. Either:
- Add `working-directory: api` to the azd login and deploy steps, OR
- Move `azure.yaml` to repo root (not recommended -- would require updating all `project:` paths)

The recommended approach is to add `working-directory: api` to the azd steps:

```yaml
      - name: Log in with Azure (Federated Credentials)
        working-directory: api
        run: |
          azd auth login \
            --client-id "$AZURE_CLIENT_ID" \
            --federated-credential-provider "github" \
            --tenant-id "$AZURE_TENANT_ID"

      - name: Deploy Application
        working-directory: api
        run: azd deploy --no-prompt
```

### Auto-Migration Implementation

**File:** `api/src/Web/Program.cs`

The current `Program.cs` has this pattern:

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();  // Delete + recreate (dev only)
}
```

Add auto-migration for non-Development environments:

```csharp
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // Auto-migrate: apply pending EF Core migrations on startup.
    // Safe for single-instance deployments (current scale).
    // For multi-instance, switch to a migration job or init container.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Applying database migrations...");
    await db.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully.");
}
```

**Key design decisions:**
- Uses the existing `if (app.Environment.IsDevelopment())` block -- Development continues to use delete-and-recreate via `InitialiseDatabaseAsync()`, while all other environments (Staging, Production) use `MigrateAsync()`
- `MigrateAsync()` is idempotent -- if no pending migrations, it's a no-op
- Runs before `app.Run()` -- the API does not accept traffic until migrations complete
- Single-instance only: if scaled to multiple instances, two instances could race to apply migrations. At that point, switch to a separate migration job or Azure App Service deployment slot warm-up
- Requires `using Microsoft.EntityFrameworkCore;` for the `MigrateAsync()` extension method (may already be implicitly available, but add explicitly if needed)
- Requires adding `using api.Infrastructure.Data;` to access `ApplicationDbContext` directly (the DI container resolves it)

**Why not a configuration flag?** The epic's implementation notes mention "guarded by configuration flag or environment check." An environment check (`!IsDevelopment()`) is simpler and more reliable than a configuration flag. A flag adds a setting that could be misconfigured. The environment check aligns with the existing pattern in `Program.cs`.

### Rollback Strategy

The rollback strategy is intentionally simple: re-deploy the previous known-good commit.

Steps to roll back:
1. Go to the GitHub Actions `CD` workflow
2. Click "Run workflow" (workflow_dispatch)
3. Select the branch/ref for the last known-good commit
4. The pipeline rebuilds and redeploys that version

**Database rollback:** There is no automatic reverse-migration. If a migration introduced a breaking schema change:
1. Create a new forward migration that reverts the schema change
2. Merge and deploy normally

This is the standard EF Core approach and avoids the complexity of `Database.Migrate("targetMigration")` in production.

### GitHub Repository Variables Required

The CD pipeline requires these GitHub repository variables (not secrets -- OIDC federated credentials eliminate stored secrets):

| Variable | Description | Example |
|----------|-------------|---------|
| `AZURE_CLIENT_ID` | App registration client ID for GitHub OIDC | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_TENANT_ID` | Azure AD tenant ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_ENV_NAME` | azd environment name | `recruitment-tracker` |
| `AZURE_LOCATION` | Azure region | `swedencentral` |

These are set up via `azd pipeline config` which creates the federated credential in Azure AD and sets the GitHub variables automatically.

### CI Workflow Trigger Consideration

The existing `ci.yml` triggers on:
```yaml
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
```

With `cd.yml` also triggering on `push: branches: [main]`, every merge to main will run BOTH `ci.yml` and `cd.yml` in parallel. Since `cd.yml` already includes build+test steps, the `ci.yml` push trigger is redundant. Removing it saves GitHub Actions minutes and avoids confusion from two simultaneous runs.

**Recommendation:** Remove `push: branches: [main]` from `ci.yml` so it only triggers on PRs. This is a minimal change (remove 2 lines). The CD workflow provides the post-merge build+test+deploy.

### Cross-References

- **Story 7.1:** Health check endpoints (`/health`, `/ready`) must exist for App Service health probes to work after deployment. The `healthCheckPath: '/health'` is already configured in `api/infra/services/web.bicep`.
- **Story 7.2:** Bicep templates must be provisioned (`azd provision`) before the first `azd deploy`. The CD pipeline does NOT run `azd provision` -- infrastructure is managed separately.
- **Story 7.4:** Will update `cd.yml` to target the staging environment by default, with manual promotion to production via `workflow_dispatch` with an environment input parameter.

### Project Structure Notes

**Files created:**
- `.github/workflows/cd.yml` -- new CD pipeline (repo root, alongside `ci.yml`)

**Files modified:**
- `api/src/Web/Program.cs` -- add auto-migration block for non-Development environments
- `.github/workflows/ci.yml` -- remove `push` trigger (recommended, optional)

**Files deleted:**
- `api/.github/workflows/azure-dev.yml` -- superseded by repo-root `cd.yml`
- `api/.github/workflows/` directory (if empty after deletion)
- `api/.github/` directory (if empty after deletion)

### Testing Strategy

**Testing mode: Spike** -- Pipeline configuration is tested by executing it. No unit tests for YAML files.

- **Auto-migration code:** The `MigrateAsync()` call is a single EF Core method with well-understood behavior. The existing integration test suite (which runs migrations implicitly via the test database) validates that the migration chain is valid. No additional unit test needed for the `Program.cs` startup code.
- **CD workflow:** Tested by merging a PR to main and observing the pipeline run. Check:
  - Build+test steps pass
  - `azd auth login` succeeds with federated credentials
  - `azd deploy` completes successfully
  - Application starts and responds on `/health`
  - Migrations applied (check Application Insights logs for "Applying database migrations" message)
- **Rollback test:** Manually trigger `cd.yml` via `workflow_dispatch` for a previous commit SHA to verify rollback works.
- **CI regression:** Open a test PR and confirm `ci.yml` still runs as a merge gate.

### Dev Guardrails

1. **Do NOT modify `ci.yml` logic** -- only the trigger is optionally changed (remove `push` trigger). The CI job definitions (`api` and `web` jobs) must remain untouched.
2. **Do NOT add `azd provision` to `cd.yml`** -- infrastructure provisioning is a separate manual/deliberate action. The CD pipeline only deploys.
3. **Keep the Development environment behavior unchanged** -- `InitialiseDatabaseAsync()` (delete + recreate) continues to run in Development. Auto-migration is for non-Development environments only.
4. **The `azd deploy` command must run from the `api/` directory** -- `azure.yaml` is located at `api/azure.yaml`, not repo root.

### References

- [Epic 7: Deployment & Infrastructure Automation](../../_bmad-output/planning-artifacts/epics/epic-7-deployment-infrastructure-automation.md)
- [Architecture: Infrastructure shard](../../_bmad-output/planning-artifacts/architecture/infrastructure.md)
- [EF Core Migrations documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli#apply-migrations-at-runtime)
- [azd deploy reference](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/reference#azd-deploy)
- [GitHub Actions OIDC for Azure](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#set-up-azure-login-with-openid-connect-authentication)

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
