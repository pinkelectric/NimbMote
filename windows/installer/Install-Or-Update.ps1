[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'

$productName = 'Deskora'
$processName = 'BentleyRemote.Agent'
$runKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$runValueName = 'Deskora'
$legacyRunValueName = 'BentleyRemote.Agent'
$installRoot = Join-Path $env:LOCALAPPDATA 'BentleyRemote'
$agentDirectory = Join-Path $installRoot 'Agent'
$agentPath = Join-Path $agentDirectory 'BentleyRemote.Agent.exe'
$sourceDirectory = $PSScriptRoot
$sourceAgent = Join-Path $sourceDirectory 'BentleyRemote.Agent.exe'

if (-not (Test-Path -LiteralPath $sourceAgent -PathType Leaf)) {
    throw "BentleyRemote.Agent.exe was not found next to this installer: $sourceDirectory"
}

$sourceVersion = (Get-Item -LiteralPath $sourceAgent).VersionInfo.ProductVersion
if ([string]::IsNullOrWhiteSpace($sourceVersion)) { $sourceVersion = 'unknown' }
$startupCommand = '"{0}"' -f $agentPath

Write-Host "$productName installer"
Write-Host "Source version: $sourceVersion"
Write-Host "Managed path: $agentPath"

# This name is exclusive to Bentley Remote. It includes both the former v0.1.1
# registration name and the current name; no other Run values are enumerated or changed.
if ($PSCmdlet.ShouldProcess("process $processName", 'Stop running Bentley Remote Agent instances')) {
    Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction Stop
}

$stagingDirectory = Join-Path $installRoot ('.staging-' + [guid]::NewGuid().ToString('N'))
$backupDirectory = Join-Path $installRoot ('.previous-' + [guid]::NewGuid().ToString('N'))

try {
    if ($PSCmdlet.ShouldProcess($stagingDirectory, 'Create staging directory and copy release files')) {
        New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
        Get-ChildItem -LiteralPath $sourceDirectory -Force | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $stagingDirectory -Recurse -Force
        }
    }

    if ($PSCmdlet.ShouldProcess($agentDirectory, 'Atomically replace managed Bentley Remote Agent files')) {
        if (Test-Path -LiteralPath $agentDirectory) { Move-Item -LiteralPath $agentDirectory -Destination $backupDirectory }
        Move-Item -LiteralPath $stagingDirectory -Destination $agentDirectory
        if (Test-Path -LiteralPath $backupDirectory) { Remove-Item -LiteralPath $backupDirectory -Recurse -Force }
    }

    if ($PSCmdlet.ShouldProcess($runKeyPath, 'Register Bentley Remote managed autostart')) {
        New-Item -Path $runKeyPath -Force | Out-Null
        # v0.1.1 used the same product value but an unpacked-path command. Replace it exactly.
        Remove-ItemProperty -Path $runKeyPath -Name $legacyRunValueName -ErrorAction SilentlyContinue
        New-ItemProperty -Path $runKeyPath -Name $runValueName -Value $startupCommand -PropertyType String -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($agentPath, 'Start installed Bentley Remote Agent')) {
        Start-Process -FilePath $agentPath -WorkingDirectory $agentDirectory
    }
}
catch {
    if ((Test-Path -LiteralPath $backupDirectory) -and -not (Test-Path -LiteralPath $agentDirectory)) {
        Move-Item -LiteralPath $backupDirectory -Destination $agentDirectory -ErrorAction SilentlyContinue
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $stagingDirectory) { Remove-Item -LiteralPath $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host "Installed version: $sourceVersion"
Write-Host "Executable path: $agentPath"
Write-Host "Autostart: $runValueName = $startupCommand"
