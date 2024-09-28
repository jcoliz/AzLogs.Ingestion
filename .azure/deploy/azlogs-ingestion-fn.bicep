//
// Deploys a complete set of needed resources for the sample at:
//    https://github.com/jcoliz/AzLogs.Ingestion
//
// Includes:
//    * Log Analytics Workspace (LAW) with custom table
//    * Data Collection Endpoint (DCE)
//    * Data Collection Rule (DCR) with connection to DCE and LAW
//    * Function App to run the extraction
//    * Monitoring Metrics Publisher role on DCR for Function App
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

// Deploy Log Anaytics Workspace

module logs 'AzDeploy.Bicep/OperationalInsights/loganalytics.bicep' = {
  name: 'logs'
  params: {
    suffix: suffix
    location: location
  }
}

// Deploy custom table on that workspace

module table 'AzDeploy.Bicep/OperationalInsights/workspace-table.bicep' = {
  name: 'table'
  params: {
    logAnalyticsName: logs.outputs.name
    tableSchema: tableSchema
  }
}

// Deploy Data Collection Endpoint

module dcep 'AzDeploy.Bicep/Insights/datacollectionendpoint.bicep' = {
  name: 'dcep'
  params: {
    suffix: suffix
    location: location
  }
}

// Deploy Data Collection Rule (DCR) with connection to DCE and LAW custom table

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

// Deploy Azure function app with storage, including connection to DCR & DCE

module fnconfig './fn-config.bicep' = {
  name: 'fnconfig'
  params: {
    suffix: suffix
    location: location
    endpointName: dcep.outputs.name
    dcrName: dcr.outputs.name
    streamName: 'Custom-${tableSchema.name}'
  }
}

// Deploy Monitoring Metrics Publisher role on DCR for Function App

module publisherRole 'AzDeploy.Bicep/Insights/monitoring-metrics-publisher-role.bicep' = {
  name: 'publisherRole'
  params: {
    dcrName: dcr.outputs.name
    principalId: fnconfig.outputs.principal
    principalType: 'ServicePrincipal'
  }
}

// Return necessary outputs to user. Put these in `local.settings.json

// "LogIngestion__DcrImmutableId": "<below value>"
output DcrImmutableId string = dcr.outputs.DcrImmutableId

// "LogIngestion__EndpointUri": "<below value>"
output EndpointUri string = dcep.outputs.EndpointUri

// "LogIngestion__Stream": "<below value>"
output Stream string = dcr.outputs.Stream
