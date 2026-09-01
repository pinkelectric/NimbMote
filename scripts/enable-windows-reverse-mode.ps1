#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$WindowsUser = "$env:USERDOMAIN\$env:USERNAME"
)

$ErrorActionPreference = 'Stop'
$url = 'http://+:45893/bentley/'

& netsh http delete urlacl "url=$url" 2>$null | Out-Null
& netsh http add urlacl "url=$url" "user=$WindowsUser"
if ($LASTEXITCODE -ne 0) { throw 'Could not create HTTP URL reservation.' }

$existingRule = Get-NetFirewallRule -DisplayName 'Bentley Remote reverse WebSocket' -ErrorAction SilentlyContinue
if ($null -eq $existingRule) {
    New-NetFirewallRule -DisplayName 'Bentley Remote reverse WebSocket' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 45893 -Profile Private | Out-Null
}

Write-Host "Reverse mode is enabled for $WindowsUser on TCP 45893 (Private profile)."

