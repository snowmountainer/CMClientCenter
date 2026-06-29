#Requires -Version 5.1
<#
.SYNOPSIS
    Forces install of pending/available software updates on this client,
    optionally filtered by name.

.DESCRIPTION
    The original version's "filter by name" branch referenced an undefined
    $AppEvalState variable (only $AppEvalState0/$AppEvalState1 existed), so
    that path always matched nothing. It also used -ComputerName for what
    is, in this tool, always a local WMI call — CMClientCenter already runs
    this script on the target machine via WinRM, so remoting to "localhost"
    added nothing but the slower remote-WMI code path.
#>

param(
    # Optional substring filter; leave blank (default) to install every
    # update currently available (EvaluationState 0 = available, 1 = submitted)
    [string]$UpdateNameFilter = ''
)

$availableStates = @('0', '1')   # 0 = Available, 1 = Submitted
$allUpdates = Get-WmiObject -Namespace 'ROOT\ccm\ClientSDK' -Class CCM_SoftwareUpdate |
    Where-Object { $_.EvaluationState -in $availableStates }

if ($UpdateNameFilter) {
    $allUpdates = $allUpdates | Where-Object { $_.Name -like "*$UpdateNameFilter*" }
}

if (-not $allUpdates) {
    Write-Output 'No matching updates available to install.'
    return
}

Invoke-WmiMethod -Namespace 'ROOT\ccm\ClientSDK' -Class CCM_SoftwareUpdatesManager -Name InstallUpdates -ArgumentList (, [System.Management.ManagementObject[]]$allUpdates) | Out-Null

Write-Output "Install triggered for $($allUpdates.Count) update(s):"
$allUpdates | ForEach-Object { Write-Output "  $($_.Name)" }
