# Azure Deployment Guide

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (v2.50+)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) (v1.5+)
- An Azure subscription (Visual Studio Professional subscription with ~$50/month credit is sufficient)
- .NET 10 SDK (for local builds before deploy)

## Provisioned Resources

| Resource | SKU (default) | Approx Monthly Cost |
|----------|---------------|---------------------|
| App Service Plan | B1 (1 core, 1.75 GB) | ~$13 |
| Azure SQL Database | Basic (5 DTU, 2 GB) | ~$5 |
| Blob Storage | Standard_LRS | ~$0.02/GB |
| Key Vault | Standard | ~$0.03/10K ops |
| Log Analytics + App Insights | PerGB2018 | Free first 5 GB/month |
| **Total** | | **~$18-20/month** |

## Provision Infrastructure

```bash
# Navigate to the api directory (contains azure.yaml)
cd api

# Authenticate with Azure
azd auth login

# Create a new environment
azd env new <environment-name>
# Example: azd env new recruitment-dev

# Set the Azure location
azd env set AZURE_LOCATION westeurope

# Provision all infrastructure
azd provision
```

`azd provision` will prompt for:
- `dbAdminPassword` — SQL Server admin password
- `dbAppUserPassword` — SQL Server application user password

Provisioning typically completes within 15-20 minutes.

## Deploy the Application

```bash
cd api
azd deploy
```

This builds the .NET application and deploys it to the provisioned App Service.

## Verify Deployment

After deployment, verify the health endpoints:

```bash
# Get the App Service URL
azd env get-values | grep WEB_BASE_URI

# Check liveness
curl https://<app-url>/health

# Check readiness (database connectivity)
curl https://<app-url>/ready
```

Both endpoints should return HTTP 200 with `Healthy` status.

## Override Default SKUs

Default SKUs are cost-optimized for a test/dev subscription. Override them for different environments:

```bash
# Use a more powerful App Service tier
azd env set appServiceSkuName S1

# Use a larger SQL Database tier
azd env set sqlDatabaseSku '{"name":"S0","tier":"Standard","capacity":10}'

# Use geo-redundant storage
azd env set storageSku Standard_GRS

# Apply changes
azd provision
```

## Teardown

To remove all provisioned resources and stop incurring charges:

```bash
cd api
azd down
```

This deletes:
- The resource group and all contained resources
- App Service, SQL Server, Storage Account, Key Vault, Log Analytics, Application Insights

Confirm when prompted. The teardown typically completes within 5-10 minutes.

To also remove the local azd environment configuration:

```bash
azd env delete <environment-name>
```

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
