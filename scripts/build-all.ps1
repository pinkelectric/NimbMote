[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

& (Join-Path $PSScriptRoot 'validate-protocol.ps1')
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 8 SDK is not available on PATH.'
}
& dotnet build (Join-Path $projectRoot 'windows\BentleyRemote.sln') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Windows build failed.' }

$gradle = Join-Path $projectRoot 'android\gradlew.bat'
& $gradle ':app:assembleDebug'
if ($LASTEXITCODE -ne 0) { throw 'Android build failed.' }

Write-Host 'Bentley Remote builds completed.'

