[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$schemaPath = Join-Path $projectRoot 'protocol\protocol.schema.json'
$examplesPath = Join-Path $projectRoot 'protocol\examples'

$schema = Get-Content -Raw -LiteralPath $schemaPath | ConvertFrom-Json
$allowedTypes = @($schema.properties.type.enum)
$failures = @()

Get-ChildItem -LiteralPath $examplesPath -Filter '*.json' | Where-Object Name -ne 'hmac-test-vector.json' | ForEach-Object {
    try {
        $message = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json
        if ($message.version -ne 1) { throw 'version must be 1' }
        if ($allowedTypes -notcontains $message.type) { throw "unknown type: $($message.type)" }
        if (-not [Guid]::TryParse($message.id, [ref]([Guid]::Empty))) { throw 'id is not a UUID' }
        if ($null -eq $message.payload) { throw 'payload is required' }
        Write-Host "OK  $($_.Name)"
    } catch {
        $failures += "$($_.Name): $($_.Exception.Message)"
    }
}

$vectorPath = Join-Path $examplesPath 'hmac-test-vector.json'
$systemExample = Get-Content -Raw -LiteralPath (Join-Path $examplesPath 'system.action.json') | ConvertFrom-Json
if (@('lock', 'sleep', 'restart', 'shutdown') -notcontains $systemExample.payload.action) {
    $failures += 'system.action.json: action is outside the fixed allowlist'
} else {
    Write-Host 'OK  system.action allowlist'
}

$vector = Get-Content -Raw -LiteralPath $vectorPath | ConvertFrom-Json
$secret = [Convert]::FromBase64String($vector.secretBase64)
$body = [Text.Encoding]::UTF8.GetBytes("$($vector.clientId)`n$($vector.timestamp)`n$($vector.nonce)")
$hmac = [Security.Cryptography.HMACSHA256]::new($secret)
try {
    $actualProof = [Convert]::ToBase64String($hmac.ComputeHash($body))
} finally {
    $hmac.Dispose()
}
if ($actualProof -ne $vector.proofBase64) {
    $failures += 'hmac-test-vector.json: HMAC proof mismatch'
} else {
    Write-Host 'OK  hmac-test-vector.json'
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Protocol examples are valid.'
