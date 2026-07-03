#Requires -Version 5.1
<#
.SYNOPSIS
    Site-server/DP tool: ensures the WSUS, ConfigMgr, WID database, and IIS
    services this site role depends on are running.

.DESCRIPTION
    Targets services that only exist on a Software Update Point / Site
    Server (WsusService, the WID database instance, IIS), not on an
    ordinary managed client — see this folder's LICENSE-and-SOURCE.md for
    why it lives outside the client-facing PSScripts folder.

    The original version's while-loops had no timeout: if a service never
    reaches "Running" (e.g. missing permissions, a dependency that's also
    down), the script would block forever. This caps each service at a
    fixed number of retries before giving up and reporting the failure.
#>

function Wait-ForServiceRunning {
    param(
        [string]$ServiceName,
        [int]$MaxRetries = 6,
        [int]$RetryDelaySeconds = 5
    )

    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $service) {
        Write-Warning "Service '$ServiceName' not found on this machine — skipping (this role may not be installed here)."
        return
    }

    if ($service.Status -eq 'Running') {
        Write-Output "$ServiceName is already running."
        return
    }

    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
        Write-Output "$ServiceName status: $($service.Status) — starting (attempt $attempt of $MaxRetries)..."
        Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds $RetryDelaySeconds
        $service.Refresh()
        if ($service.Status -eq 'Running') {
            Write-Output "$ServiceName is now running."
            return
        }
    }

    Write-Warning "$ServiceName did not reach Running state after $MaxRetries attempts (last status: $($service.Status))."
}

Wait-ForServiceRunning -ServiceName 'WsusService'
Wait-ForServiceRunning -ServiceName 'CcmExec'
Wait-ForServiceRunning -ServiceName 'MSSQL$MICROSOFT##WID'
Wait-ForServiceRunning -ServiceName 'W3SVC'

# Nudges the site's WSUS Control Manager thread to re-check its
# configuration against WSUS now, rather than waiting for its next cycle.
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\SMS\Components\SMS_EXECUTIVE\Threads\SMS_WSUS_CONTROL_MANAGER' -Name 'Requested Operation' -Value 'Start' -Force -ErrorAction SilentlyContinue
