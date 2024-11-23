param(
    [Parameter(Mandatory=$true)]
    [string]
    $Name
)

Write-Output "Creating App registration $Name"

$app = az ad app create --display-name $Name --key-type Password --sign-in-audience AzureADMyOrg | ConvertFrom-Json
$appId = $app.appId

Write-Output "OK. Created $appId"
Write-Output ""

Write-Output "Resetting app secret for $Name"

$creds = az ad app credential reset --id $appId | ConvertFrom-json

Write-Output "OK."

Write-Output "Copy these values to local.settings.json:"
Write-Output ""

$password = $creds.password
$tenant = $creds.tenant

Write-Output """AZURE_TENANT_ID"": ""$tenant"","
Write-Output """AZURE_CLIENT_ID"": ""$appId"","
Write-Output """AZURE_CLIENT_SECRET"": ""$password"","

Write-Output "Creating Service Principal for $Name"

$sp = az ad sp create --id $appId | ConvertFrom-json

$spId = $sp.id

Write-Output "OK."

Write-Output "Include this Service Principal when deploying Azure resources:"
Write-Output ""

Write-Output "-ServicePrincipal $spId"
