[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$GradleArgs)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$version = '8.11.1'
$androidRoot = $PSScriptRoot
$cacheRoot = Join-Path $androidRoot '.gradle-dist'
$zipPath = Join-Path $cacheRoot "gradle-$version-bin.zip"
$checksumPath = "$zipPath.sha256"
$gradleHome = Join-Path $cacheRoot "gradle-$version"
$distributionUrl = "https://downloads.gradle.org/distributions/gradle-$version-bin.zip"
$checksumUrl = "$distributionUrl.sha256"
$gradleExe = if ($env:OS -eq 'Windows_NT') {
    Join-Path $gradleHome 'bin\gradle.bat'
} else {
    Join-Path $gradleHome 'bin/gradle'
}

function Download-WithRetry([string]$Uri, [string]$Destination) {
    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($curl) {
        & $curl.Source -L --fail --show-error --retry 8 --retry-all-errors --retry-delay 4 `
            --connect-timeout 30 --continue-at - --output $Destination $Uri
        if ($LASTEXITCODE -eq 0) { return }
        Write-Warning 'curl could not finish the Gradle download; retrying with PowerShell.'
    }

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Invoke-WebRequest -UseBasicParsing -Uri $Uri -OutFile $Destination
            return
        }
        catch {
            if ($attempt -eq 5) { throw }
            Write-Warning "PowerShell download attempt $attempt failed; retrying in 4 seconds."
            Start-Sleep -Seconds 4
        }
    }
}

if (-not (Test-Path -LiteralPath $gradleExe)) {
    New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

    if (-not (Test-Path -LiteralPath $checksumPath) -or (Get-Item -LiteralPath $checksumPath).Length -lt 64) {
        if (Test-Path -LiteralPath $checksumPath) { Remove-Item -LiteralPath $checksumPath -Force }
        Write-Host "Downloading the Gradle $version checksum..."
        Download-WithRetry $checksumUrl $checksumPath
    }
    $expectedSha256 = [regex]::Match((Get-Content -Raw -LiteralPath $checksumPath), '[A-Fa-f0-9]{64}').Value
    if (-not $expectedSha256) { throw 'Could not parse the official Gradle SHA-256 checksum.' }

    if (Test-Path -LiteralPath $zipPath) {
        $cachedSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
        if ($cachedSha256 -ne $expectedSha256) {
            Write-Warning 'Discarding an incomplete cached Gradle archive.'
            Remove-Item -LiteralPath $zipPath -Force
        }
    }
    if (-not (Test-Path -LiteralPath $zipPath)) {
        Write-Host "Downloading Gradle $version..."
        Download-WithRetry $distributionUrl $zipPath
    }

    $actualSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    if ($actualSha256 -ne $expectedSha256) {
        throw "Gradle SHA-256 mismatch. Expected $expectedSha256, got $actualSha256."
    }
    Write-Host 'Gradle SHA-256 verified.'
    Expand-Archive -LiteralPath $zipPath -DestinationPath $cacheRoot -Force
}

Push-Location $androidRoot
try {
    & $gradleExe @GradleArgs
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
