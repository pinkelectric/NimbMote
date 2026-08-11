[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$WindowsConfiguration = 'Release',
    [switch]$PackageOnly
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$projectRoot = Split-Path -Parent $PSScriptRoot
$outputsRoot = Split-Path -Parent $projectRoot
$workspaceRoot = Split-Path -Parent $outputsRoot
$workRoot = Join-Path $workspaceRoot 'work'
$downloadsRoot = Join-Path $workRoot 'downloads'
$toolchainsRoot = Join-Path $workRoot 'toolchains'
$gradleUserHome = Join-Path $workRoot 'gradle-user-home'
$packageRoot = Join-Path $outputsRoot 'BentleyRemote-binaries'
$packageZip = Join-Path $outputsRoot 'BentleyRemote-binaries.zip'

$dotnetRoot = Join-Path $toolchainsRoot 'dotnet'
$jdkExtractRoot = Join-Path $toolchainsRoot 'jdk-17'
$androidSdkRoot = Join-Path $toolchainsRoot 'android-sdk'

$dotnetInstallScript = Join-Path $downloadsRoot 'dotnet-install.ps1'
$jdkArchive = Join-Path $downloadsRoot 'microsoft-jdk-17.0.20-windows-x64.zip'
$jdkChecksum = Join-Path $downloadsRoot 'microsoft-jdk-17.0.20-windows-x64.zip.sha256sum.txt'
$androidToolsArchive = Join-Path $downloadsRoot 'commandlinetools-win-15859902_latest.zip'

$dotnetInstallUrl = 'https://dot.net/v1/dotnet-install.ps1'
$jdkUrl = 'https://download.visualstudio.microsoft.com/download/pr/0f58ae64-1e29-47f5-b1ce-ef382709d781/66a348e218e226c0370b071d25bd7879/microsoft-jdk-17.0.20-windows-x64.zip'
$jdkChecksumUrl = 'https://download.visualstudio.microsoft.com/download/pr/0f58ae64-1e29-47f5-b1ce-ef382709d781/1579378e01272da43414341fa746b9a3/microsoft-jdk-17.0.20-windows-x64.zip.sha256sum.txt'
$androidToolsUrl = 'https://dl.google.com/android/repository/commandlinetools-win-15859902_latest.zip'
$androidToolsSha256 = '90ae805d20434428bffcb699c290860f19bb5f66a67e6b330067e3de801fb04a'

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Download-File([string]$Uri, [string]$Destination) {
    if ((Test-Path -LiteralPath $Destination) -and (Get-Item -LiteralPath $Destination).Length -gt 0) {
        Write-Host "Using cached $(Split-Path -Leaf $Destination)"
        return
    }

    Write-Host "Downloading $(Split-Path -Leaf $Destination)"
    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($curl) {
        & $curl.Source -L --fail --show-error --retry 8 --retry-all-errors --retry-delay 4 `
            --connect-timeout 30 --continue-at - --output $Destination $Uri
        if ($LASTEXITCODE -eq 0) { return }
        Write-Warning 'curl could not finish the download; retrying with PowerShell.'
    }

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Invoke-WebRequest -UseBasicParsing -Uri $Uri -OutFile $Destination
            return
        }
        catch {
            if ($attempt -eq 5) {
                throw "Download failed: $Uri`n$($_.Exception.Message)"
            }
            Write-Warning "PowerShell download attempt $attempt failed; retrying in 4 seconds."
            Start-Sleep -Seconds 4
        }
    }
}

function Assert-Sha256([string]$Path, [string]$Expected) {
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Expected.ToLowerInvariant()) {
        throw "SHA-256 mismatch for $(Split-Path -Leaf $Path). Expected $Expected, got $actual."
    }
    Write-Host "SHA-256 verified: $(Split-Path -Leaf $Path)"
}

function Download-VerifiedFile([string]$Uri, [string]$Destination, [string]$ExpectedSha256) {
    if (Test-Path -LiteralPath $Destination) {
        $cachedHash = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($cachedHash -ne $ExpectedSha256.ToLowerInvariant()) {
            Write-Warning "Discarding an incomplete cached $(Split-Path -Leaf $Destination)."
            Remove-Item -LiteralPath $Destination -Force
        }
    }
    Download-File $Uri $Destination
    Assert-Sha256 $Destination $ExpectedSha256
}

