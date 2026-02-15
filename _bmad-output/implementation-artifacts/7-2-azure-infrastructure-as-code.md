# Story 7.2: Azure Infrastructure as Code

Status: ready-for-dev

## Story

As a **platform operator**,
I want all Azure infrastructure defined declaratively in Bicep templates,
so that I can reproduce the full environment from source control within 30 minutes.

## Acceptance Criteria

### AC1: azure.yaml reflects actual project identity
**Given** the existing azd scaffolding in `api/infra/`
**When** `azure.yaml` is updated
**Then** the project name is `recruitment-tracker` (not `clean-architecture-azd`) and the service definition reflects the actual API project path (`./src/Web`)

### AC2: Blob Storage provisioned for document storage
**Given** the existing `main.bicep` provisions App Service, SQL, Key Vault, and Monitoring
**When** the Bicep templates are updated
**Then** Azure Blob Storage is provisioned for document storage with a `documents` container
**And** the Blob Storage connection string is stored in Key Vault
**And** the App Service can access the storage connection string via Key Vault reference

### AC3: App Service runtime corrected to .NET 10
**Given** the App Service runtime is currently configured as `dotnetcore|9.0`
**When** the Bicep templates are updated
**Then** the App Service runtime version is `10.0` (not `9.0`)
**And** `ASPNETCORE_ENVIRONMENT` is set to `Production` (not `Development`)

### AC4: Cost-effective SKUs with parameterization
**Given** the deployment targets a Visual Studio Professional Subscription (~$50/month credit)
**When** resource SKUs are configured
**Then** App Service uses B1 tier, Azure SQL uses Basic tier (5 DTU), and storage uses Standard_LRS
**And** SKU values are parameterized in Bicep so they can be overridden for different environments

### AC5: Full environment provisioning within 30 minutes
**Given** the complete Bicep templates
**When** `azd provision` is run against a clean Azure subscription
**Then** all required resources are created: App Service, Azure SQL, Blob Storage, Key Vault, Application Insights
**And** the environment is fully functional within 30 minutes

### AC6: Clean teardown with azd down
**Given** a deployed environment is no longer needed
**When** `azd down` is executed
**Then** all provisioned resources are removed and the Azure subscription incurs no further charges
**And** teardown instructions are documented in the deployment guide

### AC7: Declarative infrastructure, no manual portal configuration
**Given** the infrastructure templates
**When** a developer reviews the Bicep files
**Then** all resources are defined declaratively with no manual Azure portal configuration required

### Prerequisites
- None. This story can be implemented independently.

### Cross-References
- **Story 7.1** (Health Check Endpoints): The `healthCheckPath: '/health'` in `services/web.bicep` must match the `/health` endpoint implemented in Story 7.1. The value is already correct in the template.
- **Story 7.3** (CI/CD Pipeline): The `cd.yml` workflow will use `azd provision` and `azd deploy` against these templates. Story 7.3 depends on this story being complete.
- **Story 7.4** (Staging Environment): Uses these same templates with different `azd env` names. Parameterized SKUs enable environment-specific overrides.

### FRs Fulfilled
- None (infrastructure story, no functional requirements)

### NFRs Addressed
- **NFR37:** Infrastructure defined as code (Bicep templates)
- **NFR38 (amended):** API deployed to Azure App Service using azd deployment pipeline

## Tasks / Subtasks

