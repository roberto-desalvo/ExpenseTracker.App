<#
.SYNOPSIS
    One-shot setup of the environment: loads infra/.env, runs the Bicep bootstrap (subscription scope)
    and configures the GitHub environment used by .github/workflows/deploy-infra.yml.

.DESCRIPTION
    1. Loads infra/.env into the session (load-env.ps1) and selects AZURE_SUBSCRIPTION_ID.
    2. Deploys infra/bootstrap (resource group, GitHub OIDC deploy identity, Graph permission, RBAC,
       optional Files.Sender assignments) with `az deployment sub create`.
    3. Creates the GitHub environment and sets its secrets (bootstrap outputs + DEMO_AUTH_SIGNING_KEY)
       and variables (values from infra/.env). Empty values are skipped.

    Re-runnable: every step is idempotent. Requires Azure CLI (logged in, Owner on the subscription and
    Privileged Role Administrator in Entra ID) and GitHub CLI (`gh auth login`, admin on the repository).
    Supports -WhatIf / -Confirm.

.EXAMPLE
    .\infra\scripts\bootstrap.ps1

.EXAMPLE
    # Second run, after the first infra deploy: Files.Sender assignments only, GitHub already configured.
    .\infra\scripts\bootstrap.ps1 -SkipGitHub

.EXAMPLE
    .\infra\scripts\bootstrap.ps1 -SkipBootstrap -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    # Default: infra/.env.
    [string] $EnvFile,

    # GitHub environment referenced by the workflow jobs (and by the OIDC federated credential).
    [string] $GitHubEnvironment = 'production',

    # Name of the subscription-scope deployment (its outputs are read back for the GitHub secrets).
    [string] $DeploymentName = 'expensetracker-bootstrap',

    # Skip the Bicep bootstrap and read the outputs of the last one.
    [switch] $SkipBootstrap,

    # Skip the GitHub configuration.
    [switch] $SkipGitHub
)

$ErrorActionPreference = 'Stop'

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$infraDir = Split-Path -Parent $scriptDir
$bootstrapParams = Join-Path $infraDir 'bootstrap\bootstrap.bicepparam'

# Native commands do not throw on failure: check the exit code explicitly.
# ErrorActionPreference is relaxed inside the functions because Windows PowerShell 5.1 turns redirected
# native stderr into terminating errors when it is 'Stop'.
function Invoke-Native {
    param([Parameter(Mandatory)] [string] $Description, [Parameter(Mandatory)] [scriptblock] $Command)
    $ErrorActionPreference = 'Continue'
    $output = & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Description failed (exit code $LASTEXITCODE)." }
    return $output
}

# Runs a native command silently and returns its stdout, or $null when it fails.
function Get-NativeOutput {
    param([Parameter(Mandatory)] [scriptblock] $Command)
    $ErrorActionPreference = 'Continue'
    $output = & $Command 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    if ($null -eq $output) { return '' }
    return ($output -join "`n")
}

function Write-Step([string] $Message) { Write-Host "`n==> $Message" -ForegroundColor Cyan }

# --- Prerequisites ---------------------------------------------------------------------------------
Write-Step 'Prerequisites'
$tools = @('az')
if (-not $SkipGitHub) { $tools += 'gh' }
foreach ($tool in $tools) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        $hint = if ($tool -eq 'gh') { 'winget install --id GitHub.cli' } else { 'winget install --id Microsoft.AzureCLI' }
        throw "$tool not found. Install it ($hint) and open a new terminal."
    }
}

if ($null -eq (Get-NativeOutput { az account show --query user.name -o tsv })) { throw 'Not logged in to Azure: run az login.' }

if (-not $SkipGitHub -and $null -eq (Get-NativeOutput { gh auth status })) {
    throw 'Not logged in to GitHub: run gh auth login.'
}

# --- 1. Environment variables ----------------------------------------------------------------------
Write-Step 'Loading infra/.env'
$loadArgs = @{ SetSubscription = $true }
if ($EnvFile) { $loadArgs.Path = $EnvFile }
. (Join-Path $scriptDir 'load-env.ps1') @loadArgs

foreach ($name in 'AZURE_RESOURCE_GROUP', 'AZURE_LOCATION', 'GITHUB_REPOSITORY', 'SQL_SERVER_NAME', 'SQL_DATABASE_NAME') {
    if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name))) { throw "$name is empty in the .env file." }
}
if ($env:DEMO_AUTH_ENABLED -eq 'true' -and -not $env:DEMO_AUTH_SIGNING_KEY) {
    Write-Warning 'DEMO_AUTH_ENABLED=true without DEMO_AUTH_SIGNING_KEY: the Function App will not start until the key is in Key Vault.'
}

