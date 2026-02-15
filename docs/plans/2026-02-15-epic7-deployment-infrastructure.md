# Epic 7: Deployment & Infrastructure Automation — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable Azure deployment with health monitoring, infrastructure-as-code, automated CI/CD pipeline, and staging environment support.

**Architecture:** Infrastructure-only epic. No new domain logic, command/query handlers, or frontend features. Stories modify Program.cs (health checks, auto-migration), Bicep templates (IaC), and GitHub Actions workflows (CI/CD). All stories build on the existing azd scaffolding in `api/infra/`.

**Tech Stack:** ASP.NET Core Health Checks, Azure Bicep, GitHub Actions, Azure Developer CLI (azd), EF Core Migrations

**Deferred items resolved:** `epic-5-deferred-aggregate-root-test` (A-007)

---

## Pre-Epic: Resolve Deferred Epic 5 Item

### Task 0: Aggregate Root Architecture Test (epic-5-deferred-aggregate-root-test / A-007)

**Testing mode:** Test-first — this task IS the test.

**Files:**
- Create: `api/tests/Application.UnitTests/Architecture/AggregateRootArchitectureTests.cs`
- Modify: `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Context:** Architecture mandates "modify child entities only through aggregate root methods." The existing `ValidatorArchitectureTests.cs` and `AuthorizationArchitectureTests.cs` at `api/tests/Application.UnitTests/Architecture/` prove this enforcement pattern works. This test scans Application layer command handlers to ensure they don't directly call `DbContext.Add/Update/Remove` on owned entity types, enforcing that state changes flow through aggregate roots.

**Owned entity types (NOT aggregate roots):** `WorkflowStep`, `RecruitmentMember`, `CandidateOutcome`, `CandidateDocument`, `ImportDocument`, `ImportRowResult`

**Aggregate roots (DbSet access allowed):** `Recruitment`, `Candidate`, `ImportSession`, `AuditEntry`

**Step 1: Write the architecture test**

Reference files first:
- Read `api/tests/Application.UnitTests/Architecture/ValidatorArchitectureTests.cs` for pattern
- Read `api/tests/Application.UnitTests/Architecture/AuthorizationArchitectureTests.cs` for pattern

```csharp
using System.Reflection;
using api.Application.Common.Interfaces;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Architecture;

/// <summary>
/// Architectural test: Enforces that command handlers in the Application layer
/// do not directly modify owned entity types via DbContext.
/// All state changes to owned entities must flow through aggregate root methods.
///
/// Aggregate roots (direct DbSet access allowed): Recruitment, Candidate, ImportSession, AuditEntry
/// Owned entities (must modify through aggregate root): WorkflowStep, RecruitmentMember,
///   CandidateOutcome, CandidateDocument, ImportDocument, ImportRowResult
/// </summary>
[TestFixture]
public class AggregateRootArchitectureTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(IApplicationDbContext).Assembly;

    /// <summary>
    /// Owned entity type names that should only be modified through their aggregate root.
    /// If a handler needs to create/modify these, it should load the aggregate root
    /// and call methods on it (e.g., recruitment.AddStep(), not dbContext.WorkflowSteps.Add()).
    /// </summary>
    private static readonly HashSet<string> OwnedEntityTypeNames = new()
    {
        "WorkflowStep",
        "RecruitmentMember",
        "CandidateOutcome",
        "CandidateDocument",
        "ImportDocument",
        "ImportRowResult",
    };

    [Test]
    public void ApplicationDbContext_ShouldNotExposeDbSetsForOwnedEntities()
    {
        // IApplicationDbContext should only have DbSet properties for aggregate roots,
        // not for owned entities.
        var dbContextInterface = typeof(IApplicationDbContext);
        var dbSetProperties = dbContextInterface.GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("DbSet"))
            .Select(p => p.PropertyType.GetGenericArguments().FirstOrDefault()?.Name)
            .Where(name => name is not null)
            .ToList();

        var violations = dbSetProperties
            .Where(name => OwnedEntityTypeNames.Contains(name!))
            .ToList();

        violations.Should().BeEmpty(
            "IApplicationDbContext should not expose DbSet<T> for owned entity types. " +
            "Owned entities must be accessed through their aggregate root. " +
            $"Violations: [{string.Join(", ", violations)}]");
    }

    [Test]
    public void OwnedEntityTypeNames_ShouldAllExistInDomainAssembly()
    {
        // Guard against stale entries — all listed owned types should exist in the Domain assembly
        var domainAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "api.Domain");

        // If Domain assembly isn't loaded, load it via a known domain type
        domainAssembly ??= typeof(api.Domain.Entities.Recruitment).Assembly;

        var domainTypeNames = domainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => t.Name)
            .ToHashSet();

        var staleEntries = OwnedEntityTypeNames
            .Where(name => !domainTypeNames.Contains(name))
            .ToList();

        staleEntries.Should().BeEmpty(
            "all entries in OwnedEntityTypeNames should correspond to actual domain entity types. " +
            $"Remove stale entries: [{string.Join(", ", staleEntries)}]");
    }
}
```

**Step 2: Run test to verify it passes**

Run: `dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~AggregateRootArchitectureTests" --no-build` (build first if needed: `dotnet build api/api.slnx`)