- [ ] Task 1: Update `azure.yaml` project identity (AC: #1)
  - [ ] 1.1 In `api/azure.yaml`, change `name:` from `clean-architecture-azd` to `recruitment-tracker`
  - [ ] 1.2 Verify `services.web.project` path is `./src/Web` (already correct)
  - [ ] 1.3 Verify `services.web.host` is `appservice` (already correct)

- [ ] Task 2: Add Blob Storage module to `main.bicep` (AC: #2, #4)
  - [ ] 2.1 Add parameter `param storageAccountName string = ''` to `api/infra/main.bicep`
  - [ ] 2.2 Add parameter `param storageSku string = 'Standard_LRS'` for SKU parameterization
  - [ ] 2.3 Add storage module invocation referencing existing `core/storage/storage-account.bicep`:
    ```bicep
    module storage 'core/storage/storage-account.bicep' = {
      name: 'storage'
      params: {
        name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'
        location: location
        tags: tags
        sku: { name: storageSku }
        allowBlobPublicAccess: false
        allowSharedKeyAccess: true
        containers: [
          { name: 'documents', publicAccess: 'None' }
        ]
      }
      scope: rg
    }
    ```
  - [ ] 2.4 Note: Storage account names must be 3-24 chars, lowercase alphanumeric only. The abbreviation `st` + `resourceToken` (13 chars from `uniqueString`) = 15 chars, which is valid.

- [ ] Task 3: Store Blob Storage connection string in Key Vault (AC: #2)
  - [ ] 3.1 Add a Key Vault secret module for the storage connection string in `main.bicep`:
    ```bicep
    module storageConnectionStringSecret 'core/security/keyvault-secret.bicep' = {
      name: 'storageConnectionStringSecret'
      params: {
        name: 'ConnectionStrings--BlobStorage'
        keyVaultName: keyVault.outputs.name
        secretValue: 'DefaultEndpointsProtocol=https;AccountName=${storage.outputs.name};AccountKey=${storageAccountKey};EndpointSuffix=${environment().suffixes.storage}'
      }
      scope: rg
    }
    ```
  - [ ] 3.2 To get the storage account key, add a `listKeys` reference. Since the `core/storage/storage-account.bicep` module doesn't output keys, use an inline approach in `main.bicep`:
    ```bicep
    // After the storage module, reference the account to get keys
    // Option A: Add an output to storage-account.bicep (modifies core module)
    // Option B: Use a separate inline resource reference (preferred — avoids modifying shared modules)
    ```
    **Recommended approach:** Do NOT modify `core/storage/storage-account.bicep`. Instead, create a small helper module `api/infra/app/storage-secret.bicep` that:
    - Takes `storageAccountName` and `keyVaultName` as params
    - References the existing storage account
    - Calls `listKeys()` to get the account key
    - Creates the Key Vault secret with the full connection string
    See Dev Notes for the full module code.
  - [ ] 3.3 Add output `output AZURE_STORAGE_CONNECTION_STRING_KEY string = 'ConnectionStrings--BlobStorage'` to `main.bicep`

- [ ] Task 4: Update App Service runtime and environment (AC: #3)
  - [ ] 4.1 In `api/infra/services/web.bicep`, change `runtimeVersion: '9.0'` to `runtimeVersion: '10.0'`
  - [ ] 4.2 In `api/infra/services/web.bicep`, change `ASPNETCORE_ENVIRONMENT: 'Development'` to `ASPNETCORE_ENVIRONMENT: 'Production'`

- [ ] Task 5: Parameterize SKUs in `main.bicep` (AC: #4)
  - [ ] 5.1 Add parameter `param appServiceSkuName string = 'B1'` to `main.bicep`
  - [ ] 5.2 Add parameter `param sqlDatabaseSku object = { name: 'Basic', tier: 'Basic', capacity: 5 }` to `main.bicep`
  - [ ] 5.3 Pass `appServiceSkuName` through to `services/web.bicep` (add a `skuName` param to `web.bicep` that replaces the hardcoded `'B1'`)
  - [ ] 5.4 Pass `sqlDatabaseSku` to the `database` module. This requires updating `core/database/sqlserver/sqlserver.bicep` to accept an optional SKU param:
    - Add `param databaseSku object = {}` to `sqlserver.bicep`
    - On the `sqlDatabase` resource, add `sku: !empty(databaseSku) ? databaseSku : null`
    - Pass `databaseSku: sqlDatabaseSku` from `main.bicep`
  - [ ] 5.5 The `storageSku` parameter was already added in Task 2.2
  - [ ] 5.6 Key Vault uses `standard` SKU family by default — this is already the lowest tier, no change needed
  - [ ] 5.7 Log Analytics uses `PerGB2018` SKU by default — this is the only SKU available, no change needed

- [ ] Task 6: Create `app/storage-secret.bicep` helper module (AC: #2)
  - [ ] 6.1 Create `api/infra/app/storage-secret.bicep` (see Dev Notes for full code)
  - [ ] 6.2 Reference from `main.bicep`:
    ```bicep
    module storageSecret 'app/storage-secret.bicep' = {
      name: 'storageSecret'
      params: {
        storageAccountName: storage.outputs.name
        keyVaultName: keyVault.outputs.name
        secretName: 'ConnectionStrings--BlobStorage'
      }
      scope: rg
    }
    ```

- [ ] Task 7: Add App Service Key Vault access for Blob Storage (AC: #2)
  - [ ] 7.1 The `webKeyVaultAccess` module in `main.bicep` already grants `get` + `list` on secrets. This is sufficient for the App Service to read the Blob Storage connection string from Key Vault. No changes needed.

- [ ] Task 8: Add deployment guide / teardown documentation (AC: #6)
  - [ ] 8.1 Create or update `docs/deployment-guide.md` with:
    - Prerequisites (Azure CLI, azd CLI, VS Pro subscription)
    - `azd init` / `azd env new` instructions
    - `azd provision` to create infrastructure
    - `azd deploy` to deploy the application
    - `azd down` to tear down all resources
    - Cost estimates for the configured SKUs
    - Environment variable reference

- [ ] Task 9: Validate Bicep templates (AC: #5, #7)
  - [ ] 9.1 Run `az bicep build --file api/infra/main.bicep` to validate syntax
  - [ ] 9.2 Run `az bicep lint --file api/infra/main.bicep` to check for best practice warnings
  - [ ] 9.3 Run `azd provision --preview` (if available) or review the generated ARM template to verify all resources are declared
  - [ ] 9.4 Verify the storage account naming constraint: `st` prefix + `uniqueString` result = 15 chars (within 3-24 limit)

## Dev Notes

### Guardrails
- **Do NOT rewrite Bicep from scratch.** Adapt the existing `api/infra/main.bicep` and `api/infra/services/web.bicep`.
- **Leave unused `infra/core/` modules in place.** They are from the Clean Architecture template and are inert (AI, Cosmos, Container Apps, AKS, etc.). They cost nothing and removing them risks breaking module references.
- **Do NOT modify `core/storage/storage-account.bicep`** unless absolutely necessary. It is a shared module from the azd template. Create an `app/` layer module instead.
- **The `core/database/sqlserver/sqlserver.bicep` change is minimal** — adding an optional `databaseSku` param with an empty object default. Existing behavior is preserved when no SKU is passed (Azure will use the default service tier).

### Current State of Infrastructure (what already exists)

| Resource | Module | Status |
|----------|--------|--------|
| Resource Group | inline in `main.bicep` | Exists |
| App Service Plan | `core/host/appserviceplan.bicep` | Exists, B1 hardcoded in `services/web.bicep` |
| App Service | `core/host/appservice.bicep` | Exists, runtime 9.0, env=Development |
| Azure SQL Server + DB | `core/database/sqlserver/sqlserver.bicep` | Exists, no SKU specified (uses default) |
| Key Vault | `core/security/keyvault.bicep` | Exists, standard tier |
| Key Vault Access (App Service) | `core/security/keyvault-access.bicep` | Exists |
| Log Analytics | `core/monitor/loganalytics.bicep` | Exists |
| Application Insights | `core/monitor/applicationinsights.bicep` | Exists |
| App Insights Dashboard | `core/monitor/applicationinsights-dashboard.bicep` | Exists |
| **Blob Storage** | `core/storage/storage-account.bicep` | **Module exists but NOT referenced from main.bicep** |

### Gap Analysis

| What Needs to Change | File | Change Type |
|----------------------|------|-------------|
| Project name `clean-architecture-azd` -> `recruitment-tracker` | `api/azure.yaml` | Edit line 4 |
| Runtime version `9.0` -> `10.0` | `api/infra/services/web.bicep` | Edit line 31 |
| Environment `Development` -> `Production` | `api/infra/services/web.bicep` | Edit line 34 |
| Add Blob Storage module reference | `api/infra/main.bicep` | Add module block |
| Add Storage connection string to Key Vault | New `api/infra/app/storage-secret.bicep` | New file |
| Parameterize App Service SKU | `api/infra/main.bicep` + `api/infra/services/web.bicep` | Add params |
| Parameterize SQL Database SKU | `api/infra/main.bicep` + `api/infra/core/database/sqlserver/sqlserver.bicep` | Add optional param |
| Parameterize Storage SKU | `api/infra/main.bicep` | Add param (passed to storage module) |
| Add deployment/teardown docs | `docs/deployment.md` | New file |

### `app/storage-secret.bicep` — Full Module Code

```bicep
metadata description = 'Stores Blob Storage connection string in Key Vault.'

param storageAccountName string
param keyVaultName string
param secretName string = 'ConnectionStrings--BlobStorage'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: secretName
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
  }
}

output secretName string = secretName
```

### Updated `services/web.bicep` — Changes Required

```bicep
// Add new parameter (line ~8):
param skuName string = 'B1'

// Change appServicePlan sku (line ~16):
sku: {
  name: skuName
}

// Change runtimeVersion (line ~31):
runtimeVersion: '10.0'

// Change appSettings (line ~33-35):
appSettings: {
  ASPNETCORE_ENVIRONMENT: 'Production'
}
```

### Updated `main.bicep` — New Parameters to Add

```bicep
// SKU parameters (add after existing params)
@description('App Service Plan SKU name (default: B1 for test deployment)')
param appServiceSkuName string = 'B1'

@description('Azure SQL Database SKU (default: Basic tier for test deployment)')
param sqlDatabaseSku object = {
  name: 'Basic'
  tier: 'Basic'
  capacity: 5
}

@description('Storage Account SKU (default: Standard_LRS for test deployment)')
param storageSku string = 'Standard_LRS'

param storageAccountName string = ''
```

### Updated `main.bicep` — Pass SKU to web module

```bicep
module web 'services/web.bicep' = {
  name: 'web'
  params: {
    name: !empty(appServiceName) ? appServiceName : '${abbrs.webSitesAppService}${resourceToken}'
    location: location
    tags: tags
    serviceName: webServiceName
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    keyVaultName: keyVault.outputs.name
    skuName: appServiceSkuName   // NEW
  }
  scope: rg
}
```

### Updated `main.bicep` — Pass SKU to database module

```bicep
module database 'core/database/sqlserver/sqlserver.bicep' = {
  name: 'database'
  params: {
    name: !empty(dbServerName) ? dbServerName : '${abbrs.sqlServers}${resourceToken}'
    location: location
    tags: tags
    databaseName: !empty(dbName) ? dbName : '${abbrs.sqlServersDatabases}${resourceToken}'
    keyVaultName: keyVault.outputs.name
    connectionStringKey: 'ConnectionStrings--apiDb'
    sqlAdminPassword: dbAdminPassword
    appUserPassword: dbAppUserPassword
    databaseSku: sqlDatabaseSku   // NEW
  }
  scope: rg
}
```

### Updated `core/database/sqlserver/sqlserver.bicep` — Minimal Change

```bicep
// Add parameter (after existing params):
@description('Optional database SKU. If empty, Azure uses the default tier.')
param databaseSku object = {}

// Update sqlDatabase resource:
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: !empty(databaseSku) ? databaseSku : null
}
```

### Cost Estimate (Default SKUs for Test Deployment)

| Resource | SKU | Approx Monthly Cost |
|----------|-----|-------------------|
| App Service Plan | B1 (1 core, 1.75 GB) | ~$13 |
| Azure SQL | Basic (5 DTU, 2 GB) | ~$5 |
| Blob Storage | Standard_LRS | ~$0.02/GB (negligible for test) |
| Key Vault | Standard | ~$0.03/10K operations (negligible) |
| Log Analytics | PerGB2018 | Free tier first 5 GB/month |
| Application Insights | (uses Log Analytics) | Included in Log Analytics |
| **Total** | | **~$18-20/month** |

This fits well within the ~$50/month VS Pro subscription credit.

### Testing Strategy

Since this is an infrastructure story, there is no application code to unit test. Validation approach:

1. **Static validation:** `az bicep build` and `az bicep lint` catch syntax errors and best-practice violations locally.
2. **What-if deployment:** `az deployment sub what-if` previews resource changes without actually provisioning.
3. **Provisioning test:** Run `azd provision` against a test Azure subscription and verify all resources appear in the portal.
4. **Smoke test:** After provisioning, verify the App Service exists with correct runtime and settings via `az webapp show`.
5. **Teardown test:** Run `azd down` and verify all resources are removed.

The dev agent should at minimum complete step 1 (static validation). Steps 2-5 require Azure credentials and are typically done by the platform operator.

### File List (All Files Modified or Created)

| Action | File Path |
|--------|-----------|
| Edit | `api/azure.yaml` |
| Edit | `api/infra/main.bicep` |
| Edit | `api/infra/services/web.bicep` |
| Edit | `api/infra/core/database/sqlserver/sqlserver.bicep` |
| Create | `api/infra/app/storage-secret.bicep` |
| Create | `docs/deployment-guide.md` |

### Project Structure Notes

```
api/
  azure.yaml                               # azd project config (edit: name)
  infra/
    main.bicep                             # orchestrator (edit: add storage, params)
    main.parameters.json                   # azd parameter bindings (no changes needed)
    abbreviations.json                     # naming prefixes (has storageStorageAccounts: "st")
    app/
      storage-secret.bicep                 # NEW: stores blob conn string in KV
    services/
      web.bicep                            # App Service config (edit: runtime, env, sku param)
    core/
      database/sqlserver/
        sqlserver.bicep                    # SQL Server module (edit: add optional sku param)
      storage/
        storage-account.bicep              # Blob Storage module (EXISTS, no changes)
      security/
        keyvault.bicep                     # Key Vault (no changes)
        keyvault-access.bicep              # KV access policy (no changes)
        keyvault-secret.bicep              # KV secret helper (no changes — not used directly)
      monitor/
        monitoring.bicep                   # Log Analytics + App Insights (no changes)
      host/
        appservice.bicep                   # App Service resource (no changes)
        appserviceplan.bicep               # App Service Plan (no changes)
docs/
  deployment-guide.md                      # NEW: provisioning + teardown guide
```

### References
- [azd CLI documentation](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/)
- [Bicep documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure App Service pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)
- [Azure SQL Database pricing](https://azure.microsoft.com/en-us/pricing/details/azure-sql-database/single/)
- Architecture shard: `_bmad-output/planning-artifacts/architecture/infrastructure.md`
- Epic definition: `_bmad-output/planning-artifacts/epics/epic-7-deployment-infrastructure-automation.md`

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
