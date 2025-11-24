targetScope = 'resourceGroup'

@description('Location for the function apps')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param env string = 'dev'

@description('Application name prefix')
param appName string = 'receiptfly'

@description('Storage account name')
param storageAccountName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault name')
param keyVaultName string

@description('Static Web App default hostname for CORS configuration')
param staticWebAppHostname string = ''

var apiFunctionAppName = 'func-${toLower(appName)}-api-${env}-001'
var processingFunctionAppName = 'func-${toLower(appName)}-processing-${env}-001'

// CORS設定用のallowedOriginsを構築
// Static Web AppのURLとワイルドカードパターンを含める
var corsAllowedOrigins = staticWebAppHostname != '' 
  ? [
      'https://${staticWebAppHostname}'
      'https://*.azurestaticapps.net'
    ]
  : [
      'https://*.azurestaticapps.net'
    ]
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource apiFunctionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: apiFunctionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'AzureStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'AzureStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'GoogleCloud:ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=GoogleCloudApiKey)'
        }
        {
          name: 'Gemini:ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=GeminiApiKey)'
        }
        {
          name: 'UseAzure'
          value: 'true'
        }
      ]
      minTlsVersion: '1.2'
    }
  }
}

resource apiFunctionAppConfig 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: apiFunctionApp
  name: 'web'
  properties: {
    cors: {
      allowedOrigins: corsAllowedOrigins
      supportCredentials: false
    }
  }
}

resource processingFunctionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: processingFunctionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'AzureStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'AzureStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'GoogleCloud:ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=GoogleCloudApiKey)'
        }
        {
          name: 'Gemini:ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=GeminiApiKey)'
        }
        {
          name: 'UseAzure'
          value: 'true'
        }
      ]
      minTlsVersion: '1.2'
    }
  }
}

resource processingFunctionAppConfig 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: processingFunctionApp
  name: 'web'
  properties: {
    cors: {
      allowedOrigins: corsAllowedOrigins
      supportCredentials: false
    }
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyVaultName
}

var apiFunctionPrincipalId = apiFunctionApp.identity.principalId
var processingFunctionPrincipalId = processingFunctionApp.identity.principalId

resource apiFunctionStorageBlobRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, apiFunctionApp.name, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: apiFunctionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource apiFunctionStorageTableRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, apiFunctionApp.name, storageTableDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
    principalId: apiFunctionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource processingFunctionStorageBlobRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, processingFunctionApp.name, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: processingFunctionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource processingFunctionStorageTableRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, processingFunctionApp.name, storageTableDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
    principalId: processingFunctionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource apiFunctionKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiFunctionApp.name, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: apiFunctionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource processingFunctionKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, processingFunctionApp.name, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: processingFunctionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output apiFunctionAppName string = apiFunctionApp.name
output processingFunctionAppName string = processingFunctionApp.name
output apiFunctionPrincipalId string = apiFunctionApp.identity.principalId
output processingFunctionPrincipalId string = processingFunctionApp.identity.principalId

