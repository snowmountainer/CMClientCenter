#Requires -Version 5.1
<#
.SYNOPSIS
    Reports pending update counts by state and triggers an install for any
    that are not yet in "reboot pending" state.

.DESCRIPTION
    Fixed a pipeline-variable bug from the original: the two Where-Object
    filters referenced $TargetedUpdates.EvaluationState (the full
    collection's property array) instead of $_.EvaluationState (the
    current pipeline item). The original happened to return the right
    counts by accident because PowerShell evaluated the array as a truthy
    value, but the intent was clearly per-item filtering.

    Removed the Write-EventLog calls — writing to the Application event
    log is side-effectful and unexpected for a script you run interactively
    from the Console page to check update state.
#>

(New-Object -ComObject Microsoft.CCM.UpdatesStore).RefreshServerComplianceState()

$allTargeted = Get-WmiObject -Namespace 'ROOT\ccm\ClientSDK' -Class CCM_SoftwareUpdate -Filter 'ComplianceState=0'

$countApproved     = ($allTargeted | Measure-Object).Count
$countPending      = ($allTargeted | Where-Object { $_.EvaluationState -ne 8 } | Measure-Object).Count
$countRebootPending = ($allTargeted | Where-Object { $_.EvaluationState -eq 8 } | Measure-Object).Count

Write-Output "Targeted: $countApproved | Pending install: $countPending | Reboot pending: $countRebootPending"

if ($countPending -gt 0) {
    $toInstall = [System.Management.ManagementObject[]]($allTargeted | Where-Object { $_.EvaluationState -ne 8 })
    try {
        Invoke-WmiMethod -Namespace 'ROOT\ccm\ClientSDK' -Class CCM_SoftwareUpdatesManager -Name InstallUpdates -ArgumentList (, $toInstall) | Out-Null
        Write-Output "Install triggered for $countPending update(s)."
    } catch {
        Write-Warning "Could not trigger install: $($_.Exception.Message)"
    }
} else {
    Write-Output 'No updates pending install — client is compliant.'
}