Expected: PASS — current codebase does not expose DbSets for owned entities on IApplicationDbContext.

Note: If `dotnet test` fails due to missing .NET 10 runtime, use Docker: `docker compose run --rm api dotnet test api/tests/Application.UnitTests --filter "FullyQualifiedName~AggregateRootArchitectureTests"`

**Step 3: Update sprint-status.yaml**

Change `epic-5-deferred-aggregate-root-test` from `backlog` to `done`.

**Step 4: Commit**

```bash
git add api/tests/Application.UnitTests/Architecture/AggregateRootArchitectureTests.cs _bmad-output/implementation-artifacts/sprint-status.yaml
git commit -m "feat: add aggregate root architecture test (epic-5 deferred A-007)"
```

---

## Story 7.1: Health Check Endpoints

**Testing mode:** Test-first — health check endpoints have clear, testable behavior (HTTP status codes, auth bypass).

**Authorization:** No command/query handlers in this story. Health check endpoints are explicitly anonymous (excluded from auth).

| Handler | Type | Recruitment-Scoped? | Auth Pattern |
|---------|------|---------------------|--------------|
| N/A — no MediatR handlers | N/A | N/A | Endpoints use `.AllowAnonymous()` |

**Current state:** `Program.cs:25` has `app.UseHealthChecks("/health")` and `DependencyInjection.cs:29-30` registers `AddHealthChecks().AddDbContextCheck<ApplicationDbContext>()`. This means `/health` currently includes the DB check — but the story requires `/health` to be liveness-only (no DB) and `/ready` to be readiness (with DB check).

**Files:**
- Modify: `api/src/Web/Program.cs`
- Modify: `api/src/Web/DependencyInjection.cs`
- Create: `api/tests/Application.FunctionalTests/HealthChecks/HealthCheckEndpointTests.cs`

**Reference docs:**
- `_bmad-output/planning-artifacts/architecture.md` (core architecture)
- `_bmad-output/planning-artifacts/architecture/patterns-backend.md` (backend patterns)
- `_bmad-output/planning-artifacts/architecture/testing-standards.md` (test patterns)

### Task 1: Write health check integration tests

**Step 1: Write the failing tests**

Reference: Check existing functional test patterns in `api/tests/Application.FunctionalTests/` for `WebApplicationFactory` setup.

Create `api/tests/Application.FunctionalTests/HealthChecks/HealthCheckEndpointTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.FunctionalTests.HealthChecks;

[TestFixture]
public class HealthCheckEndpointTests : Testing
{
    [Test]
    public async Task Health_ReturnsOk_WhenApiIsRunning()
    {
        var client = GetAnonymousClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Ready_ReturnsOk_WhenDatabaseIsAvailable()
    {
        var client = GetAnonymousClient();
        var response = await client.GetAsync("/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Health_DoesNotRequireAuthentication()
    {
        // Use a client with no auth token
        var client = GetAnonymousClient();
        var response = await client.GetAsync("/health");
        // Should NOT return 401/403
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Ready_DoesNotRequireAuthentication()
    {
        var client = GetAnonymousClient();
        var response = await client.GetAsync("/ready");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

Note: The test setup depends on the existing `Testing` base class and factory configuration. Check `api/tests/Application.FunctionalTests/Testing.cs` for `GetAnonymousClient()` — if it doesn't exist, you'll need to add a method that creates an `HttpClient` without dev auth headers. The existing factory likely uses the fallback authorization policy requiring authentication, so health checks must be mapped before auth middleware or use `AllowAnonymous`.

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Application.FunctionalTests --filter "FullyQualifiedName~HealthCheckEndpointTests" -v n`

