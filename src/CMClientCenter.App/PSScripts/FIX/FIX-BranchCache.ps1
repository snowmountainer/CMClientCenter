#Requires -Version 5.1
<#
.SYNOPSIS
    Ensures the BranchCache service (PeerDistSvc) is running in Distributed
    mode and re-triggers MECM content-related schedules.
#>

$serviceName = 'PeerDistSvc'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($null -eq $service) {
    Write-Warning "Service '$serviceName' not found — BranchCache may not be installed on this machine."
    return
}

if ($service.Status -ne 'Running') {
    Write-Output "$serviceName is not running — resetting and starting..."

    netsh branchcache reset | Out-Null
    netsh branchcache set service mode=DISTRIBUTED | Out-Null
    sc.exe config $serviceName start= delayed-auto | Out-Null
    Start-Service -Name $serviceName

    ([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000024}') | Out-Null  # Software update scan
    ([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000023}') | Out-Null  # Software update assignments evaluation
    ([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000021}') | Out-Null  # Machine policy retrieval
    ([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000108}') | Out-Null  # Software update deployment evaluation

    $service.Refresh()
    Write-Output "$serviceName is now: $($service.Status)"
} else {
    Write-Output "$serviceName is already running."
}
