param(
    [Parameter(Mandatory=$true)]
    [string]
    $ResourceGroup,
    [Parameter(Mandatory=$true)]
    [string]
    $Location,
    [Parameter(Mandatory=$true)]
    [GUID]
    $ServicePrincipal
)

Write-Output "Creating Resource Group $ResourceGroup in $Location"
az group create --name $ResourceGroup --location $Location

Write-Output "Deploying to Resource Group $ResourceGroup"
$result = az deployment group create --name "Deploy-$(Get-Random)" --resource-group $ResourceGroup --template-file .azure\deploy\azlogs-ingestion.bicep --parameters .azure\deploy\azlogs-ingestion.parameters.json --parameters principalId=$ServicePrincipal | ConvertFrom-Json

Write-Output "OK"
Write-Output ""

$storageName = $result.properties.outputs.StorageName.value

Write-Output "Getting connnection string for $storageName"
$cstrResult = az storage account show-connection-string --name $storageName

Write-Output "OK"
Write-Output ""

$functionAppName = $result.properties.outputs.functionAppName.value

Write-Output "Your function app is named: $functionAppName"
Write-Output ""

Write-Output "Copy these values to local.settings.json:"
Write-Output ""

$dcrImmutableId = $result.properties.outputs.dcrImmutableId.value
$endpointUri = $result.properties.outputs.endpointUri.value
$stream = $result.properties.outputs.stream.value
$cstr = $cstrResult.connectionString

Write-Output """LogIngestion__DcrImmutableId"": ""$dcrImmutableId"","
Write-Output """LogIngestion__EndpointUri"": ""$endpointUri"","
Write-Output """LogIngestion__Stream"": ""$stream"","
Write-Output """AzureWebJobsStorage"":  ""$cstr"","
