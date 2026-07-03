#Requires -Version 5.1
<#
.SYNOPSIS
    Removes stale CCM_PrePostActions policy entries (the "SG Lock") that
    can cause software deployments to stall.
#>

$policyQuery = 'SELECT * FROM CCM_PrePostActions'

Get-WmiObject -Namespace 'ROOT\ccm\Policy\Machine\RequestedConfig' -Query $policyQuery -ErrorAction SilentlyContinue |
    Remove-WmiObject -ErrorAction SilentlyContinue

Get-WmiObject -Namespace 'ROOT\ccm\Policy\Machine\ActualConfig' -Query $policyQuery -ErrorAction SilentlyContinue |
    Remove-WmiObject -ErrorAction SilentlyContinue

([wmiclass]'root\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000108}') | Out-Null

Write-Output 'SG lock cleared. Software update deployment evaluation re-triggered.'
