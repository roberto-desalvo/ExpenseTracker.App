<#
.SYNOPSIS
    One-time consent for the automatic OneDrive import (functions OneDrive_*).

.DESCRIPTION
    The watched folder is on a personal OneDrive (Microsoft account): Microsoft Graph does not allow app-only
    access there, so the Function App acts on behalf of the OneDrive owner with a delegated refresh token.

    1. Device code flow (sign in with the Microsoft account that owns the OneDrive) on the public client
       "ExpenseTracker OneDrive import" created by infra/modules/entra.bicep: scopes Files.ReadWrite + offline_access.
    2. Writes the refresh token (secret OneDriveRefreshToken) and, if missing, a random clientState for the
       Graph notifications (secret OneDriveClientState) to the Key Vault.
    3. Restarts the Function App (reloads the Key Vault reference of the clientState) and runs
       OneDrive_SubscriptionRenewal, which creates the delta baseline and the Graph subscription.

    Run it once after the first deploy with ONEDRIVE_IMPORT_ENABLED=true, and again only if the refresh token
    expires (90 days without use) or is revoked. The Function App rotates the token by itself afterwards.

    Requires Azure CLI logged in with: Key Vault Secrets Officer on the vault, and permission to restart the
    Function App / read its master key (Contributor on the resource group).
    Values not passed as parameters are read from the outputs of the last successful infra deployment.

.EXAMPLE
    . ./infra/scripts/load-env.ps1
    ./infra/scripts/connect-onedrive.ps1

.EXAMPLE
    ./infra/scripts/connect-onedrive.ps1 -ResourceGroup rg-expensetracker -SkipSubscription
#>
[CmdletBinding()]
param(
    [string] $ResourceGroup = $env:AZURE_RESOURCE_GROUP,

    # Defaults: outputs of the last successful deployment of infra/main.bicep in the resource group.
    [string] $ClientId,
    [string] $KeyVaultName,
    [string] $FunctionAppName,

    [string] $RefreshTokenSecretName = 'OneDriveRefreshToken',
    [string] $ClientStateSecretName = 'OneDriveClientState',

    # Only store the token: do not restart the Function App nor create the subscription.
    [switch] $SkipSubscription
)

$ErrorActionPreference = 'Stop'

function Invoke-Native {
    param([Parameter(Mandatory)] [string] $Description, [Parameter(Mandatory)] [scriptblock] $Command)
    $ErrorActionPreference = 'Continue'
    $output = & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Description failed (exit code $LASTEXITCODE)." }
    return $output
}

if (-not $ResourceGroup) { throw 'Pass -ResourceGroup or set AZURE_RESOURCE_GROUP (infra/.env + load-env.ps1).' }

# --- 1. Values from the last infra deployment -----------------------------------------------------------
if (-not ($ClientId -and $KeyVaultName -and $FunctionAppName)) {
    $query = "sort_by([?properties.provisioningState=='Succeeded' && properties.outputs.oneDriveClientId != null], &properties.timestamp)[-1].properties.outputs"
    $outputsJson = Invoke-Native 'Reading deployment outputs' {
        az deployment group list -g $ResourceGroup --query $query -o json
    }
    $outputs = ($outputsJson -join "`n") | ConvertFrom-Json
    if (-not $outputs) { throw "No successful infra deployment with the oneDriveClientId output in '$ResourceGroup': deploy infra/main.bicep first." }

    if (-not $ClientId) { $ClientId = $outputs.oneDriveClientId.value }
    if (-not $KeyVaultName) { $KeyVaultName = $outputs.keyVaultName.value }
    if (-not $FunctionAppName) { $FunctionAppName = $outputs.functionAppName.value }
}

Write-Host "OneDrive app: $ClientId | Key Vault: $KeyVaultName | Function App: $FunctionAppName"

# --- 2. Device code flow (personal Microsoft accounts: tenant 'consumers') ------------------------------
$authority = 'https://login.microsoftonline.com/consumers/oauth2/v2.0'
$scope = 'Files.ReadWrite offline_access'

$device = Invoke-RestMethod -Method Post -Uri "$authority/devicecode" -Body @{ client_id = $ClientId; scope = $scope }
Write-Host ''
Write-Host $device.message -ForegroundColor Yellow
Write-Host 'Sign in with the Microsoft account that owns the OneDrive folder.'

