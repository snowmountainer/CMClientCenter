#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the ConfigMgr Client Retry Task, falling back to ccmsetup.exe if
    the scheduled task can't be found/run.

.DESCRIPTION
    schtasks.exe signals failure through its exit code, not a PowerShell
    exception — wrapping it in try/catch (as the original script did) never
    actually catches anything. This checks $LASTEXITCODE instead.
#>

schtasks /Run /TN 'Microsoft\Configuration Manager\Configuration Manager Client Retry Task' | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Output 'Client Retry Task started.'
} else {
    Write-Warning 'Client Retry Task not found or failed to start — falling back to ccmsetup.exe.'
    Start-Process -FilePath 'C:\Windows\ccmsetup\ccmsetup.exe'
    Write-Output 'ccmsetup.exe launched as fallback.'
}
