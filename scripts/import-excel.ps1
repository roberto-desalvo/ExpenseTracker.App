param(
    [Parameter(Mandatory = $true, Position = 0)]
    [Alias("File")]
    [string]$FilePath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
    throw "File non trovato: $FilePath"
}

$resolvedPath = (Resolve-Path -LiteralPath $FilePath).Path
$fileName = [System.IO.Path]::GetFileName($resolvedPath)

Write-Host "Import in corso..."
Write-Host "File: $resolvedPath"

$endpoint = "https://localhost:7120/api/import/excel"
Write-Host "Endpoint: $endpoint"

if (-not (Get-Command curl.exe -ErrorAction SilentlyContinue)) {
    throw "curl.exe non trovato nel PATH. Installa curl oppure esegui da un Windows recente."
}

$result = & curl.exe -sS -k -w "`n%{http_code}" -X POST $endpoint -F "file=@$resolvedPath"

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