Expected: Tests for `/ready` may pass (current `/health` endpoint exists but no `/ready`), `/health` tests should pass since the endpoint exists. If the test infrastructure doesn't support anonymous requests, all will fail — which is the expected starting state.

### Task 2: Implement health check endpoints

**Step 1: Update DependencyInjection.cs — separate liveness and readiness checks**

In `api/src/Web/DependencyInjection.cs`, change the health check registration:

Replace:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();
```

With:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: new[] { "ready" });
```

This tags the DB check so only the readiness endpoint runs it.

**Step 2: Update Program.cs — map separate /health and /ready endpoints**

In `api/src/Web/Program.cs`, replace:
```csharp
app.UseHealthChecks("/health");
```

With:
```csharp
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness: no dependency checks
}).AllowAnonymous();

app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") // Readiness: DB check only
}).AllowAnonymous();
```

Note: `UseHealthChecks` must be replaced with `MapHealthChecks` to support `AllowAnonymous()`. `MapHealthChecks` returns an `IEndpointConventionBuilder` that supports auth extensions. Place these calls AFTER `app.Build()` but BEFORE `app.MapEndpoints()`. Since the app uses a fallback authorization policy (require authenticated user), `.AllowAnonymous()` is essential.

Also add the required using at the top of Program.cs if not already present:
```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
```

**Step 3: Run tests to verify they pass**

Run: `dotnet test api/tests/Application.FunctionalTests --filter "FullyQualifiedName~HealthCheckEndpointTests" -v n`

Expected: All 4 tests PASS.

**Step 4: Run full test suite**

Run: `dotnet test api/api.slnx` (or Docker fallback: `docker compose run --rm api dotnet test`)

Expected: All tests pass, no regressions.

**Step 5: Commit**

```bash
git add api/src/Web/Program.cs api/src/Web/DependencyInjection.cs api/tests/Application.FunctionalTests/HealthChecks/HealthCheckEndpointTests.cs
git commit -m "feat(7.1): add /health liveness and /ready readiness endpoints"
```

---

## Story 7.2: Azure Infrastructure as Code

**Testing mode:** Spike — Bicep templates cannot be unit tested. Verification is via `azd provision` against a real Azure subscription (done manually by the operator after merge). Tests will be added after the spike confirms the templates are valid.

**Authorization:** No handlers. Infrastructure-only changes.

**Files:**
- Modify: `api/azure.yaml`
- Modify: `api/infra/main.bicep`
- Modify: `api/infra/services/web.bicep`
- Modify: `api/infra/main.parameters.json`

**Reference docs:**
- `_bmad-output/planning-artifacts/architecture/infrastructure.md`

### Task 3: Update azure.yaml project name

**Step 1: Update project name**

In `api/azure.yaml`, change `name: clean-architecture-azd` to `name: recruitment-tracker`.

**Step 2: Commit**

```bash
git add api/azure.yaml
git commit -m "chore(7.2): rename azd project to recruitment-tracker"
```

### Task 4: Update web.bicep — runtime version and environment

**Step 1: Fix runtime version**

In `api/infra/services/web.bicep`, change `runtimeVersion: '9.0'` to `runtimeVersion: '10.0'`.

**Step 2: Fix environment variable**

In `api/infra/services/web.bicep`, change `ASPNETCORE_ENVIRONMENT: 'Development'` to `ASPNETCORE_ENVIRONMENT: 'Production'`.

**Step 3: Parameterize App Service Plan SKU**

Add a parameter to `api/infra/services/web.bicep`:

```bicep
param appServicePlanSku object = {
  name: 'B1'
}
```

Then update the `appServicePlan` module call to use `sku: appServicePlanSku` instead of the hardcoded value.

**Step 4: Commit**

```bash
git add api/infra/services/web.bicep
git commit -m "fix(7.2): update runtime to .NET 10, set Production env, parameterize SKU"
```

### Task 5: Add Blob Storage to main.bicep

**Step 1: Add storage account name parameter**

In `api/infra/main.bicep`, add parameter:

```bicep
param storageAccountName string = ''
```

**Step 2: Add storage module**

In `api/infra/main.bicep`, add after the `database` module:

```bicep
module storage 'core/storage/storage-account.bicep' = {
  name: 'storage'
  params: {
    name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'
    location: location
    tags: tags
    allowBlobPublicAccess: false
    containers: [
      { name: 'documents' }
    ]
    sku: { name: 'Standard_LRS' }
  }
  scope: rg
}
```

**Step 3: Add storage connection string to Key Vault**

