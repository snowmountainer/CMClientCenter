#Requires -Version 5.1
<#
.SYNOPSIS
    Clears BITS jobs stuck in TransientError and resumes any suspended
    BITS jobs, for all users.

.DESCRIPTION
    Runs as two short-lived scheduled tasks (as SYSTEM) rather than
    directly in this script's own session, since Get-BitsTransfer -AllUsers
    needs to enumerate jobs across every user profile, not just the
    account this script happens to run as.

    Fixed a bug in the original: the second scheduled task's trigger set
    $t.EndBoundary (the first task's trigger variable, a typo) instead of
    $T1.EndBoundary, so the "Resume BITS" task's end boundary was never
    actually set.
#>

Set-Service -Name MpsSvc -StartupType Automatic
Start-Service -Name MpsSvc

$clearAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-command &{Get-BitsTransfer -AllUsers | Where-Object { $_.JobState -eq "TransientError" } | Remove-BitsTransfer}'
$clearTrigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddSeconds(10)
$clearTrigger.EndBoundary = (Get-Date).AddSeconds(20).ToString('s')
$clearSettings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DeleteExpiredTaskAfter '00:02:00'
Register-ScheduledTask -Force -User SYSTEM -TaskName 'Fix Stuck BITS' -Action $clearAction -Trigger $clearTrigger -Settings $clearSettings | Out-Null
Start-ScheduledTask -TaskName 'Fix Stuck BITS'

$resumeAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-command &{Get-BitsTransfer -AllUsers | Where-Object { $_.JobState -eq "Suspended" } | Resume-BitsTransfer}'
$resumeTrigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddSeconds(10)
$resumeTrigger.EndBoundary = (Get-Date).AddSeconds(20).ToString('s')
$resumeSettings = New-ScheduledTaskSettingsSet -StartWhenAvailable -DeleteExpiredTaskAfter '00:02:00'
Register-ScheduledTask -Force -User SYSTEM -TaskName 'Resume BITS' -Action $resumeAction -Trigger $resumeTrigger -Settings $resumeSettings | Out-Null
Start-ScheduledTask -TaskName 'Resume BITS'

Write-Output 'Stuck BITS jobs cleared and suspended jobs resumed (via two short-lived SYSTEM scheduled tasks).'
