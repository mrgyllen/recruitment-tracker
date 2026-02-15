# Story 7.4: Staging Environment

Status: ready-for-dev

## Story

As a **platform operator**,
I want a staging environment that mirrors production configuration,
So that I can verify changes end-to-end before they reach production.

**NFR covered:** NFR36

**Dependencies:**
- **Story 7.1** (Health Check Endpoints) -- `/health` and `/ready` endpoints used to verify staging deployment is functional
- **Story 7.2** (Azure Infrastructure as Code) -- Bicep templates that define the resources provisioned per environment
- **Story 7.3** (CI/CD Pipeline with Auto-Migration) -- `cd.yml` workflow that this story modifies to target staging by default and support manual promotion

## Acceptance Criteria

### AC1: Staging environment provisioned via azd with full resource isolation
**Given** the Bicep templates from Story 7.2
**When** a staging environment is provisioned via `azd env new staging` and `azd provision`
**Then** all resources are created mirroring the production resource types (App Service, SQL, Blob Storage, Key Vault, Application Insights)
**And** staging uses its own resource group, database, and storage (fully isolated from production)

### AC2: CD pipeline deploys to staging by default
**Given** the staging environment exists
**When** the CD pipeline is updated to target staging
**Then** code merged to main deploys to the staging environment automatically

### AC3: Health check verification in staging
**Given** the staging environment is running
**When** the health check endpoints are probed
**Then** `/health` and `/ready` return healthy status confirming the staging deployment is functional

### AC4: Manual promotion to production via workflow_dispatch
**Given** staging and production environments
**When** promotion to production is needed
**Then** production deployment is triggered manually (re-run CD pipeline targeting production environment)
**And** no automatic promotion from staging to production occurs

### AC5: Environment-specific secrets and configuration
**Given** staging and production environments
**When** environment-specific configuration is reviewed
**Then** each environment uses its own Key Vault, connection strings, and app settings
**And** no production secrets are accessible from staging

### AC6: Teardown to conserve credits
**Given** the test deployment constraint (~$50/month Azure credit)
**When** staging is not actively being used for verification
**Then** the staging environment can be torn down via `azd down` to conserve credits
**And** re-provisioned when needed via `azd provision`

### AC7: Clean teardown with no orphaned resources
**Given** the test environment is running (staging or production)
**When** the user runs `azd down`
**Then** all Azure resources in that environment are destroyed
**And** monthly cost drops to zero for that environment
**And** no orphaned resources remain incurring charges

## Tasks / Subtasks

### Task 1: Understand and verify azd multi-environment model (AC: #1, #5)

No code changes in this task -- this is analysis and verification that the existing Bicep parameterization from Story 7.2 supports multi-environment deployment without modification.

- [ ] 1.1 Verify that `main.bicep` uses `environmentName` to derive resource group name (`rg-{environmentName}`), resource token (`uniqueString(subscription().id, environmentName, location)`), and tags (`azd-env-name`). This means each azd environment automatically gets fully isolated resources.
- [ ] 1.2 Verify that `main.parameters.json` maps `AZURE_ENV_NAME` to `environmentName`, and that `dbAdminPassword`/`dbAppUserPassword` use `secretOrRandomPassword` scoped to the environment's Key Vault (each environment generates its own passwords).
- [ ] 1.3 Confirm the resource isolation chain:
  - `azd env new staging` creates `.azure/staging/.env` with `AZURE_ENV_NAME=staging`
  - `azd env new production` creates `.azure/production/.env` with `AZURE_ENV_NAME=production`
  - `azd provision` (with staging selected) creates resource group `rg-staging` with all resources named using a unique token derived from `staging`
  - `azd provision` (with production selected) creates resource group `rg-production` with a different unique token
  - Result: completely separate Key Vaults, SQL servers, databases, App Services, storage accounts, and Application Insights instances
- [ ] 1.4 Document that no Bicep template changes are needed for multi-environment support -- azd's environment model handles isolation inherently through parameterization

### Task 2: Update cd.yml to support environment selection (AC: #2, #4)

Modify the `cd.yml` created in Story 7.3 to deploy to staging by default and support manual promotion to production via `workflow_dispatch`.

- [ ] 2.1 Add `workflow_dispatch` trigger with an `environment` input parameter:
  ```yaml
  on:
    push:
      branches: [main]
    workflow_dispatch:
      inputs:
        environment:
          description: 'Target environment (staging or production)'
          required: true
          default: 'staging'
          type: choice
          options:
            - staging
            - production
  ```