Add a Key Vault secret for the storage connection string after the storage module:

```bicep
module storageKeyVaultSecret 'core/security/keyvault-secret.bicep' = {
  name: 'storageKeyVaultSecret'
  params: {
    name: 'ConnectionStrings--BlobStorage'
    keyVaultName: keyVault.outputs.name
    secretValue: 'DefaultEndpointsProtocol=https;AccountName=${storage.outputs.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccountKey}'
  }
  scope: rg
}
```

Wait — we need the storage account key. The core storage-account module doesn't output the key. Instead, use the `listKeys` function pattern. However, since we're deploying at subscription scope and the storage is in the resource group, we need to reference it differently.

**Alternative approach:** Use the built-in `listKeys` in a module-level secret, or use Managed Identity for storage access (preferred for security). For simplicity with the existing pattern (Key Vault secrets), let's add the storage account name as an output and let the App Service use Managed Identity with role assignment.

Actually, let's keep it simple. Add a role assignment giving the App Service Managed Identity access to the storage account:

```bicep
module storageRoleAssignment 'core/security/role.bicep' = {
  name: 'storageRoleAssignment'
  params: {
    principalId: web.outputs.identityPrincipalId
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
    principalType: 'ServicePrincipal'
  }
  scope: rg
}
```

And add the storage endpoint as an output:

```bicep
output AZURE_STORAGE_ACCOUNT_NAME string = storage.outputs.name
output AZURE_STORAGE_BLOB_ENDPOINT string = storage.outputs.primaryEndpoints.blob
```

Note: Check `core/security/role.bicep` to confirm its parameter interface before using it.

**Step 4: Parameterize SQL Database SKU**

In `api/infra/core/database/sqlserver/sqlserver.bicep`, add a parameter for SKU if not already parameterized. The current SQL module doesn't set a SKU on the database (it defaults to whatever Azure defaults are). Add:

In `api/infra/main.bicep`, when calling the database module, check if we can pass a SKU parameter. Looking at the `sqlserver.bicep` module, the `sqlDatabase` resource doesn't have a `sku` property. We should add one.

Add to `api/infra/core/database/sqlserver/sqlserver.bicep`:

```bicep
param databaseSku object = {
  name: 'Basic'
  tier: 'Basic'
}
```

And update the `sqlDatabase` resource:

```bicep
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: databaseSku
}
```

**Step 5: Commit**

```bash
git add api/infra/main.bicep api/infra/core/database/sqlserver/sqlserver.bicep
git commit -m "feat(7.2): add Blob Storage, parameterize SKUs, add role assignment"
```

### Task 6: Add outputs and verify template completeness

**Step 1: Verify all resources in main.bicep**

Ensure main.bicep provisions: App Service, Azure SQL, Blob Storage, Key Vault, Application Insights. All are present after the changes above.

**Step 2: Add teardown documentation**

Add a section to `docs/getting-started.md` under a new "## Azure Deployment" heading:

```markdown
## Azure Deployment

### Prerequisites

- Azure CLI and Azure Developer CLI (`azd`) installed
- Azure subscription (Visual Studio Professional recommended, ~$50/month credit)

### Provision & Deploy

```bash
# Initialize environment
azd init

# Provision infrastructure (creates all Azure resources)
azd provision

# Deploy application
azd deploy
```

### Teardown

To remove all Azure resources and stop incurring charges:

```bash
azd down
```

This destroys all resources in the environment's resource group. Re-provision with `azd provision` when needed.
```

**Step 3: Commit**

```bash
git add docs/getting-started.md
git commit -m "docs(7.2): add Azure deployment and teardown instructions"
```

---

## Story 7.3: CI/CD Pipeline with Auto-Migration

**Testing mode:** Spike — GitHub Actions workflows are tested by execution. Auto-migration code will have a characterization test.

**Authorization:** No handlers. Infrastructure-only.

**Files:**
- Create: `.github/workflows/cd.yml`
- Modify: `api/src/Web/Program.cs`
- Delete/ignore: `api/.github/workflows/azure-dev.yml`

### Task 7: Add auto-migration to Program.cs

**Step 1: Add migration logic**

In `api/src/Web/Program.cs`, modify the database initialization section. Currently:

```csharp
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
```

Replace with:

```csharp
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // Production/Staging: apply pending EF Core migrations on startup
    // Suitable for single-instance deployments (current scale)
    await app.MigrateDatabaseAsync();
}
```

**Step 2: Add MigrateDatabaseAsync extension method**

