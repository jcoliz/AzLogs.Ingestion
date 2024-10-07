# AzLogs.Ingestion Sample

[![Build](https://github.com/jcoliz/AzLogs.Ingestion/actions/workflows/build.yml/badge.svg)](https://github.com/jcoliz/AzLogs.Ingestion/actions/workflows/build.yml)

This is a fully-built sample using the [Logs Ingestion API in Azure Monitor](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/logs-ingestion-api-overview) on .NET 8.0.
The sample retrieves weather forecasts from the U.S. [National Weather Service API](https://www.weather.gov/documentation/services-web-api), then forwards them on to a [Logs Analytics Workspace](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/log-analytics-workspace-overview) using a [Data Collection Rule](https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/data-collection-rule-overview). It can be deployed as an [Azure Function](https://learn.microsoft.com/en-us/azure/azure-functions/functions-overview?pivots=programming-language-csharp) or run as a worker service on your local machine.

## Use case

Let's say we have some important data available in an external service, and we want some of that data in our Log Analytics Workspace. However, we don't control the external service, so we can't simply modify it to upload logs directly. What we can do instead is:

* **Extract** the data using a separate process, then send to a Data Collection Endpoint, which will
* **Transform** it into our desired schema using the Data Collection Rule, and
* **Load** it into a Log Analytics Workspace table.

## Architecture

<p align="center"><img src="https://raw.github.com/jcoliz/AzLogs.Ingestion/refs/heads/topic/azfn-2/docs/images/Architecture.png" alt="System Architecture"></p>

This is a very simple, focused sample. Our Azure Function application sits at the center of the system, doing all the work.
It periodically pulls data from an external source (here, weather.gov) then forwards it to a Log Analytics Workspace
using a Data Collection Endpoint and Data Collection Rule.

## Prerequisites

In order to follow the instructions shown here, and run this sample, you will first need:

* An Azure Account. Set up a [Free Azure Account](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account) to get started.
* [Azure CLI tool with Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/install#azure-cli)

Please read through the [Logs Ingestion API in Azure Monitor](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/logs-ingestion-api-overview) article carefully before proceeding.
This sample will first follow that article closely, before moving on to demonstrate deploying an Azure Function.

## Register a Microsoft Entra app

The very first step is to [Register an application with the Microsoft Identity Platform](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app?tabs=client-secret). This is helpful for running the sample locally. 
When running as an Azure Function, this app identity is not needed. Instead, the function app will use its [Managed Identity](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity) to connect with the DCR. Be sure to also [Add a client secret](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app?tabs=client-secret#add-credentials) as described on the page above. 

Alternately, you can follow these steps using the Azure CLI:

```dotnetcli
az ad app create --display-name azlogs-ingestion --key-type Password --sign-in-audience AzureADMyOrg
```

In the output of that command, look for the `AppId` field, which you'll need for the following step.

```json
"appId": "<client_id>",
```

Next, you'll need a client secret to connect:

```dotnetcli
az ad app credential reset --id <client_id>
```

This produces critical information you'll need to record and later configure the apps to connect.

```dotnetcli
{
  "appId": "<client_id>",
  "password": "<client_secret>",
  "tenant": "<tenant_id>"
}
```

After registering the application, either using the portal or CLI, you'll also need the Service Principal ID.
For more details, see [Application and service principal objects in Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/identity-platform/app-objects-and-service-principals?tabs=azure-cli). The fastest way to get this is using the Azure CLI, supplying the Client ID for your new application.

```dotnetcli
az ad sp list --filter "appId eq '<client_id>'"
```

This displays a full list of information about the Service Principal for your application.
The piece you're looking for is the `id` field.

When you're done, you'll have four key pieces of information

* Tenant ID
* Client ID
* Client Secret
* Service Principal ID

## Deploy Azure resources

This sample requires five Azure resources: Log Analytics Workspace, Data Collection Rule, and Data Collection Endpoint, Azure Function, and Blob Storage. There is an Azure Resource Manager (ARM) template to set up everything you need, ready to go: [azlogs-ingestion.bicep](./.azure/deploy/azlogs-ingestion-fn.bicep).
Did you clone this repo with submodules? If not, now is the time to init and update submodules so you have the [AzDeploy.Bicep](https://github.com/jcoliz/AzDeploy.Bicep) project handy with the
necessary module templates.

```powershell
git submodule update --init
```

From a PowerShell window in this folder, complete the following steps.

First, we'll set an environment variable to our chosen resource group name. Pick anything that helps you remember what the group is for:

```powershell
$env:RESOURCEGROUP = "azlogs-ingestion"
```

Next, we will create that group, in our chosen location. I'm a fan of [Moses Lake](https://www.datacenters.com/microsoft-azure-west-us-2-washington). You may feel differently.

```powershell
az group create --name $env:RESOURCEGROUP --location "West US 2"
```

Finally, the most important step, where we deploy our resources:

```powershell
az deployment group create --name "Deploy-$(Get-Random)" --resource-group $env:RESOURCEGROUP --template-file .azure\deploy\azlogs-ingestion.bicep --parameters .azure\deploy\azlogs-ingestion.parameters.json
```

You will be prompted to enter the Service Principal ID of the Entra App Registration you created earlier.

```dotnetcli
Please provide string value for 'principalId' (? for help): 
```

After the deployment completes, take note of the outputs from this deployment. You will use some of these values to configure the sample so it points to your newly-provisioned resources.
Look for the `outputs` section of the deployment. Please refer the configuration section below to find where to put them.

```json
"outputs": {
  "EndpointUri": {
    "type": "String",
    "value": "https://dcep-redacted.westus2-1.ingest.monitor.azure.com"
  },
  "DcrImmutableId": {
    "type": "String",
    "value": "dcr-redacted"
  },
  "Stream": {
    "type": "String",
    "value": "Custom-Forecasts_CL"
  },
  "StorageName": {
    "type": "String",
    "value": "storage000redacted"
  },
  "FunctionAppName": {
    "type": "String",
    "value": "fn-redacted"
  }
},
```

## Configuration

Once you have everyhing above running, you'll need to configure the sample with the App Registration you completed initially,
as well as the details on your Data Collection Rule. You could follow the practices outlined in
[Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

Alternately, you can create a `config.toml` file containing these secrets in the `BackgroundService` folder. This file will not be committed to source control.
To begin, copy the existing `config.template.toml` file to a new file named `config.toml`. Then fill this in with the values unique to your deployment.

```toml
[Identity]
TenantId = "<tenant_id>" # Directory (tenant) ID
AppId = "<client_id>" # Application (client) ID
AppSecret = "<client_secret>" # Client secret value

[LogIngestion]
EndpointUri = "<data_collection_endpoint_uri>" # Data collection endpoint, be sure to include https://
Stream = "<stream_name>" # The stream name to send to, usually `Custom-<table>_CL`
DcrImmutableId = "<data_collection_rule_id>" # The Immutable ID for this Data Collection Rule 
```

Optionally, you could elect to configure the options for connecting to the weather service.
Out of the box, the sample requests a weather forecast for the area surrounding the [Space Needle](https://www.spaceneedle.com/),
checking once every 5 seconds. You can find these values in `appsettings.json`.

```json
"Weather": {
  "Office": "SEW",
  "GridX": 124,
  "GridY": 69
},
"Worker": {
  "Frequency": "00:00:05"
}
```

The weather office, and grid x,y positions are specific to the NWS grid system. You can find values by calling the `/points/{lat,long}`
endpoint. The NWS has a handy Swagger UI on its API page, so you can try these out diretly.

Frequency is described in in Hours:Minutes:Seconds.

## Running Locally

Once you have all that set up, simply build and run the `BackgroundService` project!

```powershell
dotnet run --project BackgroundService

<6> [ 23/09/2024 12:04:42 ] AzLogs.Ingestion.Worker[1010] FetchForecastAsync: Received OK {"number":1,"name":"Today","startTime":"2024-09-23T11:00:00-07:00","endTime":"2024-09-23T18:00:00-07:00","isDaytime":true,"temperature":72,"temperatureUnit":"F","temperatureTrend":"","probabilityOfPrecipitation":{"value":null,"maxValue":0,"minValue":0,"unitCode":"wmoUnit:percent","qualityControl":"Z"},"dewpoint":null,"relativeHumidity":null,"windSpeed":"6 mph","windGust":null,"windDirection":"SSW","icon":"https://api.weather.gov/icons/land/day/sct?size=medium","shortForecast":"Mostly Sunny","detailedForecast":"Mostly sunny, with a high near 72. South southwest wind around 6 mph."}
<6> [ 23/09/2024 12:04:42 ] AzLogs.Ingestion.Worker[1020] UploadToLogsAsync: Sent OK 204
```

Note that the underlying services all log quite a bit of information to the application logger as well. If you want to see that in action, simply increase the default level in `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
```

## Verify data flow

After observing that logs are sent successfully to Log Analytics from the client application logs, it's time to turn our attention
to verifying the data has landed correctly in the service. Using the Azure Portal, navigate to the resource group you created above, e.g. `azlogs-ingestion`, then click into the Data Change Rule resource. In the navigation panel on the left, expand "Monitoring", then choose "Metrics". Click "Add Metric", then choose "Logs Ingestion Requests per Min". You should see a chart like the one below:

![Data Change Rule Metrics](./docs/images/dcr-metrics.png)

Next, we can look at the Log Analytics workspace itself to confirm that the logs have landed in their final destination. Again, navigate to the resource group page, but this time click into the Log Analytics resource. Click "Logs", and then enter this query:

```kql
Forecasts_CL
| summarize Count = count() by bin(TimeGenerated, 1min)
| render timechart 
```

If all is well, you will see a chart like the one below:

![Log Analytics Workspace Query](./docs/images/logs-query.png)

Congratulations, you have successfully ingested logs into a Log Analytics Workspace custom table using a Data Collection Rule! Now we can move on to running as a function app.

## Function App Locally

We will now run the same code as above, built as an Azure Function. For this section, you'll need to have a
Terminal Window open, in the `FunctionApp` folder.

### Install Tools

You'll need to [Install the Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-csharp#install-the-azure-functions-core-tools) to follow the remaining steps in this article.

### Set up local configuration

The Azure Function Core tools use a file named `local.settings.json` to store local configuration, including secrets. Much like the `config.toml` in the previous steps, this
file is not committed to source control. Copy the `local.settings.template.json` file to a new file named `local.settings.json`, and fill out the details. The information needed is the same as previously set in `config.toml`.

The one additional piece of information you'll need in a connection string to the Azure Blob Storage where configuration is stored for the function information. You can retrieve this using the Azure CLI, using the name of the storage resource. This was displayed after you deployed resources above as the `StorageName` output.

```dotnetcli
az storage account show-connection-string --name <StorageName>
{
  "connectionString": "<storage_connection_string>"
}
```

Add the strong shown here to the `AzureWebJobsStorage` field in `local.settings.json`.

### Run locally

We'll first build the app, then run it using the tools:

```dotnetcli
dotnet build
func host start
```

We can watch the function running locally. To further confirm, we can go back to look at the the Azure Monitor metrics for the DCR.

```
TODO: Logs
```

### Deploy and run remotely

Now that you can see it all running locally, it's time to deploy! You'll need the name of the function app which you deployed earlier. This was included in the outputs of the deployment, as the `FunctionAppName` output.

```dotnetcli
func azure functionapp publish <FunctionAppName>
```

Once it's complete, you can connect with the logstream and watch it at work

```dotnetcli
func azure functionapp logstream <FunctionAppName>
```

Unfortunately, at the current moment, the application fails to upload the data to the DCE, giving this result:

```json
{"Error":{"Code":"InvalidApiVersion","Message":"The api version is invalid. Allowed Api versions: 2021-11-01-preview,2023-04-24."}}
```

Track the details here: [[BUG] LogsIngestionClient.UploadAsync fails when running in Azure Function](https://github.com/Azure/azure-sdk-for-net/issues/46439)

## Tear down

When you're done, don't forget to tear down the resource group to avoid unexpected charges.

```powershell
az group delete --yes --name $env:RESOURCEGROUP
```