function Reset-Directory([string]$Path, [string]$AllowedParent) {
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    $resolvedParent = [IO.Path]::GetFullPath($AllowedParent).TrimEnd('\') + '\'
    if (-not $resolvedPath.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside ${AllowedParent}: $Path"
    }
    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $resolvedPath | Out-Null
}

New-Item -ItemType Directory -Force -Path $downloadsRoot, $toolchainsRoot, $gradleUserHome | Out-Null

Write-Step 'Installing portable .NET 8 SDK'
$dotnetExe = Join-Path $dotnetRoot 'dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetExe)) {
    Download-File $dotnetInstallUrl $dotnetInstallScript
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $dotnetInstallScript `
        -Channel '8.0' -Quality 'GA' -Architecture 'x64' -InstallDir $dotnetRoot -NoPath
    if ($LASTEXITCODE -ne 0) { throw 'Portable .NET SDK installation failed.' }
}
if (-not (Test-Path -LiteralPath $dotnetExe)) { throw "dotnet.exe was not created in $dotnetRoot" }

Write-Step 'Installing portable Microsoft OpenJDK 17'
$javaExe = Get-ChildItem -LiteralPath $jdkExtractRoot -Recurse -Filter java.exe -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\bin\\java\.exe$' } |
    Select-Object -First 1
if (-not $javaExe) {
    Download-File $jdkChecksumUrl $jdkChecksum
    $expectedJdkSha = [regex]::Match((Get-Content -Raw -LiteralPath $jdkChecksum), '[A-Fa-f0-9]{64}').Value
    if (-not $expectedJdkSha) { throw 'Could not parse the Microsoft OpenJDK checksum file.' }
    Download-VerifiedFile $jdkUrl $jdkArchive $expectedJdkSha
    Reset-Directory $jdkExtractRoot $toolchainsRoot
    Expand-Archive -LiteralPath $jdkArchive -DestinationPath $jdkExtractRoot -Force
    $javaExe = Get-ChildItem -LiteralPath $jdkExtractRoot -Recurse -Filter java.exe -File |
        Where-Object { $_.FullName -match '\\bin\\java\.exe$' } |
        Select-Object -First 1
}
if (-not $javaExe) { throw "java.exe was not found below $jdkExtractRoot" }
$jdkHome = Split-Path -Parent (Split-Path -Parent $javaExe.FullName)

Write-Step 'Installing portable Android SDK command-line tools'
$sdkManager = Join-Path $androidSdkRoot 'cmdline-tools\latest\bin\sdkmanager.bat'
if (-not (Test-Path -LiteralPath $sdkManager)) {
    Download-VerifiedFile $androidToolsUrl $androidToolsArchive $androidToolsSha256
    $androidExtractRoot = Join-Path $workRoot 'android-commandline-extract'
    Reset-Directory $androidExtractRoot $workRoot
    Expand-Archive -LiteralPath $androidToolsArchive -DestinationPath $androidExtractRoot -Force
    $extractedTools = Join-Path $androidExtractRoot 'cmdline-tools'
    $latestTools = Join-Path $androidSdkRoot 'cmdline-tools\latest'
    Reset-Directory $latestTools $androidSdkRoot
    Copy-Item -Path (Join-Path $extractedTools '*') -Destination $latestTools -Recurse -Force
}
if (-not (Test-Path -LiteralPath $sdkManager)) { throw "sdkmanager.bat was not created at $sdkManager" }

$env:DOTNET_ROOT = $dotnetRoot
$env:JAVA_HOME = $jdkHome
$env:ANDROID_HOME = $androidSdkRoot
$env:ANDROID_SDK_ROOT = $androidSdkRoot
$env:GRADLE_USER_HOME = $gradleUserHome
$env:Path = "$dotnetRoot;$jdkHome\bin;$androidSdkRoot\platform-tools;$env:Path"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

$androidRoot = Join-Path $projectRoot 'android'
$apkPath = Join-Path $androidRoot 'app\build\outputs\apk\debug\app-debug.apk'
$windowsPackage = Join-Path $packageRoot 'windows-x64'
$agentExe = Join-Path $windowsPackage 'BentleyRemote.Agent.exe'

if (-not $PackageOnly) {
    Write-Step 'Accepting Android SDK licenses and installing API 35 tools'
    (1..100 | ForEach-Object { 'y' }) | & $sdkManager "--sdk_root=$androidSdkRoot" --licenses | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Android SDK license acceptance failed.' }
    & $sdkManager "--sdk_root=$androidSdkRoot" 'platform-tools' 'platforms;android-35' 'build-tools;35.0.0'
    if ($LASTEXITCODE -ne 0) { throw 'Android SDK package installation failed.' }

    Write-Step 'Validating the protocol fixtures'
    & (Join-Path $PSScriptRoot 'validate-protocol.ps1')
    if (-not $?) { throw 'Protocol validation failed.' }

    Write-Step 'Building the Android debug APK'
    $sdkPathForGradle = $androidSdkRoot.Replace('\', '\\')
    Set-Content -LiteralPath (Join-Path $androidRoot 'local.properties') -Encoding ASCII -Value "sdk.dir=$sdkPathForGradle"
    & (Join-Path $androidRoot 'gradlew.bat') ':app:clean' ':app:assembleDebug' '--stacktrace' `
        '--no-daemon' '--max-workers=2' '--console=plain'
    if ($LASTEXITCODE -ne 0) { throw 'Android debug APK build failed.' }
    if (-not (Test-Path -LiteralPath $apkPath)) { throw "APK was not created at $apkPath" }

    Write-Step 'Publishing the self-contained Windows x64 agent'
    Reset-Directory $packageRoot $outputsRoot
    New-Item -ItemType Directory -Force -Path $windowsPackage | Out-Null
    $windowsProject = Join-Path $projectRoot 'windows\src\BentleyRemote.Agent\BentleyRemote.Agent.csproj'
    & $dotnetExe publish $windowsProject -c $WindowsConfiguration -r 'win-x64' --self-contained 'true' `
        -p:PublishSingleFile=false -o $windowsPackage
    if ($LASTEXITCODE -ne 0) { throw 'Windows x64 publish failed.' }
}

if (-not (Test-Path -LiteralPath $apkPath)) { throw "APK was not found at $apkPath" }
if (-not (Test-Path -LiteralPath $agentExe)) { throw "Windows agent was not found at $agentExe" }

Write-Step 'Packaging binaries and checksums'
$androidPackage = Join-Path $packageRoot 'android'
New-Item -ItemType Directory -Force -Path $androidPackage | Out-Null
Copy-Item -LiteralPath $apkPath -Destination (Join-Path $androidPackage 'BentleyRemote-debug.apk') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $packageRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'TESTING.md') -Destination $packageRoot -Force

$dotnetVersion = (& $dotnetExe --version | Select-Object -First 1)
$savedErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$javaVersion = ((& $javaExe.FullName -version 2>&1 | Select-Object -First 1).ToString())
$ErrorActionPreference = $savedErrorActionPreference
$report = @"
# Bentley Remote binary build report

- Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')
- Windows: win-x64, self-contained, $WindowsConfiguration
- Android: debug APK, API 35
- .NET SDK: $dotnetVersion
- Java: $javaVersion
- SDK location: work/toolchains (not included in the package)

Hardware integration still requires the checks in TESTING.md on Windows 11 and the Galaxy A56.
"@
Set-Content -LiteralPath (Join-Path $packageRoot 'BUILD_REPORT.md') -Encoding UTF8 -Value $report

$checksumFile = Join-Path $packageRoot 'SHA256SUMS.txt'
$checksumLines = Get-ChildItem -LiteralPath $packageRoot -Recurse -File |
    Where-Object { $_.FullName -ne $checksumFile } |
    Sort-Object FullName |
    ForEach-Object {
        $relative = $_.FullName.Substring($packageRoot.Length + 1).Replace('\', '/')
        '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
    }
Set-Content -LiteralPath $checksumFile -Encoding ASCII -Value $checksumLines

if (Test-Path -LiteralPath $packageZip) { Remove-Item -LiteralPath $packageZip -Force }
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $packageZip -CompressionLevel Optimal
$zipHash = (Get-FileHash -LiteralPath $packageZip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$packageZip.sha256.txt" -Encoding ASCII -Value "$zipHash  $(Split-Path -Leaf $packageZip)"

Write-Host "`nBuild completed." -ForegroundColor Green
Write-Host "APK:     $(Join-Path $androidPackage 'BentleyRemote-debug.apk')"
Write-Host "Agent:   $agentExe"
Write-Host "Package: $packageZip"
Write-Host "SHA-256: $zipHash"