- [ ] 2.2 Add environment resolution logic at the job level. When triggered by push to main, the environment is `staging`. When triggered by `workflow_dispatch`, use the selected input:
  ```yaml
  env:
    AZURE_ENV_NAME: ${{ github.event_name == 'workflow_dispatch' && github.event.inputs.environment || 'staging' }}
  ```
- [ ] 2.3 Ensure the azd deploy step uses the resolved environment name. The `AZURE_ENV_NAME` env var is what `azd` reads from `main.parameters.json` via `${AZURE_ENV_NAME}`, so setting it correctly is sufficient.
- [ ] 2.4 Add a post-deploy health check step that verifies the deployment is functional:
  ```yaml
  - name: Verify deployment health
    run: |
      APP_URL=$(azd env get-values --output json | jq -r '.WEB_BASE_URI')
      echo "Checking health at $APP_URL/health..."
      for i in {1..6}; do
        STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$APP_URL/health" || echo "000")
        if [ "$STATUS" = "200" ]; then
          echo "Health check passed"
          exit 0
        fi
        echo "Attempt $i: status $STATUS, retrying in 10s..."
        sleep 10
      done
      echo "Health check failed after 60 seconds"
      exit 1
  ```
- [ ] 2.5 Verify that the cd.yml correctly handles the azd auth flow with federated credentials for the target environment

### Task 3: Configure GitHub repository variables for staging (AC: #2, #5)

This task documents the GitHub configuration steps the operator must perform. These are manual portal/CLI steps, not code changes.

- [ ] 3.1 Document the following GitHub repository variables that must be set (these are referenced by cd.yml via `vars.*`):
  - `AZURE_CLIENT_ID` -- Service principal client ID (from `azd pipeline config`)
  - `AZURE_TENANT_ID` -- Azure AD tenant ID
  - `AZURE_SUBSCRIPTION_ID` -- Azure subscription ID
  - `AZURE_LOCATION` -- Azure region (e.g., `westeurope`)