$token = $null
$interval = [Math]::Max([int]$device.interval, 5)
$deadline = (Get-Date).AddSeconds([int]$device.expires_in)
while (-not $token -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $interval
    try {
        $token = Invoke-RestMethod -Method Post -Uri "$authority/token" -Body @{
            grant_type  = 'urn:ietf:params:oauth:grant-type:device_code'
            client_id   = $ClientId
            device_code = $device.device_code
        }
    }
    catch {
        $errorBody = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
        switch ($errorBody.error) {
            'authorization_pending' { continue }
            'slow_down' { $interval += 5; continue }
            default { throw "Token request failed: $($_.ErrorDetails.Message)" }
        }
    }
}

if (-not $token) { throw 'Device code expired before the sign-in was completed.' }
if (-not $token.refresh_token) { throw 'No refresh token returned: check that offline_access is granted.' }

# Quick check: the token can read the drive.
$me = Invoke-RestMethod -Uri 'https://graph.microsoft.com/v1.0/me/drive' -Headers @{ Authorization = "Bearer $($token.access_token)" }
Write-Host "Signed in. Drive: $($me.owner.user.displayName) ($($me.driveType))" -ForegroundColor Green

# --- 3. Secrets ------------------------------------------------------------------------------------------
# The values go through temporary files, not the command line (visible in the process list).
$tempFile = New-TemporaryFile
try {
    Set-Content -Path $tempFile -Value $token.refresh_token -NoNewline -Encoding ascii
    Invoke-Native "Writing $RefreshTokenSecretName" {
        az keyvault secret set --vault-name $KeyVaultName -n $RefreshTokenSecretName -f $tempFile --content-type text/plain -o none
    } | Out-Null
    Write-Host "Secret $RefreshTokenSecretName saved."

    # Kept if present: the active Graph subscription was created with it (renewals do not change it).
    $ErrorActionPreference = 'Continue'
    $existingState = az keyvault secret show --vault-name $KeyVaultName -n $ClientStateSecretName --query id -o tsv 2>$null
    $ErrorActionPreference = 'Stop'

    if ($existingState) {
        Write-Host "Secret $ClientStateSecretName already present: kept."
    }
    else {
        $bytes = [byte[]]::new(32)
        [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
        Set-Content -Path $tempFile -Value ([Convert]::ToBase64String($bytes)) -NoNewline -Encoding ascii
        Invoke-Native "Writing $ClientStateSecretName" {
            az keyvault secret set --vault-name $KeyVaultName -n $ClientStateSecretName -f $tempFile --content-type text/plain -o none
        } | Out-Null
        Write-Host "Secret $ClientStateSecretName created."
    }
}
finally {
    Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
}

if ($SkipSubscription) {
    Write-Host 'Done (subscription not created: run OneDrive_SubscriptionRenewal or wait for its schedule).'
    return
}

# --- 4. Restart + subscription ---------------------------------------------------------------------------
Write-Host 'Restarting the Function App (reloads the Key Vault reference of the clientState)...'
Invoke-Native 'Function App restart' { az functionapp restart -g $ResourceGroup -n $FunctionAppName -o none } | Out-Null

$hostName = Invoke-Native 'Reading the Function App host name' {
    az functionapp show -g $ResourceGroup -n $FunctionAppName --query defaultHostName -o tsv
}
$masterKey = Invoke-Native 'Reading the Function App master key' {
    az functionapp keys list -g $ResourceGroup -n $FunctionAppName --query masterKey -o tsv
}

$uri = "https://$hostName/admin/functions/OneDrive_SubscriptionRenewal"
for ($attempt = 1; $attempt -le 6; $attempt++) {
    try {
        Invoke-RestMethod -Method Post -Uri $uri -Headers @{ 'x-functions-key' = $masterKey } -ContentType 'application/json' -Body '{"input":""}' | Out-Null
        Write-Host 'OneDrive_SubscriptionRenewal started: check its outcome in Application Insights (traces "OneDrive subscription").' -ForegroundColor Green
        return
    }
    catch {
        Write-Host "  attempt $attempt failed ($($_.Exception.Message)), retrying in 15s..."
        Start-Sleep -Seconds 15
    }
}

throw "Could not start OneDrive_SubscriptionRenewal: run it from the portal (Functions > OneDrive_SubscriptionRenewal > Test/Run)."
