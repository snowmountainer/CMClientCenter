#Requires -Version 5.1
<#
.SYNOPSIS
    Cancels a pending SCCM-mandated reboot and clears the related
    reboot-tracking registry data.

.DESCRIPTION
    Removed a leftover comment about a PowerShell 2.0 Remove-ItemProperty
    workaround — irrelevant since this library targets PowerShell 5.1 and
    above. shutdown -a now has -ErrorAction so the script doesn't surface
    a (harmless) error when no shutdown/restart is actually pending to
    abort.
#>

Remove-Item -Path 'HKLM:\SOFTWARE\Microsoft\SMS\Mobile Client\Reboot Management\RebootData' -ErrorAction SilentlyContinue
Remove-Item -Path 'HKLM:\SOFTWARE\Microsoft\SMS\Mobile Client\Updates Management\Handler\UpdatesRebootStatus\*' -ErrorAction SilentlyContinue
Remove-ItemProperty -Name '*' -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired' -ErrorAction SilentlyContinue

shutdown -a 2>$null
Restart-Service -Name CcmExec -Force

Write-Output 'Pending reboot cancelled and reboot-tracking data cleared.'
