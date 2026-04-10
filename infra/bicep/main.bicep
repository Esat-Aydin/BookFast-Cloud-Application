targetScope = 'resourceGroup'

@description('Short application identifier used in Azure resource names.')
@minLength(3)
@maxLength(12)
param appName string = 'bookfast'

@description('Deployment environment name.')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string = 'dev'

@description('Primary Azure region for the resource group deployment.')
param location string = resourceGroup().location

@description('Tags applied consistently across all platform resources.')
param tags object = {
  application: appName
  environment: environmentName
  workload: 'integration-platform'
}

var uniqueToken = toLower(uniqueString(subscription().subscriptionId, resourceGroup().id, environmentName))
var platformPrefix = '${appName}-${environmentName}'
var keyVaultName = take(replace(toLower('${appName}${environmentName}${uniqueToken}'), '-', ''), 24)
var plannedModules = [
  'modules/platform'
  'modules/api'
  'modules/functions'
  'modules/messaging'
  'modules/security'
  'modules/observability'
]
var plannedResourceNames = {
  applicationInsights: '${platformPrefix}-appi'
  apiManagement: '${platformPrefix}-apim'
  apiWebApp: '${platformPrefix}-api'
  functionApp: '${platformPrefix}-func'
  keyVault: keyVaultName
  logAnalyticsWorkspace: '${platformPrefix}-log'
  serviceBusNamespace: '${platformPrefix}-sb'
  sqlServer: '${platformPrefix}-sql'
}

// Phase 0 intentionally establishes naming, parameters, and module boundaries first.
// Concrete resource modules will replace these planning outputs in later phases.
output metadata object = {
  environment: environmentName
  location: location
  plannedModules: plannedModules
  plannedResourceNames: plannedResourceNames
  tags: tags
}
