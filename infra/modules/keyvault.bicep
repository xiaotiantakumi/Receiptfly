targetScope = 'resourceGroup'

@description('Location for the Key Vault')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param env string = 'dev'

@description('Application name prefix')
param appName string = 'receiptfly'

@description('Principal ID of the deployment user (for Key Vault Administrator role)')
param deployUserPrincipalId string = ''

// Key Vault名は3-24文字、英数字とハイフン、文字で始まり文字または数字で終わる
// 命名規則: kv-receiptfly-dev-001
// kv(2) + -(1) + receiptfly(10) + -(1) + dev(3) + -(1) + 001(3) = 21文字
var envNumber = env == 'dev' ? '001' : env == 'test' ? '002' : '003'
var keyVaultName = 'kv-${toLower(appName)}-${env}-${envNumber}'
var keyVaultAdministratorRoleId = '00482a5a-887f-4fb3-b363-3b7fe8e74483'

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource deployUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployUserPrincipalId != '') {
  name: guid(keyVault.id, deployUserPrincipalId, keyVaultAdministratorRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultAdministratorRoleId)
    principalId: deployUserPrincipalId
    principalType: 'User'
  }
}

output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri

