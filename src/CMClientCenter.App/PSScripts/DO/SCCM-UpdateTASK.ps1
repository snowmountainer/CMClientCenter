#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the ConfigMgr Client Upgrade Task, falling back to a manual
    ccmsetup /AutoUpgrade if the scheduled task can't be found/run.

.DESCRIPTION
    Same fix as SCCM-AutoUpdateClient.ps1 in this library — kept as a
    separate script since it predates that one and some setups may already
    reference it by name. schtasks.exe signals failure through its exit
    code, not a PowerShell exception, so the original try/catch never
    actually caught the failure case.
#>

schtasks /Run /TN 'Microsoft\Configuration Manager\Configuration Manager Client Upgrade Task' | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Output 'Configuration Manager Client Upgrade Task started.'
} else {
    Write-Warning 'Client Upgrade Task not found or failed to start — falling back to ccmsetup /AutoUpgrade.'
    Start-Process -FilePath 'C:\Windows\ccmsetup\ccmsetup.exe' -ArgumentList '/AutoUpgrade'
    Write-Output 'ccmsetup /AutoUpgrade launched as fallback.'
}