- [ ] 3.2 Document that `AZURE_ENV_NAME` is NOT set as a GitHub variable -- it is derived dynamically in cd.yml based on the trigger (push = staging, workflow_dispatch = operator's choice)
- [ ] 3.3 Document the federated credential setup via `azd pipeline config`:
  ```bash
  # Select the staging environment first
  azd env select staging
  # Configure GitHub Actions federated credentials for the staging environment
  azd pipeline config
  ```
- [ ] 3.4 Document that a single service principal with Contributor role on the subscription (or both resource groups) is sufficient for deploying to both environments. Alternatively, use environment-scoped GitHub environments with separate credentials for stronger isolation.

### Task 4: Document staging workflow and cost management (AC: #3, #6, #7)

Create a deployment guide document covering the staging workflow, cost management, and troubleshooting.

- [ ] 4.1 Create `docs/deployment-guide.md` with the following sections:

**4.1a: Environment Setup**
```bash
# One-time: create environments locally
azd env new staging
azd env select staging
azd provision          # Creates rg-staging with all resources

azd env new production
azd env select production
azd provision          # Creates rg-production with all resources

# One-time: configure GitHub Actions credentials
azd env select staging
azd pipeline config    # Sets up federated credentials for GitHub Actions
```

**4.1b: Daily Workflow**
```
Developer pushes to main
  -> CI runs (ci.yml: build + test for api/ and web/)
  -> CD runs (cd.yml: deploy to staging)
  -> Operator verifies staging via health checks and manual testing
  -> If good: operator triggers workflow_dispatch with environment=production
  -> Production updated
```

**4.1c: Cost Management**
```bash
# Tear down staging when not in use (saves ~$25/month)
azd env select staging
azd down               # Destroys all resources in rg-staging

# Re-provision when ready to test again
azd env select staging
azd provision          # Recreates all resources from Bicep templates
```

**4.1d: Estimated Monthly Costs**

| Resource | Staging | Production | Notes |
|----------|---------|------------|-------|
| App Service B1 | ~$13 | ~$13 | Linux, 1 core, 1.75 GB |
| Azure SQL Basic | ~$5 | ~$5 | 5 DTU, 2 GB |
| Blob Storage | ~$1 | ~$1 | LRS, hot tier |
| Key Vault | ~$0.03/10K ops | ~$0.03/10K ops | Standard tier |
| Application Insights | Free tier | Free tier | 5 GB/month included |
| Log Analytics | Free tier | Free tier | 5 GB/month included |
| **Total per env** | **~$19** | **~$19** | |
| **Both running** | | | **~$38/month** |

With ~$50/month VS Pro credits, running both environments simultaneously is feasible. Tear down staging when not verifying to stay well under budget.

**4.1e: Troubleshooting**
- `azd provision` fails: check `AZURE_LOCATION` supports all resource types, check subscription quota
- Health check fails after deploy: check App Service logs via `az webapp log tail --name <app> --resource-group rg-staging`
- Deployment takes too long: SQL deployment script (user creation) can take 2-3 minutes -- this is normal
- `azd down` leaves orphans: check `az group list --tag azd-env-name=staging` to find any remaining resources

**4.1f: Environment Isolation Verification Checklist**
- [ ] Staging resource group is `rg-staging`, production is `rg-production`
- [ ] Each has its own Key Vault with independent secrets
- [ ] Each has its own SQL Server + database with unique passwords
- [ ] Each has its own App Service with separate URL
- [ ] Each has its own Blob Storage account
- [ ] Each has its own Application Insights instance
- [ ] Staging App Service cannot access production Key Vault (and vice versa)

### Task 5: Add GitHub Actions environment protection (AC: #4) -- OPTIONAL

This task adds optional GitHub environment protection rules for production deployments. This provides an extra safety layer beyond `workflow_dispatch` but is not strictly required for the story.

- [ ] 5.1 Document how to create a `production` GitHub environment with required reviewers:
  - Go to repo Settings > Environments > New environment > "production"
  - Add required reviewer (the operator/developer)
  - This means production deployments via `workflow_dispatch` require approval before executing
- [ ] 5.2 Update cd.yml to use GitHub environment when deploying to production:
  ```yaml
  jobs:
    deploy:
      environment: ${{ env.AZURE_ENV_NAME == 'production' && 'production' || '' }}
  ```
  Note: This is optional and can be added later. The core flow (workflow_dispatch with environment choice) works without it.

## Dev Notes

### How azd Multi-Environment Works (Critical Context for Implementation)

The azd CLI natively supports multiple environments through the `azd env` command family. Understanding this model is essential for this story.

**Environment storage:** Each environment has its own `.azure/<env-name>/.env` file containing environment-specific values:
```
AZURE_ENV_NAME=staging
AZURE_LOCATION=westeurope
AZURE_SUBSCRIPTION_ID=<sub-id>
AZURE_PRINCIPAL_ID=<principal-id>
```

**Resource isolation via parameterization:** The existing `main.bicep` already achieves full isolation through these two mechanisms:
1. **Resource group name:** `rg-${environmentName}` -- each environment gets its own resource group
2. **Resource token:** `uniqueString(subscription().id, environmentName, location)` -- each environment gets unique resource names

This means `azd provision` with `AZURE_ENV_NAME=staging` creates `rg-staging` with resources like `app-abc123staging...`, while `AZURE_ENV_NAME=production` creates `rg-production` with `app-xyz789prod...`. Zero overlap, zero shared resources.

**Password isolation:** `main.parameters.json` uses `secretOrRandomPassword ${AZURE_KEY_VAULT_NAME}` which generates unique passwords per environment because each environment has its own Key Vault with a unique name.

### cd.yml Modification Pattern

Story 7.3 creates `cd.yml` with a structure like:
```yaml
name: CD

on:
  push:
    branches: [main]

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
      - uses: Azure/setup-azd@v2
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Log in with Azure (Federated Credentials)
        run: azd auth login --client-id "$AZURE_CLIENT_ID" --federated-credential-provider "github" --tenant-id "$AZURE_TENANT_ID"
      - name: Deploy Application
        run: azd deploy --no-prompt
```

This story modifies cd.yml to:
1. Add `workflow_dispatch` trigger with `environment` input
2. Replace static `AZURE_ENV_NAME: ${{ vars.AZURE_ENV_NAME }}` with dynamic resolution: `staging` on push, operator's choice on `workflow_dispatch`
3. Add post-deploy health check step
4. Optionally add GitHub environment protection for production

### What This Story Does NOT Change

- **No Bicep template changes.** The parameterized templates from Story 7.2 already support multi-environment deployment. The `environmentName` parameter drives complete resource isolation.
- **No `azure.yaml` changes.** The azd project configuration is environment-agnostic.
- **No `ci.yml` changes.** CI remains a PR gate and push check, independent of deployment target.
- **No application code changes.** The API application is environment-agnostic; only Azure resource configuration differs.

### Cost Awareness

The VS Pro subscription provides ~$50/month in Azure credits. With both staging and production running simultaneously, expect ~$38/month. Key cost drivers:
- **App Service B1:** ~$13/month per environment (the largest single cost)
- **Azure SQL Basic:** ~$5/month per environment
- **Strategy:** Tear down staging (`azd down`) when not actively verifying. Re-provision takes ~5-10 minutes.

### Testing Strategy

| Task | Mode | Rationale |
|------|------|-----------|
| Task 1 (Verify azd model) | **Analysis only** | No code changes. Verify existing parameterization works for multi-env. |
| Task 2 (cd.yml updates) | **Characterization** | Workflow changes tested by manual trigger in GitHub Actions. Verify staging deploys on push, production deploys on workflow_dispatch. |
| Task 3 (GitHub config) | **Manual verification** | GitHub repository settings configured via portal/CLI. Verify via dry-run. |
| Task 4 (Documentation) | **No tests** | Documentation deliverable. |
| Task 5 (Environment protection) | **Manual verification** | Optional GitHub environment protection. Verify approval flow works. |

**End-to-end verification checklist (manual, post-implementation):**
1. `azd env new staging && azd provision` -- creates all resources in `rg-staging`
2. `azd deploy` -- deploys application to staging App Service
3. `curl https://<staging-app>.azurewebsites.net/health` returns 200
4. `curl https://<staging-app>.azurewebsites.net/ready` returns 200
5. Push to main triggers cd.yml and deploys to staging automatically
6. `workflow_dispatch` with `environment=production` deploys to production
7. `azd env select staging && azd down` removes all staging resources
8. `az group list --tag azd-env-name=staging` returns empty list (no orphans)
9. `azd env select staging && azd provision` recreates staging cleanly

### Dev Guardrails

- **Never hardcode environment names in Bicep.** All environment-specific values flow through `environmentName` parameter.
- **Never store `AZURE_ENV_NAME` as a GitHub variable.** It must be derived dynamically in the workflow to support multi-environment targeting.
- **Never share Key Vault references across environments.** Each environment's `secretOrRandomPassword` is scoped to its own Key Vault.
- **Never auto-promote to production.** The workflow_dispatch trigger with explicit environment selection is the only path to production.
- **Always verify health after deployment.** The post-deploy health check step must pass before considering the deployment successful.

### Project Structure Notes

**Files to modify:**
```
.github/workflows/cd.yml    # Add workflow_dispatch, dynamic env, health check
```

**Files to create:**
```
docs/deployment-guide.md     # Staging workflow, cost management, troubleshooting
```

**No changes to:**
```
api/azure.yaml               # Environment-agnostic
api/infra/main.bicep          # Already parameterized for multi-env
api/infra/main.parameters.json # Already uses AZURE_ENV_NAME
api/infra/services/web.bicep  # Already has healthCheckPath: '/health'
.github/workflows/ci.yml      # Independent of deployment target
```

### References

- [Source: `_bmad-output/planning-artifacts/epics/epic-7-deployment-infrastructure-automation.md` -- Story 7.4 definition, acceptance criteria, implementation notes]
- [Source: `_bmad-output/planning-artifacts/architecture/infrastructure.md` -- Hosting: App Service, SQL, Blob Storage, Key Vault, Application Insights]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions` -- Decision #4: Azure hosting model (App Service + Static Web Apps), Decision #10: Environment configuration]
- [Source: `api/infra/main.bicep` -- `environmentName` parameterization, resource group naming (`rg-{environmentName}`), resource token (`uniqueString`), tags (`azd-env-name`)]
- [Source: `api/infra/main.parameters.json` -- `AZURE_ENV_NAME` mapping, `secretOrRandomPassword` scoped to env Key Vault]
- [Source: `api/infra/services/web.bicep` -- App Service B1 SKU, `healthCheckPath: '/health'`, runtime config]
- [Source: `api/infra/core/database/sqlserver/sqlserver.bicep` -- SQL Server + database + Key Vault secret storage per environment]
- [Source: `api/infra/core/security/keyvault.bicep` -- Key Vault per environment with access policies]
- [Source: `api/.github/workflows/azure-dev.yml` -- Template CD workflow with federated credentials pattern, `AZURE_ENV_NAME` usage]
- [Source: `.github/workflows/ci.yml` -- Existing CI pipeline (unchanged by this story)]
- [Source: `_bmad-output/implementation-artifacts/7-1-health-check-endpoints.md` -- Story 7.1: Health check endpoints used for staging verification]
- [Source: `_bmad-output/implementation-artifacts/7-2-azure-infrastructure-as-code.md` -- Story 7.2: Bicep templates, Blob Storage addition, SKU parameterization]
- [Source: `_bmad-output/implementation-artifacts/7-3-cicd-pipeline-auto-migration.md` -- Story 7.3: cd.yml creation, azd deploy flow]

## Dev Agent Record

### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
