# AzLogs.Ingestion Sample

[![Build](https://github.com/jcoliz/AzLogs.Ingestion/actions/workflows/build.yml/badge.svg)](https://github.com/jcoliz/AzLogs.Ingestion/actions/workflows/build.yml)

This is a fully-built sample using the [Logs Ingestion API in Azure Monitor](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/logs-ingestion-api-overview) on .NET 8.0. 
The sample retrieves weather forecasts from the U.S. [National Weather Service API](https://www.weather.gov/documentation/services-web-api), then forwards them on to a [Logs Analytics Workspace](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/log-analytics-workspace-overview) using a [Data Collection Rule](https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/data-collection-rule-overview). It runs as a worker service on your local machine.

## Prerequisites

* An Azure Account
* Azure CLI tool with Bicep extension
* App Registration in Entra ID
* Log Analytics workspace resource
* Custom Log Analytics table with a corresponding Data Collection Rule

Please read through the [Logs Ingestion API in Azure Monitor](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/logs-ingestion-api-overview) article carefully before proceeding.
This sample will follow that article closely.

## Register a Microsoft Entra app

The very first step is to [Register a Microsoft Entra app and create a service principal](https://learn.microsoft.com/en-us/entra/identity-platform/howto-create-service-principal-portal#register-an-application-with-azure-ad-and-create-a-service-principal) following the steps in that article. 
When you're done, you'll need four key pieces of information

* Tenant ID
* Client ID
* Client Secret
* Service Principal ID

## Deploy Azure Resources

There is quite a bit of setup needed on Azure resources. If you're setting them up in the portal, the easiest way is create a new Log Analytics resource, then create a custom table.
That flow will lead you down the path of creating a Data Collection Rule with a Data Collection Endpoint.

However, for this sample, there's an even easier way. This sample contains an Azure Resource Manager (ARM) template to set up everything you need, ready to go. 
Did you clone this repo with submodules? If not, now is the time to update submodules so you have the [AzDeploy.Bicep](https://github.com/jcoliz/AzDeploy.Bicep) project handy with the
necessary baseline templates.

```powershell
git submodules --update
```

From a terminal window in the `.azure\deploy` folder, complete the following steps.

First, we'll set an environment variable to our chosen resource group name. Pick anything that helps you remember what the group is for:

```powershell
$env:RESOURCEGROUP = "azlogs-ingestion"
```

Next, we will create that group, in our chosen location. I'm a fan of Moses Lake. You may feel differently.

```powershell
az group create --name $env:RESOURCEGROUP --location "West US 2"
```

Finally, the most important step, where we deploy our resources:

```powershell
az deployment group create --name "Deploy-$(Get-Random)" --resource-group $env:RESOURCEGROUP --template-file .\AzDeploy.Bicep\Insights\logs-with-dcr.bicep --parameters .\logs-with-dcr.parameters.json
```

Later on, when you're done, don't forget to tear down the resource group to avoid unexpected charges.

```powershell
az group delete --yes --name $env:RESOURCEGROUP
```

## Configuration

Once you have everyhing above running, you'll need to configure the sample with the App Registration you completed initially,
as well as the details on your Data Collection Rule. You could follow the practices outlined in
[Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

Alternately, you can create a `config.toml` file containing these secrets in this directory. This file will not be checked into source control.
To begin, copy the existing `config.template.toml` file to a new file named `config.toml`. Then fill this in with the values unique to your deployment.

```toml
[Identity]
TenantId = "<tenant_id>" # Directory (tenant) ID
AppId = "<client_id>" # Application (client) ID
AppSecret = "<client_secret>" # Client secret value

[LogIngestion]
Endpoint = "https://<data_collection_endpoint_uri>/" # Data collection endpoint, be sure to include https://
Stream = "<stream_name>" # The sream name to send to, usually `Custom-<table>_CL`
DcrImmutableId = "<data_collection_rule_id>" # The Immutable ID for this data collection rule 
```

Additionally, you can configure the options for connecting to the weather service. 
Out of the box, the sample requests a weather forecast for the area surrounding the [Space Needle](https://www.spaceneedle.com/),
checking once every 5 seconds. You can find these values in `appsettings.json`.

```json
  "Weather": {
    "Office": "SEW",
    "GridX": 124,
    "GridY": 69,
    "Frequency": "00:00:05"
  }
```

The weather office, and grid x,y positions are specific to the NWS grid system. You can find values by calling the `/points/{lat,long}`
endpoint. The NWS has a handy Swagger UI on its API page, so you can try these out diretly.

Frequency is described in in Hours:Minutes:Seconds.

## Running

Once you have all that set up, simply build and run the sample!

```powershell
dotnet run

<6> [ 23/09/2024 12:04:42 ] Weather.Worker.Worker[1010] FetchForecastAsync: Received OK {"number":1,"name":"Today","startTime":"2024-09-23T11:00:00-07:00","endTime":"2024-09-23T18:00:00-07:00","isDaytime":true,"temperature":72,"temperatureUnit":"F","temperatureTrend":"","probabilityOfPrecipitation":{"value":null,"maxValue":0,"minValue":0,"unitCode":"wmoUnit:percent","qualityControl":"Z"},"dewpoint":null,"relativeHumidity":null,"windSpeed":"6 mph","windGust":null,"windDirection":"SSW","icon":"https://api.weather.gov/icons/land/day/sct?size=medium","shortForecast":"Mostly Sunny","detailedForecast":"Mostly sunny, with a high near 72. South southwest wind around 6 mph."}
<6> [ 23/09/2024 12:04:42 ] Weather.Worker.Worker[1020] UploadToLogsAsync: Sent OK 204
```

Note that the underlying services all log quite a bit of information to the application logger as well. If you want to see that in action, simply increase the default level in `appsettings.json`:

```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
```

## Verify data flow

TODO!