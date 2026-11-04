<#
.SYNOPSIS
    Loads the infrastructure deploy variables from infra/.env into the current PowerShell session.

.DESCRIPTION
    Reads KEY=VALUE lines (blank lines and # comments are ignored, surrounding quotes are stripped)
    and sets them as process environment variables, so that `az deployment group ...` with
    infra/parameters/prod.bicepparam can read them via readEnvironmentVariable().
    Empty values remove the variable, so Bicep defaults apply.

.EXAMPLE
    . .\infra\scripts\load-env.ps1
    az deployment group what-if -g $env:AZURE_RESOURCE_GROUP --parameters .\infra\parameters\prod.bicepparam

.EXAMPLE
    .\infra\scripts\load-env.ps1 -Path .\infra\.env.dev -SetSubscription
#>
[CmdletBinding()]
param(
    # Default: infra/.env (next to this scripts/ folder).
    [string] $Path,

    # Also run `az account set` with AZURE_SUBSCRIPTION_ID, when set.
    [switch] $SetSubscription
)

$ErrorActionPreference = 'Stop'

if (-not $Path) {
    # $PSScriptRoot can be empty in Windows PowerShell 5.1 when dot-sourced.
    $scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $Path = Join-Path $scriptDir '..\.env'
}

if (-not (Test-Path -LiteralPath $Path)) {
    throw "File not found: $Path (copy infra/.env.example to infra/.env and fill it in)."
}

$loaded = @()
$lineNumber = 0
foreach ($line in Get-Content -LiteralPath $Path -Encoding UTF8) {
    $lineNumber++
    $trimmed = $line.Trim()
    if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }

    if ($trimmed -notmatch '^(?:export\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        Write-Warning "Line $lineNumber ignored (not KEY=VALUE): $trimmed"
        continue
    }

    $name = $Matches[1]
    $value = $Matches[2].Trim()
    if ($value.Length -ge 2 -and (
            ($value.StartsWith('"') -and $value.EndsWith('"')) -or
            ($value.StartsWith("'") -and $value.EndsWith("'")))) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    # Empty string removes the variable (readEnvironmentVariable defaults then apply).
    [Environment]::SetEnvironmentVariable($name, $(if ($value -eq '') { $null } else { $value }), 'Process')
    $loaded += $name
}

Write-Host "Loaded $($loaded.Count) variables from $(Resolve-Path -LiteralPath $Path): $($loaded -join ', ')"

$required = 'AZURE_RESOURCE_GROUP', 'SQL_SERVER_NAME', 'SQL_DATABASE_NAME'
$missing = $required | Where-Object { [string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($_)) }
if ($missing) {
    Write-Warning "Required variables still empty: $($missing -join ', ')"
}

if ($SetSubscription -and $env:AZURE_SUBSCRIPTION_ID) {
    az account set --subscription $env:AZURE_SUBSCRIPTION_ID
    if ($LASTEXITCODE -ne 0) { throw "az account set failed (run az login first)." }
    Write-Host "Active subscription: $env:AZURE_SUBSCRIPTION_ID"
}
