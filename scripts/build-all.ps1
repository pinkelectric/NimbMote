[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

# This repository is developed with its portable Android toolchain. Prefer it when it is
# available so a fresh terminal does not depend on a machine-wide JAVA_HOME setting.
$workspaceRoot = Split-Path -Parent (Split-Path -Parent $projectRoot)
$toolchainRoot = Join-Path $workspaceRoot 'work\toolchains'
$portableDotnet = Join-Path $toolchainRoot 'dotnet\dotnet.exe'
$portableJdk = Get-ChildItem -LiteralPath (Join-Path $toolchainRoot 'jdk-17') -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    Select-Object -First 1
if ($null -ne $portableJdk -and (Test-Path (Join-Path $portableJdk.FullName 'bin\java.exe'))) {
    $env:JAVA_HOME = $portableJdk.FullName
    $env:ANDROID_HOME = Join-Path $toolchainRoot 'android-sdk'
    $env:GRADLE_USER_HOME = Join-Path $workspaceRoot 'work\gradle-user-home'
    $env:JAVA_TOOL_OPTIONS = '-Djava.io.tmpdir=C:\Users\mkind\AppData\Local\Temp'
    Write-Host "Using portable JDK: $env:JAVA_HOME"
}

& (Join-Path $PSScriptRoot 'validate-protocol.ps1')
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)?.Source
if (-not $dotnet -and (Test-Path $portableDotnet)) {
    $dotnet = $portableDotnet
    Write-Host "Using portable .NET SDK: $dotnet"
}
if (-not $dotnet) {
    throw '.NET 8 SDK is not available on PATH.'
}
& $dotnet build (Join-Path $projectRoot 'windows\BentleyRemote.sln') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Windows build failed.' }

$gradle = Join-Path $projectRoot 'android\gradlew.bat'
& $gradle ':app:assembleDebug'
if ($LASTEXITCODE -ne 0) { throw 'Android build failed.' }

Write-Host 'Bentley Remote builds completed.'
