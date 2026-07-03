#Requires -Version 5.1
<#
.SYNOPSIS
    Schedules a one-time forced restart for today at 20:00 (or a time
    you specify).

.DESCRIPTION
    Creates (or overwrites, /F) a scheduled task that runs
    'Restart-Computer -Force' as SYSTEM at the given time today. Useful
    when you need to guarantee a restart happens during a maintenance
    window after the user has been notified.
#>

param(
    # Time-of-day for the restart in HH:mm format (24-hour clock).
    [string] $RestartTime = '20:00'
)

schtasks /create /RU SYSTEM /F /TN 'Reboot' /TR 'powershell.exe -Command Restart-Computer -Force' /SC once /ST $RestartTime | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Output "Forced restart scheduled for today at $RestartTime."
} else {
    Write-Warning "schtasks returned exit code $LASTEXITCODE — restart may not have been scheduled."
}
