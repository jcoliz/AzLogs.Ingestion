//
// Deploys an Azure Function reource
//    with attached storage
//    with configuration references to a data collection rule (DCR)
//    with Metrics Publisher permission to the DCR
//

@description('Unique suffix for all resources in this deployment')
param suffix string = uniqueString(resourceGroup().id)

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Name of required data collection endpoint resource')
param endpointName string

@description('Name of required data collection rule resource')
param dcrName string

@description('Name of required stream name')
param streamName string

resource endpoint 'Microsoft.Insights/dataCollectionEndpoints@2023-03-11' existing = {
  name: endpointName
}

resource dcr 'Microsoft.Insights/dataCollectionRules@2023-03-11' existing = {
  name: dcrName
}

module fn 'AzDeploy.Bicep/Web/fn-storage.bicep' = {
  name: 'fnstorage'
  params: {
    suffix: suffix
    location: location
    configuration: [
      {
        name: 'LogIngestion__DcrImmutableId'
        value: dcr.properties.immutableId
      }
      {
        name: 'LogIngestion__EndpointUri'
        value: endpoint.properties.metricsIngestion.endpoint
      }
      {
        name: 'LogIngestion__Stream'
        value: streamName
      }
      {
        name: 'WEATHER__OFFICE'
        value: 'SEW'
      }
      {
        name: 'WEATHER__GRIDX'
        value: '124'
      }
      {
        name: 'WEATHER__GRIDY'
        value: '69'
      }
    ]
  }
}

// Deploy Monitoring Metrics Publisher role on DCR for Function App

module publisherRole 'AzDeploy.Bicep/Insights/monitoring-metrics-publisher-role.bicep' = {
  name: 'publisherRole'
  params: {
    dcrName: dcrName
    principalId: fn.outputs.principal
    principalType: 'ServicePrincipal'
  }
}
