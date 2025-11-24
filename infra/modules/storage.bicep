targetScope = 'resourceGroup'

@description('Location for the storage account')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param env string = 'dev'

@description('Application name prefix')
param appName string = 'receiptfly'

// Storage account name must be between 3 and 24 characters in length and use numbers and lower-case letters only.
var storageAccountName = 'streceiptfly${env}001'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
  resource blobService 'blobServices' = {
    name: 'default'
    resource receiptImagesContainer 'containers' = {
      name: 'receipt-images'
      properties: {
        publicAccess: 'None'
      }
    }
  }
  resource tableService 'tableServices' = {
    name: 'default'
    resource receiptsTable 'tables' = {
      name: 'Receipts'
    }
  }
}

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id

