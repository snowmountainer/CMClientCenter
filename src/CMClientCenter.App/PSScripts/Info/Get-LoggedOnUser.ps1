#Requires -Version 5.1
<#
.SYNOPSIS
    Returns the domain and username of every user with an active Explorer
    session (i.e. interactively logged on).

.DESCRIPTION
    The original returned only the first explorer.exe owner, silently
    ignoring multi-session scenarios (RDS, multiple interactive logons).
    This returns all of them.
#>

$sessions = Get-WmiObject -Query "SELECT * FROM Win32_Process WHERE Name='explorer.exe'" |
    ForEach-Object {
        $owner = $_.GetOwner()
        [PSCustomObject]@{
            User   = $owner.User
            Domain = $owner.Domain
            PID    = $_.ProcessId
        }
    }

if ($null -eq $sessions) {
    Write-Output 'No interactive user sessions found (no explorer.exe running).'
} else {
    $sessions | ForEach-Object { Write-Output "$($_.Domain)\$($_.User)  (PID $($_.PID))" }
}
