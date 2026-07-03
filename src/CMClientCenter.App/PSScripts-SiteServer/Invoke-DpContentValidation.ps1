#Requires -Version 5.1
<#
.SYNOPSIS
    Site-server/DP tool: runs the "Content Validation" scheduled task
    immediately instead of waiting for its normal schedule.

.DESCRIPTION
    The "Content Validation" task only exists on a machine hosting the
    Distribution Point role — it checks that package content on disk still
    matches its expected hash. See this folder's LICENSE-and-SOURCE.md for
    why this script lives outside the client-facing PSScripts folder.
#>

$taskPath = '\Microsoft\Configuration Manager\Content Validation'
$task = Get-ScheduledTask -TaskPath '\Microsoft\Configuration Manager\' -TaskName 'Content Validation' -ErrorAction SilentlyContinue

if (-not $task) {
    Write-Warning "Scheduled task 'Content Validation' not found — this machine likely doesn't have the Distribution Point role installed."
    return
}

Start-ScheduledTask -TaskName $taskPath
Write-Output 'Content Validation task started.'