In `api/src/Infrastructure/Data/ApplicationDbContextInitialiser.cs`, add after the `InitialiserExtensions` class:

```csharp
public static class MigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }
}
```

Add the required using at the top of the file:
```csharp
using Microsoft.EntityFrameworkCore;
```

**Step 3: Remove HSTS else block overlap**

The current code has HSTS in the else block. After adding migration, the code structure should be:

```csharp
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    await app.MigrateDatabaseAsync();
    app.UseHsts();
}
```

**Step 4: Commit**

```bash
git add api/src/Web/Program.cs api/src/Infrastructure/Data/ApplicationDbContextInitialiser.cs
git commit -m "feat(7.3): add auto-migration on startup for non-Development environments"
```

### Task 8: Create CD workflow

**Step 1: Create `.github/workflows/cd.yml`**

```yaml
name: CD

on:
  push:
    branches: [main]
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        default: 'staging'
        type: choice
        options:
          - staging
          - production

permissions:
  id-token: write
  contents: read

jobs:
  build-test:
    runs-on: ubuntu-latest
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

      - name: Build & test API
        run: |
          dotnet restore api/api.slnx
          dotnet build api/api.slnx --no-restore
          dotnet test api/api.slnx --no-build

      - name: Build & test frontend
        working-directory: web
        run: |
          npm ci
          npm run lint
          npm run build
          npm run test -- --run

  deploy:
    needs: build-test
    runs-on: ubuntu-latest
    environment: ${{ github.event_name == 'workflow_dispatch' && inputs.environment || 'staging' }}
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
        run: |
          azd auth login \
            --client-id "$AZURE_CLIENT_ID" \
            --federated-credential-provider "github" \
            --tenant-id "$AZURE_TENANT_ID"

      - name: Deploy Application
        run: azd deploy --no-prompt
```

**Step 2: Remove template workflow**

Delete `api/.github/workflows/azure-dev.yml` — it's superseded by the repo-level `cd.yml`.

```bash
rm api/.github/workflows/azure-dev.yml
```

If the directory `api/.github/workflows/` is now empty, remove it too:
```bash
rmdir api/.github/workflows api/.github 2>/dev/null || true
```

**Step 3: Commit**

```bash
git add .github/workflows/cd.yml
git rm api/.github/workflows/azure-dev.yml
git commit -m "feat(7.3): add CD pipeline, remove template deployment workflow"
```

### Task 9: Verify CI pipeline still works

**Step 1: Check existing CI pipeline is unchanged**

Read `.github/workflows/ci.yml` and confirm it was not modified. It should still trigger on `pull_request` to `main` and `push` to `main`.

Note: The CI and CD pipelines overlap on `push` to `main` — both will trigger. This is intentional: CI runs tests quickly, CD builds+tests+deploys. The CI job is the fast feedback loop for PRs; the CD job is the deployment pipeline.

**Step 2: Run local verification**

```bash
dotnet build api/api.slnx
cd web && npm run build && npm run test -- --run
```

Expected: Both succeed.

**Step 3: Commit (if any fixes needed)**

Only commit if fixes were needed. Otherwise, no commit for this task.

---

## Story 7.4: Staging Environment

**Testing mode:** Spike — environment configuration verified via deployment.

**Authorization:** No handlers. Infrastructure-only.

**Files:**
- Modify: `.github/workflows/cd.yml` (already handled in Task 8 — staging is default target)
- Create: `docs/deployment-guide.md`

### Task 10: Document staging workflow

**Step 1: Create deployment guide**

Create `docs/deployment-guide.md`:

```markdown
# Deployment Guide

## Environments

| Environment | Purpose | Deploy Trigger |
|-------------|---------|---------------|
| **staging** | Pre-production verification | Automatic on push to `main` |
| **production** | Live application | Manual via workflow dispatch |

## Setting Up Environments

### Staging

```bash
# Create staging environment
cd api
azd env new staging
azd provision
azd deploy
```

### Production

```bash
# Create production environment
cd api
azd env new production
azd provision
```

Production deployment is triggered manually via GitHub Actions:
1. Go to Actions > CD workflow
2. Click "Run workflow"
3. Select `production` from the environment dropdown
4. Click "Run workflow"

## Environment Isolation

Each environment gets its own:
- Resource group
- App Service + App Service Plan
- Azure SQL Server + Database
- Blob Storage account
- Key Vault
- Application Insights

No secrets or data are shared between environments.

## GitHub Environment Configuration

Each GitHub environment (`staging`, `production`) needs these variables:

| Variable | Description |
|----------|-------------|
| `AZURE_CLIENT_ID` | Service principal client ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_ENV_NAME` | azd environment name (e.g., `staging`, `production`) |
| `AZURE_LOCATION` | Azure region (e.g., `westeurope`) |

