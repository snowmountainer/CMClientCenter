#Requires -Version 5.1
<#
.SYNOPSIS
    Uninstalls and reinstalls the ConfigMgr client.

.DESCRIPTION
    The original version shipped with unfilled placeholders
    (\\SERVER\SCCM-CLIENT, SMSSITECODE=DP1, /MP:SERVER) that would fail
    outright if run as-is. The values below default to this environment's
    site (TT1, management point VSRV-SCCM-002.TINUTEST.LOCAL) — edit the
    three variables if you reuse this script against a different site.
#>

$siteCode = 'TT1'
$managementPoint = 'VSRV-SCCM-002.TINUTEST.LOCAL'
$clientSourcePath = "\\$managementPoint\SMS_$siteCode\Client"

Write-Output 'Uninstalling ConfigMgr client...'
Start-Process -FilePath 'C:\Windows\ccmsetup\ccmsetup.exe' -ArgumentList '/uninstall' -Wait
Start-Sleep -Seconds 30

Stop-Process -Name 'ccmsetup' -Force -ErrorAction SilentlyContinue

Remove-Item -Path 'C:\Windows\CCM' -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item -Path 'C:\Windows\SMSCFG.ini' -Force -ErrorAction SilentlyContinue

Write-Output "Reinstalling ConfigMgr client from $clientSourcePath..."
$ccmsetupArgs = @(
    '/service'
    '/forceinstall'
    '/retry:1'
    "/MP:$managementPoint"
    '/BITSPriority:FOREGROUND'
    "/Source:`"$clientSourcePath`""
    "SMSSITECODE=$siteCode"
    'RESETKEYINFORMATION=TRUE'
)
Start-Process -FilePath 'C:\Windows\ccmsetup\ccmsetup.exe' -ArgumentList $ccmsetupArgs

Start-Sleep -Seconds 20
schtasks /Run /TN 'Microsoft\Configuration Manager\Configuration Manager Client Retry Task' | Out-Null

Write-Output 'Reinstall launched — ccmsetup runs in the background; check ccmsetup.log for progress.'
