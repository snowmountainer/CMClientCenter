#Requires -Version 5.1
<#
.SYNOPSIS
    Checks CAS.log for the "IsCacheCopyNeeded" error and clears the
    registry callback that causes it, then triggers an update install.
#>

$logPath = 'C:\Windows\CCM\Logs\CAS.log'
$pattern = 'IsCacheCopyNeeded'

$updates = Get-WmiObject -Namespace 'ROOT\ccm\ClientSDK' -Query 'SELECT * FROM CCM_SoftwareUpdate' -ErrorAction SilentlyContinue

if (Select-String -Path $logPath -Pattern $pattern -Quiet -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path 'HKLM:\Software\Microsoft\SMS\Mobile Client\Software Distribution' -Name 'IsCacheCopyNeededCallBack' -ErrorAction SilentlyContinue
    Restart-Service -Name CcmExec -Force
    if ($updates) {
        ([wmiclass]'ROOT\ccm\ClientSDK:CCM_SoftwareUpdatesManager').InstallUpdates([System.Management.ManagementObject[]]$updates) | Out-Null
    }
    Write-Output "IsCacheCopyNeeded error found and cleared — CcmExec restarted, update install triggered."
} else {
    if ($updates) {
        ([wmiclass]'ROOT\ccm\ClientSDK:CCM_SoftwareUpdatesManager').InstallUpdates([System.Management.ManagementObject[]]$updates) | Out-Null
    }
    Write-Output 'No IsCacheCopyNeeded error found in CAS.log — update install triggered anyway.'
}