# --- 2. Bicep bootstrap ----------------------------------------------------------------------------
if (-not $SkipBootstrap) {
    Write-Step "Bicep bootstrap (resource group $env:AZURE_RESOURCE_GROUP, Files.Sender: $(if ($env:FILES_SENDER_ASSIGN) { $env:FILES_SENDER_ASSIGN } else { 'false' }))"
    if ($PSCmdlet.ShouldProcess("subscription $env:AZURE_SUBSCRIPTION_ID", "az deployment sub create $DeploymentName")) {
        Invoke-Native 'Bootstrap deployment' {
            az deployment sub create --location $env:AZURE_LOCATION --name $DeploymentName `
                --parameters $bootstrapParams --only-show-errors -o none
        }
        Write-Host 'Bootstrap completed.'
    }
}

# --- 3. GitHub environment -------------------------------------------------------------------------
if (-not $SkipGitHub) {
    Write-Step "GitHub environment '$GitHubEnvironment' on $env:GITHUB_REPOSITORY"
    $repo = $env:GITHUB_REPOSITORY

    $outputsJson = Get-NativeOutput { az deployment sub show --name $DeploymentName --query properties.outputs -o json }
    if (-not $outputsJson) {
        if ($WhatIfPreference) {
            $outputs = $null
            Write-Warning "Deployment $DeploymentName not found: secret values unknown in -WhatIf mode."
        }
        else { throw "Deployment $DeploymentName not found: run the bootstrap first (without -SkipBootstrap)." }
    }
    else {
        $outputs = $outputsJson | ConvertFrom-Json
    }

    if ($PSCmdlet.ShouldProcess("$repo", "create environment $GitHubEnvironment")) {
        Invoke-Native 'Environment creation' { gh api -X PUT "repos/$repo/environments/$GitHubEnvironment" --silent }
    }

    $secrets = [ordered]@{
        AZURE_CLIENT_ID       = $(if ($outputs) { $outputs.AZURE_CLIENT_ID.value })
        AZURE_TENANT_ID       = $(if ($outputs) { $outputs.AZURE_TENANT_ID.value })
        AZURE_SUBSCRIPTION_ID = $(if ($outputs) { $outputs.AZURE_SUBSCRIPTION_ID.value })
        DEMO_AUTH_SIGNING_KEY = $env:DEMO_AUTH_SIGNING_KEY
    }
    foreach ($name in $secrets.Keys) {
        $value = $secrets[$name]
        if ([string]::IsNullOrEmpty($value)) { Write-Host "  secret   $name skipped (empty)"; continue }
        if ($PSCmdlet.ShouldProcess("$repo/$GitHubEnvironment", "set secret $name")) {
            Invoke-Native "Secret $name" { gh secret set $name -R $repo --env $GitHubEnvironment --body $value }
            Write-Host "  secret   $name"
        }
    }

    # Variables read by deploy-infra.yml. CUSTOM_DOMAIN is set only when filled in .env (after moving the domain).
    $variables = 'AZURE_RESOURCE_GROUP', 'SQL_SERVER_NAME', 'SQL_DATABASE_NAME', 'SQL_SERVER_RESOURCE_GROUP',
                 'DEMO_AUTH_ENABLED', 'CUSTOM_DOMAIN', 'ONEDRIVE_IMPORT_ENABLED'
    foreach ($name in $variables) {
        $value = [Environment]::GetEnvironmentVariable($name)
        if ([string]::IsNullOrEmpty($value)) { Write-Host "  variable $name skipped (empty)"; continue }
        if ($PSCmdlet.ShouldProcess("$repo/$GitHubEnvironment", "set variable $name=$value")) {
            Invoke-Native "Variable $name" { gh variable set $name -R $repo --env $GitHubEnvironment --body $value }
            Write-Host "  variable $name=$value"
        }
    }
}

Write-Step 'Done'
Write-Host 'Next: run the "Deploy Infrastructure (Bicep)" workflow, then set FILES_SENDER_ASSIGN=true and'
Write-Host 'FILES_SENDER_UPNS in infra/.env and run this script again with -SkipGitHub (see infra/README.md).'
