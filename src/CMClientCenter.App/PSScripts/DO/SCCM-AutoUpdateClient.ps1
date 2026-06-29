#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the ConfigMgr Client Upgrade Task, falling back to a manual
    ccmsetup /AutoUpgrade if the scheduled task can't be found/run.

.DESCRIPTION
    schtasks.exe signals failure through its exit code, not a PowerShell
    exception — wrapping it in try/catch (as the original script did) never
    actually catches anything, so the fallback path never ran. This checks
    $LASTEXITCODE instead.
#>

schtasks /Run /TN 'Microsoft\Configuration Manager\Configuration Manager Client Upgrade Task' | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Output 'Configuration Manager Client Upgrade Task started.'
} else {
    Write-Warning 'Client Upgrade Task not found or failed to start — falling back to ccmsetup /AutoUpgrade.'
    Start-Process -FilePath 'C:\Windows\ccmsetup\ccmsetup.exe' -ArgumentList '/AutoUpgrade'
    Start-Sleep -Seconds 30
    schtasks /Run /TN 'Microsoft\Configuration Manager\Configuration Manager Client Upgrade Task' | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Output 'Client Upgrade Task started after fallback.'
    } else {
        Write-Warning 'Client Upgrade Task still unavailable after ccmsetup /AutoUpgrade fallback.'
    }
}
