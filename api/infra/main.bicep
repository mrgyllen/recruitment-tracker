targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string

// Optional parameters to override the default azd resource naming conventions.
// Add the following to main.parameters.json to provide values:
// "resourceGroupName": {
//      "value": "myGroupName"
// }
param resourceGroupName string = ''
param logAnalyticsName string = ''
param applicationInsightsName string = ''
param applicationInsightsDashboardName string = ''
param keyVaultName string = ''
param appServiceName string = ''
param dbServerName string = ''
param dbName string = ''

@secure()
param dbAdminPassword string

@secure()
param dbAppUserPassword string

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

var abbrs = loadJsonContent('./abbreviations.json')

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
}

// Generate a unique token to be used in naming resources.
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Name of the service defined in azure.yaml
// A tag named azd-service-name with this value should be applied to the service host resource, such as:
//   Microsoft.Web/sites for appservice, function
// Example usage:
//   tags: union(tags, { 'azd-service-name': apiServiceName })
var webServiceName = 'web'

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// Add resources to be provisioned below.

module monitoring 'core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: !empty(applicationInsightsDashboardName) ? applicationInsightsDashboardName : '${abbrs.portalDashboards}${resourceToken}'
  }
  scope: rg
}

module keyVault 'core/security/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    tags: tags
    name: !empty(keyVaultName) ? keyVaultName : '${abbrs.keyVaultVaults}${resourceToken}'
    principalId: principalId
  }
  scope: rg
}

module web 'services/web.bicep' = {
  name: 'web'
  params: {
    name: !empty(appServiceName) ? appServiceName : '${abbrs.webSitesAppService}${resourceToken}'
    location: location
    tags: tags
    serviceName: webServiceName
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    keyVaultName: keyVault.outputs.name
    skuName: appServiceSkuName
  }
  scope: rg
}


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
    databaseSku: sqlDatabaseSku
  }
  scope: rg
}

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

module storageSecret 'app/storage-secret.bicep' = {
  name: 'storageSecret'
  params: {
    storageAccountName: storage.outputs.name
    keyVaultName: keyVault.outputs.name
    secretName: 'ConnectionStrings--BlobStorage'
  }
  scope: rg
}

module webKeyVaultAccess 'core/security/keyvault-access.bicep' = {
  name: 'webKeyVaultAccess'
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: web.outputs.identityPrincipalId
  }
  scope: rg
}

// Add outputs from the deployment here, if needed.
//
// This allows the outputs to be referenced by other bicep deployments in the deployment pipeline,
// or by the local machine as a way to reference created resources in Azure for local development.
// Secrets should not be added here.
//
// Outputs are automatically saved in the local azd environment .env file.
// To see these outputs, run `azd env get-values`,  or `azd env get-values --output json` for json output.
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output AZURE_SQL_CONNECTION_STRING_KEY string = database.outputs.connectionStringKey
output AZURE_STORAGE_BLOB_ENDPOINT string = storage.outputs.primaryEndpoints.blob
output AZURE_STORAGE_CONNECTION_STRING_KEY string = storageSecret.outputs.secretName
output WEB_BASE_URI string = web.outputs.uri
