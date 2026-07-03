#Requires -Version 5.1
<#
.SYNOPSIS
    Stops a running ccmsetup.exe process and restarts the ccmsetup
    service if present.

.DESCRIPTION
    ccmsetup is an installer process, not a permanent service — the
    service entry (if it exists at all) only appears while an install or
    upgrade is in progress. Stop-Process replaces the original taskkill
    call and does not raise an error when no matching process exists.
#>

Stop-Process -Name 'ccmsetup' -Force -ErrorAction SilentlyContinue
Restart-Service -Name 'ccmsetup' -Force -ErrorAction SilentlyContinue

Write-Output 'ccmsetup process stopped and service restarted (if present).'
