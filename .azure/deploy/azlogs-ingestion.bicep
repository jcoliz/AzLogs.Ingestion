//
// Deploys a complete set of needed resources for the sample at:
//    https://github.com/jcoliz/AzLogs.Ingestion
//

@description('Unique suffix for all resources in this deployment')
@minLength(5)
param suffix string = uniqueString(resourceGroup().id)

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Schema of table in log workspace')
param tableSchema object

@description('KQL query to transform input to putput ')
param transformKql string

@description('Columns of input schema')
param inputColumns array

@description('The principal that will be given data owner permission for the Data Collection Rule resource')
param principalId string

@description('The type of the given principal')
param principalType string = 'ServicePrincipal'

module logs 'AzDeploy.Bicep/OperationalInsights/loganalytics.bicep' = {
  name: 'logs'
  params: {
    suffix: suffix
    location: location
  }
}

module table 'AzDeploy.Bicep/OperationalInsights/workspace-table.bicep' = {
  name: 'table'
  params: {
    logAnalyticsName: logs.outputs.name
    tableSchema: tableSchema
  }
}

module dcep 'AzDeploy.Bicep/Insights/datacollectionendpoint.bicep' = {
  name: 'dcep'
  params: {
    suffix: suffix
    location: location
  }
}

module dcr 'AzDeploy.Bicep/Insights/datacollectionrule.bicep' = {
  name: 'dcr'
  params: {
    suffix: suffix
    location: location
    logAnalyticsName: logs.outputs.name
    tableName: tableSchema.name
    endpointName: dcep.outputs.name
    transformKql: transformKql
    inputColumns: inputColumns
  }
}

module publisherRole 'AzDeploy.Bicep/Insights/monitoring-metrics-publisher-role.bicep' = {
  name: 'publisherRole'
  params: {
    dcrName: dcr.outputs.name
    principalId: principalId
    principalType: principalType
  }
}

output DcrImmutableId string = dcr.outputs.DcrImmutableId
output EndpointUri string = dcep.outputs.EndpointUri
output Stream string = dcr.outputs.Stream