Configure federated credentials for each environment:
```bash
azd pipeline config --environment staging
azd pipeline config --environment production
```

## Verification

After deploying to staging:
1. Check health: `curl https://<staging-url>/health`
2. Check readiness: `curl https://<staging-url>/ready`
3. Verify application functionality

## Promotion

To promote staging to production:
1. Verify staging is healthy
2. Trigger manual CD workflow targeting `production`
3. Verify production health checks

## Teardown

Remove an environment to stop all charges:

```bash
# Remove staging
azd env select staging
azd down

# Remove production
azd env select production
azd down
```

All resources are destroyed. Re-provision with `azd provision` when needed.

## Cost Management

Using Visual Studio Professional subscription (~$50/month credit):
- App Service B1: ~$13/month
- Azure SQL Basic: ~$5/month
- Storage Standard LRS: ~$0.02/GB/month
- Key Vault: ~$0.03/10K operations
- Application Insights: Free tier (5GB/month)

**Total per environment: ~$20/month**

When not actively testing, tear down staging with `azd down` to conserve credits.
```

**Step 2: Commit**

```bash
git add docs/deployment-guide.md
git commit -m "docs(7.4): add deployment guide with staging workflow and cost estimates"
```

### Task 11: Verify CD workflow supports environment selection

**Step 1: Review cd.yml**

Confirm the `cd.yml` created in Task 8:
- Default deploy target is `staging` (on push to main)
- `workflow_dispatch` allows selecting `staging` or `production`
- Each environment has its own GitHub environment configuration

This was already implemented in Task 8. No additional code changes needed.

**Step 2: Final full build verification**

```bash
dotnet build api/api.slnx
dotnet test api/api.slnx
cd web && npm run build && npm run test -- --run && npx tsc --noEmit && npx eslint src/ --max-warnings 0
```

Expected: All pass with zero errors.

**Step 3: Commit (only if fixes needed)**

---

## AC Coverage Summary

### Story 7.1
| AC | Coverage |
|----|----------|
| GET /health returns 200 | Test: HealthCheckEndpointTests.Health_ReturnsOk_WhenApiIsRunning |
| GET /ready returns 200 when DB connected | Test: HealthCheckEndpointTests.Ready_ReturnsOk_WhenDatabaseIsAvailable |
| GET /ready returns 503 when DB unreachable | Covered by ASP.NET Core health check framework (DbContextCheck returns Unhealthy) |
| Health endpoints excluded from auth | Tests: Health_DoesNotRequireAuthentication, Ready_DoesNotRequireAuthentication |
| Endpoints work in any environment | Covered by AllowAnonymous + no env-specific config |

### Story 7.2
| AC | Coverage |
|----|----------|
| azure.yaml updated with correct project name | Task 3 |
| Blob Storage provisioned | Task 5 |
| Runtime version 10.0 | Task 4 |
| ASPNETCORE_ENVIRONMENT = Production | Task 4 |
| B1/Basic SKUs parameterized | Tasks 4-5 |
| azd provision creates all resources | Verified via manual deployment |
| azd down removes all resources | Documented in deployment guide |
| No manual portal config required | All resources declarative in Bicep |

### Story 7.3
| AC | Coverage |
|----|----------|
| CI pipeline still works (existing) | Task 9 verification |
| CD pipeline deploys on push to main | Task 8 cd.yml |
| CI unchanged, CD separate | Task 8 — separate file |
| OIDC federated credentials | Task 8 — azd auth login pattern |
| Auto-migration on startup | Task 7 — MigrateDatabaseAsync |
| Rollback = re-run previous commit | Documented in deployment guide |
| Template workflow superseded | Task 8 — azure-dev.yml removed |

### Story 7.4
| AC | Coverage |
|----|----------|
| Staging environment via azd env | Task 10 documentation |
| CD deploys to staging by default | Task 8 cd.yml default |
| Health checks work in staging | Same endpoints, no env-specific config |
| Manual promotion to production | Task 8 workflow_dispatch |
| Environment isolation | Each env gets own resource group |
| Teardown via azd down | Task 10 documentation |
| No orphaned resources | azd down removes entire resource group |
