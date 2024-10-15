param(
    [Parameter(Mandatory=$true)]
    [string]
    $Name
)

Write-Output "Creating App registration $Name"

$app = az ad app create --display-name $Name --key-type Password --sign-in-audience AzureADMyOrg | ConvertFrom-Json
$appId = $app.appId

Write-Output "OK. Created $appId"

Write-Output "Resetting app secret for $Name"

$creds = az ad app credential reset --id <client_id> | ConvertFrom-json

Write-Output "OK."

Write-Output "Copy these values to config.toml:"
Write-Output ""

$password = $creds.password
$tenant = $creds.tenant

Write-Output "[Identity]"
Write-Output "TenantId = ""$tenant"""
Write-Output "AppId = ""$appId"""
Write-Output "AppSecret = ""$password"""
