targetScope = 'resourceGroup'

@description('Location for the monitoring resources')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param env string = 'dev'

@description('Application name prefix')
param appName string = 'receiptfly'

// 既存リソース名を使用（冪等性を保証するため）
// 既存のリソース名: log-receiptfly-dev-il4ap7yckrbem, appi-receiptfly-dev-il4ap7yckrbem
// 新規環境では固定形式を使用: log-receiptfly-{env}-001, appi-receiptfly-{env}-001
var envNumber = env == 'dev' ? '001' : env == 'test' ? '002' : '003'
// dev環境では既存リソース名を使用、それ以外は固定形式
var logWorkspaceName = env == 'dev' ? 'log-receiptfly-dev-il4ap7yckrbem' : 'log-${toLower(appName)}-${env}-${envNumber}'
var appInsightsName = env == 'dev' ? 'appi-receiptfly-dev-il4ap7yckrbem' : 'appi-${toLower(appName)}-${env}-${envNumber}'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018' // Pay-as-you-go (無料枠5GB/月)
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

output appInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output appInsightsConnectionString string = applicationInsights.properties.ConnectionString

