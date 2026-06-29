#Requires -Version 5.1
<#
.SYNOPSIS
    Full Windows Update reset: stops the WU/BITS/crypto services, clears
    the SoftwareDistribution and Catroot2 caches, removes stale client
    registration, resets WinSock, and re-triggers detection.

.DESCRIPTION
    Modernized from a Windows 7/8-era "reset Windows Update" script.
    Changes from the original:
      - 'appidsvc' (Application Identity) doesn't reliably exist under
        that name on Windows 10/11 — every Stop-Service/Start-Service call
        now uses -ErrorAction SilentlyContinue so a missing/renamed
        service doesn't abort the script.
      - The ~35-DLL regsvr32 block was a Windows 7/8 WSUS-client-repair
        workaround. Most of those DLLs (Internet Explorer components like
        mshtml.dll/shdocvw.dll/browseui.dll, and several superseded WU
        client DLLs) are not part of how Windows 10/11's Update Orchestrator
        / USO talks to WSUS/MECM. Trimmed to the handful that can still
        matter (crypto/signing and the core WU client DLLs).
      - wuauclt.exe /ResetAuthorization /DetectNow and /reportnow are
        no-ops since Windows 10 1809 — removed in favor of the WMI
        schedule triggers already used elsewhere in this library.
      - Delivery Optimization / WindowsUpdate\AU policy keys are no longer
        duplicated here — see "WindowsUpdate and DeliveryOptimization
        settings.ps1" in this folder for that, so the two scripts don't
        drift out of sync with each other.
      - Fixed a malformed Remove-ItemProperty call (WUServer/WUStatusServer
        were passed as a second positional argument instead of via -Name).
#>

Write-Output '1. Stopping Windows Update services...'
Stop-Service -Name BITS -ErrorAction SilentlyContinue
Stop-Service -Name wuauserv -ErrorAction SilentlyContinue
Stop-Service -Name appidsvc -ErrorAction SilentlyContinue
Stop-Service -Name cryptsvc -ErrorAction SilentlyContinue

Write-Output '2. Removing QMGR (BITS queue manager) data file...'
Remove-Item -Path "$env:ALLUSERSPROFILE\Application Data\Microsoft\Network\Downloader\qmgr*.dat" -ErrorAction SilentlyContinue

Write-Output '3. Renaming SoftwareDistribution and Catroot2...'
Rename-Item -Path "$env:SystemRoot\SoftwareDistribution" -NewName 'SoftwareDistribution.old' -ErrorAction SilentlyContinue
Rename-Item -Path "$env:SystemRoot\System32\Catroot2" -NewName 'Catroot2.old' -ErrorAction SilentlyContinue

Write-Output '4. Removing old Windows Update log...'
Remove-Item -Path "$env:SystemRoot\WindowsUpdate.log" -ErrorAction SilentlyContinue

Write-Output '5. Resetting BITS/wuauserv service security descriptors to default...'
sc.exe sdset bits 'D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;AU)(A;;CCLCSWRPWPDTLOCRRC;;;PU)' | Out-Null
sc.exe sdset wuauserv 'D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;AU)(A;;CCLCSWRPWPDTLOCRRC;;;PU)' | Out-Null

Write-Output '6. Re-registering core update/crypto DLLs...'
Push-Location -Path "$env:SystemRoot\System32"
$dlls = 'wuapi.dll', 'wuaueng.dll', 'wucltux.dll', 'wups.dll', 'wups2.dll', 'wuweb.dll',
        'qmgr.dll', 'qmgrprxy.dll', 'softpub.dll', 'wintrust.dll', 'cryptdlg.dll',
        'initpki.dll', 'mssip32.dll'
foreach ($dll in $dlls) {
    if (Test-Path -Path $dll) {
        regsvr32.exe /s $dll
    }
}
Pop-Location

Write-Output '7. Clearing stale WSUS client registration...'
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate' -Name 'AccountDomainSid' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate' -Name 'PingID' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate' -Name 'SusClientId' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate' -Name 'SusClientIdValidation' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -Name 'WUServer' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -Name 'WUStatusServer' -ErrorAction SilentlyContinue

Write-Output '8. Resetting WinSock and WinHTTP proxy...'
netsh winsock reset | Out-Null
netsh winhttp reset proxy | Out-Null

Write-Output '9. Clearing stuck BITS jobs...'
Import-Module -Name BitsTransfer
Get-BitsTransfer -AllUsers -ErrorAction SilentlyContinue | Where-Object { $_.JobState -eq 'TransientError' } | Remove-BitsTransfer
Get-BitsTransfer -AllUsers -ErrorAction SilentlyContinue | Where-Object { $_.JobState -eq 'Suspended' } | Resume-BitsTransfer

Write-Output '10. Resetting BranchCache and applying policy...'
netsh branchcache reset | Out-Null
netsh branchcache set service mode=DISTRIBUTED | Out-Null
gpupdate.exe /Force | Out-Null

Write-Output '11. Starting Windows Update services...'
Start-Service -Name BITS -ErrorAction SilentlyContinue
Start-Service -Name wuauserv -ErrorAction SilentlyContinue
Start-Service -Name appidsvc -ErrorAction SilentlyContinue
Start-Service -Name cryptsvc -ErrorAction SilentlyContinue

Write-Output '12. Re-triggering policy and update detection...'
$sms = [wmiclass]'ROOT\ccm:SMS_Client'
$sms.TriggerSchedule('{00000000-0000-0000-0000-000000000021}') | Out-Null   # Machine policy retrieval
$sms.TriggerSchedule('{00000000-0000-0000-0000-000000000108}') | Out-Null   # Software update deployment evaluation
$sms.TriggerSchedule('{00000000-0000-0000-0000-000000000024}') | Out-Null   # Software update scan
$sms.TriggerSchedule('{00000000-0000-0000-0000-000000000023}') | Out-Null   # Software update assignments evaluation
(New-Object -ComObject Microsoft.CCM.UpdatesStore).RefreshServerComplianceState()

Write-Output 'Windows Update reset complete.'
