#Requires -Version 5.1
<#
.SYNOPSIS
    Fixes the "Waiting for turn to start updates." stall some clients show
    in UpdatesDeployment.log, by clearing the SCCM policy cache that's
    usually the actual cause and re-pulling fresh policy.

.DESCRIPTION
    The original version of this fix deleted the entire local Group Policy
    cache (C:\Windows\System32\GroupPolicy\*) to force a clean policy
    re-download. That's far more than this problem needs — it wipes every
    GPO-applied setting on the machine, not just SCCM's, until the next
    gpupdate cycle. This version clears only the SCCM client policy cache
    (ROOT\ccm\Policy\Machine\RequestedConfig / ActualConfig), the same
    targeted approach used by the "remove SG lock" fix, then triggers a
    machine policy refresh and re-evaluates updates.
#>

$logPath = 'C:\Windows\CCM\Logs\UpdatesDeployment.log'
$stalled = (Test-Path -Path $logPath) -and (Select-String -Path $logPath -Pattern 'Waiting for turn to start updates\.' -Quiet)

if ($stalled) {
    Write-Output 'Stall pattern found in UpdatesDeployment.log — clearing SCCM policy cache.'

    Rename-Item -Path $logPath -NewName 'UpdatesDeployment-old.log' -Force -ErrorAction SilentlyContinue

    # Clear only the SCCM client's own policy cache, not the whole local
    # GPO store — this is what actually needs to be re-pulled.
    $policyQuery = "SELECT * FROM CCM_PrePostActions"
    Get-WmiObject -Namespace 'ROOT\ccm\Policy\Machine\RequestedConfig' -Query $policyQuery -ErrorAction SilentlyContinue | Remove-WmiObject -ErrorAction SilentlyContinue
    Get-WmiObject -Namespace 'ROOT\ccm\Policy\Machine\ActualConfig' -Query $policyQuery -ErrorAction SilentlyContinue | Remove-WmiObject -ErrorAction SilentlyContinue

    Restart-Service -Name CcmExec -Force
    Restart-Service -Name wuauserv -Force -ErrorAction SilentlyContinue

    $sms = [wmiclass]'ROOT\ccm:SMS_Client'
    $sms.TriggerSchedule('{00000000-0000-0000-0000-000000000021}') | Out-Null   # Machine policy retrieval
    $sms.TriggerSchedule('{00000000-0000-0000-0000-000000000022}') | Out-Null   # Machine policy evaluation
    $sms.TriggerSchedule('{00000000-0000-0000-0000-000000000113}') | Out-Null   # Software update scan
    $sms.TriggerSchedule('{00000000-0000-0000-0000-000000000108}') | Out-Null   # Software update deployment evaluation
    (New-Object -ComObject Microsoft.CCM.UpdatesStore).RefreshServerComplianceState()

    Write-Output 'Policy cache cleared, machine policy and update scan re-triggered.'
} else {
    Write-Output 'No "Waiting for turn to start updates." stall found — re-evaluating updates as usual.'
}

$updates = Get-WmiObject -Namespace 'ROOT\ccm\ClientSDK' -Query 'SELECT * FROM CCM_SoftwareUpdate'
if ($updates) {
    ([wmiclass]'ROOT\ccm\ClientSDK:CCM_SoftwareUpdatesManager').InstallUpdates([System.Management.ManagementObject[]]$updates) | Out-Null
    Write-Output "Install triggered for $($updates.Count) pending update(s)."
} else {
    Write-Output 'No pending updates to install.'
}
