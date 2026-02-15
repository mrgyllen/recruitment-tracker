# Azure Deployment Guide

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (v2.50+)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) (v1.5+)
- An Azure subscription (Visual Studio Professional subscription with ~$50/month credit is sufficient)
- .NET 10 SDK (for local builds before deploy)

## Environments

| Environment | Purpose | Deploy Trigger |
|-------------|---------|---------------|
| **staging** | Pre-production verification | Automatic on push to `main` |
| **production** | Live application | Manual via workflow dispatch |

Each environment gets fully isolated resources (own resource group, database, storage, Key Vault, App Service). No secrets or data are shared between environments.

## Provisioned Resources (Per Environment)

| Resource | SKU (default) | Approx Monthly Cost |
|----------|---------------|---------------------|
| App Service Plan | B1 (1 core, 1.75 GB) | ~$13 |
| Azure SQL Database | Basic (5 DTU, 2 GB) | ~$5 |
| Blob Storage | Standard_LRS | ~$0.02/GB |
| Key Vault | Standard | ~$0.03/10K ops |
| Log Analytics + App Insights | PerGB2018 | Free first 5 GB/month |
| **Total per environment** | | **~$19/month** |
| **Both environments** | | **~$38/month** |

This fits within the ~$50/month VS Professional subscription credit. Tear down staging when not verifying to stay well under budget.

## Environment Setup

### Initial Setup (One-Time)

```bash
cd api

# Authenticate with Azure
azd auth login

# Create and provision staging
azd env new staging
azd env set AZURE_LOCATION westeurope
azd provision

# Create and provision production
azd env new production
azd env set AZURE_LOCATION westeurope
azd provision
```

`azd provision` will prompt for `dbAdminPassword` and `dbAppUserPassword`. Each environment generates its own unique passwords stored in its own Key Vault.

Provisioning typically completes within 15-20 minutes per environment.

### Configure GitHub Actions Credentials

```bash
cd api

# Configure federated credentials for GitHub Actions
azd env select staging
azd pipeline config
```

This sets up OIDC federated credentials so the CD pipeline can deploy without stored secrets.

Each GitHub Environment needs its own federated credential on the Azure AD app registration. In **Azure Portal > App registrations > your app > Certificates & secrets > Federated credentials**, add a credential per environment with entity type **Environment** and the environment name (`staging` or `production`).

For the `production` environment, consider enabling protection rules under **Settings > Environments > production**: required reviewers and/or a wait timer before deployment starts.

### GitHub Environment Variables

The CD pipeline reads these from GitHub Environment variables. Create two GitHub Environments (`staging` and `production`) under **Settings > Environments** and set these variables in each:

| Variable | Description |
|----------|-------------|
| `AZURE_CLIENT_ID` | Service principal client ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_LOCATION` | Azure region (e.g., `westeurope`) |

`AZURE_ENV_NAME` is **not** a GitHub variable -- it is derived dynamically in the CD workflow (staging on push to main, operator's choice on manual dispatch).

## Daily Workflow

```
Developer pushes to main
  -> CI runs (ci.yml: build + test for api/ and web/)
  -> CD runs (cd.yml: build + test + deploy to staging)
  -> Operator verifies staging via health checks and manual testing
  -> If good: operator triggers workflow_dispatch with environment=production
  -> Production updated
```

### Deploy to Staging (Automatic)

Every push to `main` triggers the CD pipeline, which deploys to staging automatically. The pipeline:
1. Builds and tests both API and frontend
2. Authenticates with Azure via OIDC
3. Deploys to the staging environment via `azd deploy`
4. Verifies deployment health (retries for up to 60 seconds)

### Deploy to Production (Manual)

1. Go to GitHub Actions > CD workflow
2. Click "Run workflow"
3. Select `production` from the environment dropdown
4. Click "Run workflow"

Production deployment is never automatic. The `workflow_dispatch` trigger with explicit environment selection is the only path to production.

### Verify Deployment

```bash
# Get the App Service URL
cd api
azd env select staging
azd env get-values | grep WEB_BASE_URI

# Check liveness
curl https://<app-url>/health

# Check readiness (database connectivity)
curl https://<app-url>/ready
```

Both endpoints should return HTTP 200 with `Healthy` status.

### Rollback

To roll back a deployment, re-run the CD pipeline for the previous known-good commit:

1. Go to GitHub Actions > CD workflow
2. Click "Run workflow"
3. Use the branch/ref of the last known-good state
4. The pipeline rebuilds and redeploys that version

Database rollback: There is no automatic reverse-migration. If a migration introduced a breaking schema change, create a new forward migration that reverts the change and deploy it normally.

## Override Default SKUs

Default SKUs are cost-optimized for a test/dev subscription. Override them per environment:

```bash
azd env select staging

# Use a more powerful App Service tier
azd env set appServiceSkuName S1

# Use a larger SQL Database tier
azd env set sqlDatabaseSku '{"name":"S0","tier":"Standard","capacity":10}'

# Use geo-redundant storage
azd env set storageSku Standard_GRS

# Apply changes
azd provision
```

## Cost Management

```bash
# Tear down staging when not in use (saves ~$19/month)
cd api
azd env select staging
azd down

# Re-provision when ready to test again (~15-20 minutes)
azd env select staging
azd provision
```

## Teardown

To remove all provisioned resources and stop incurring charges:

```bash
cd api

# Remove staging
azd env select staging
azd down

# Remove production
azd env select production
azd down
```

This deletes the resource group and all contained resources (App Service, SQL Server, Storage Account, Key Vault, Log Analytics, Application Insights).

To also remove the local azd environment configuration:

```bash
azd env delete staging
azd env delete production
```

## Environment Isolation Verification

After provisioning both environments, verify isolation:

- Staging resource group is `rg-staging`, production is `rg-production`
- Each has its own Key Vault with independent secrets
- Each has its own SQL Server + database with unique passwords
- Each has its own App Service with separate URL
- Each has its own Blob Storage account
- Each has its own Application Insights instance

## Environment Variables

After provisioning, `azd` stores these outputs in the local `.azure/<env>/.env` file:

| Variable | Description |
|----------|-------------|
| `AZURE_LOCATION` | Azure region |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_KEY_VAULT_NAME` | Key Vault resource name |
| `AZURE_KEY_VAULT_ENDPOINT` | Key Vault URI |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string |
| `AZURE_SQL_CONNECTION_STRING_KEY` | Key Vault secret name for SQL connection string |
| `AZURE_STORAGE_BLOB_ENDPOINT` | Blob Storage endpoint URL |
| `AZURE_STORAGE_CONNECTION_STRING_KEY` | Key Vault secret name for Blob Storage connection string |
| `WEB_BASE_URI` | Deployed App Service URL |

View all values with:

```bash
azd env get-values
```

## Troubleshooting

- **`azd provision` fails**: Check that `AZURE_LOCATION` supports all resource types. Check subscription quota limits.
- **Health check fails after deploy**: Check App Service logs via `az webapp log tail --name <app> --resource-group rg-staging`.
- **Deployment takes too long**: The SQL deployment script (user creation) can take 2-3 minutes. This is normal.
- **`azd down` leaves orphans**: Check `az group list --tag azd-env-name=staging` to find any remaining resources.
- **Auto-migration fails on startup**: Check Application Insights logs for "Applying database migrations" messages. Common causes: SQL firewall rules, incorrect connection string, pending migration with breaking changes.
