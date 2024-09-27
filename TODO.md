# Azure Functions TODO

This branch, `topic/azfn` is an experimental forward-only branch to convert this sample into an Azure Function.

## References

* [AzFn.StarterKit](https://github.com/jcoliz/AzFn.StarterKit) repo
* [fn-storage.bicep](https://github.com/jcoliz/AzDeploy.Bicep/blob/main/Web/fn-storage.bicep) ARM template used in above repo
* [Quickstart: Create and deploy Azure Functions resources from an ARM template](https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-resource-manager?tabs=azure-cli)
* [Continuous delivery by using GitHub Actions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-github-actions?tabs=windows%2Cdotnet&pivots=method-manual)
* [GitHub Actions for deploying to Azure Functions](https://github.com/Azure/functions-action)
* [GitHub Actions: Deploy DotNet project to Linux Azure Function App](https://github.com/Azure/actions-workflow-samples/blob/master/FunctionApp/linux-dotnet-functionapp-on-azure.yml)
* [Using Azure Service Principal for RBAC as Deployment Credential](https://github.com/Azure/functions-action?tab=readme-ov-file#using-azure-service-principal-for-rbac-as-deployment-credential)
* [Guide for running C# Azure Functions in the isolated worker model](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows)
* [Timer trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=python-v2%2Cisolated-process%2Cnodejs-v4&pivots=programming-language-csharp)
* [Code and test Azure Functions locally](https://learn.microsoft.com/en-us/azure/azure-functions/functions-develop-local)

## Steps

1. Deploy Azure Function resource
1. Build basic azure function that runs on timer and outpus logs. Make sure we can read those logs and deploy.
1. Deploy via github workflow
1. Build a complete ARM template

## Implementation Notes

```dotnetcli
$env:RESOURCEGROUP = "azlogs-ingestion"
az group create --name $env:RESOURCEGROUP --location "West US 2"
az deployment group create --name "Deploy-$(Get-Random)" --resource-group $env:RESOURCEGROUP --template-file .\.azure\deploy\AzDeploy.Bicep\Web\fn-storage.bicep
```

## TODOs for ARM template

* Should send in LAW/DCR config via app settings
* Use the service principal created for the fn app to assign metrics publisher
