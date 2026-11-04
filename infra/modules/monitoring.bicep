// Log Analytics workspace + workspace-based Application Insights.
// Local (instrumentation key) auth is disabled: the Function App sends telemetry with its managed identity
// (APPLICATIONINSIGHTS_AUTHENTICATION_STRING=Authorization=AAD + Monitoring Metrics Publisher).
param location string
param tags object
param logAnalyticsName string
param appInsightsName string
param dailyQuotaGb int

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: dailyQuotaGb
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    DisableLocalAuth: true
  }
}

output appInsightsName string = appInsights.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output logAnalyticsId string = logAnalytics.id
output logAnalyticsName string = logAnalytics.name
