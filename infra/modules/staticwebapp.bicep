targetScope = 'resourceGroup'

@description('Location for the Static Web App')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param env string = 'dev'

@description('Application name prefix')
param appName string = 'receiptfly'

// Static Web App名を固定形式に変更
var envNumber = env == 'dev' ? '001' : env == 'test' ? '002' : '003'
var staticWebAppName = 'swa-${toLower(appName)}-${env}-${envNumber}'

resource staticWebApp 'Microsoft.Web/staticSites@2022-03-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    // GitHub連携は後で設定可能
    // repositoryUrl: ''
    // branch: 'main'
    // buildProperties: {
    //   appLocation: '/'
    //   apiLocation: 'api'
    //   outputLocation: 'dist'
    // }
  }
}

output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname

