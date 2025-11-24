targetScope = 'resourceGroup'

@description('Application name prefix')
param appName string = 'receiptfly'

@description('Environment name (dev, test, prod)')
param env string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Principal ID of the deployment user (for Key Vault Administrator role)')
param deployUserPrincipalId string = ''


module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    env: env
    appName: appName
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    env: env
    appName: appName
  }
}

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    env: env
    appName: appName
    deployUserPrincipalId: deployUserPrincipalId
  }
}

module staticwebapp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp'
  params: {
    location: 'eastasia' // Changed to a supported region for Static Web Apps
    env: env
    appName: appName
  }
}

module functions 'modules/functions.bicep' = {
  name: 'functions'
  params: {
    location: location
    env: env
    appName: appName
    storageAccountName: storage.outputs.storageAccountName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultName: keyvault.outputs.keyVaultName
    staticWebAppHostname: staticwebapp.outputs.staticWebAppDefaultHostname
  }
}


output storageAccountName string = storage.outputs.storageAccountName
output keyVaultName string = keyvault.outputs.keyVaultName
output apiFunctionAppName string = functions.outputs.apiFunctionAppName
output processingFunctionAppName string = functions.outputs.processingFunctionAppName
output staticWebAppName string = staticwebapp.outputs.staticWebAppName
output staticWebAppDefaultHostname string = staticwebapp.outputs.staticWebAppDefaultHostname

