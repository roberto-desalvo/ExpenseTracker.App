param(
    [Parameter(Mandatory = $true, Position = 0)]
    [Alias("File")]
    [string]$FilePath,

    [Parameter(Mandatory = $false)]
    [switch]$ImportAll,

    [Parameter(Mandatory = $false)]
    [string]$AccessToken,

    [Parameter(Mandatory = $false)]
    [string]$Audience,

    [Parameter(Mandatory = $false)]
    [switch]$NoAuth
)

$ErrorActionPreference = "Stop"

function Resolve-AudienceFromConfig {
    $configPaths = @(
        (Join-Path $PSScriptRoot "..\api\RDS.ExpenseTracker.Api\appsettings.Development.json"),
        (Join-Path $PSScriptRoot "..\api\RDS.ExpenseTracker.Api\appsettings.json")
    )

    foreach ($path in $configPaths) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            continue
        }

        try {
            $cfg = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
            if ($cfg.AzureAd.Audience -and -not [string]::IsNullOrWhiteSpace($cfg.AzureAd.Audience)) {
                return [string]$cfg.AzureAd.Audience
            }

            if ($cfg.AzureAd.ClientId -and -not [string]::IsNullOrWhiteSpace($cfg.AzureAd.ClientId)) {
                return "api://$($cfg.AzureAd.ClientId)"
            }
        }
        catch {
            # Ignore malformed config and continue
        }
    }

    return $null
}

function Resolve-AccessToken([string]$audienceInput, [string]$accessTokenInput) {
    if (-not [string]::IsNullOrWhiteSpace($accessTokenInput)) {
        return $accessTokenInput
    }

    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        throw "Token non fornito e Azure CLI non disponibile. Passa -AccessToken oppure installa/login Azure CLI."
    }

    $aud = $audienceInput
    if ([string]::IsNullOrWhiteSpace($aud)) {
        $aud = Resolve-AudienceFromConfig
    }

    if ([string]::IsNullOrWhiteSpace($aud)) {
        throw "Impossibile determinare AzureAd Audience. Passa -Audience (es. api://<client-id>) oppure -AccessToken."
    }

    $tokenJson = az account get-access-token --scope "$aud/execute_script" --output json 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($tokenJson)) {
        $tokenJson = az account get-access-token --resource $aud --output json 2>$null
    }

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($tokenJson)) {
        throw "Impossibile ottenere un token da Azure CLI. Esegui 'az login' e riprova, oppure passa -AccessToken."
    }

    $tokenObj = $tokenJson | ConvertFrom-Json
    if (-not $tokenObj.accessToken -or [string]::IsNullOrWhiteSpace($tokenObj.accessToken)) {
        throw "Azure CLI non ha restituito un access token valido."
    }

    return [string]$tokenObj.accessToken
}

if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
    throw "File non trovato: $FilePath"
}

$resolvedPath = (Resolve-Path -LiteralPath $FilePath).Path

Write-Host "Import Trade Republic CSV in corso..."
Write-Host "File: $resolvedPath"

$endpoint = "https://localhost:7120/api/import/traderepublic-csv"
if ($ImportAll) {
    $endpoint += "?importAll=true"
}
Write-Host "Endpoint: $endpoint"

if (-not (Get-Command curl.exe -ErrorAction SilentlyContinue)) {
    throw "curl.exe non trovato nel PATH. Installa curl oppure esegui da un Windows recente."
}

$curlArgs = @("-sS", "-k", "-w", "`n%{http_code}", "-X", "POST", $endpoint, "-F", "file=@$resolvedPath")

if (-not $NoAuth) {
    $token = Resolve-AccessToken -audienceInput $Audience -accessTokenInput $AccessToken
    $curlArgs += @("-H", "Authorization: Bearer $token")
}

$result = & curl.exe @curlArgs

$lines = $result -split "`r?`n"
if ($lines.Length -lt 1) {
    throw "Nessuna risposta dall'endpoint"
}

$statusCodeRaw = $lines[$lines.Length - 1]
$body = ""
if ($lines.Length -gt 1) {
    $body = ($lines[0..($lines.Length - 2)] -join "`n")
}

[int]$statusCode = 0
if (-not [int]::TryParse($statusCodeRaw, [ref]$statusCode)) {
    throw "Impossibile leggere lo status code della risposta. Output grezzo:`n$result"
}

if ($statusCode -lt 200 -or $statusCode -ge 300) {
    throw "HTTP $statusCode`n$body"
}

Write-Host "Import completato."
if (-not [string]::IsNullOrWhiteSpace($body)) {
    try {
        $json = $body | ConvertFrom-Json
        $json | ConvertTo-Json -Depth 10
    }
    catch {
        $body
    }
}
