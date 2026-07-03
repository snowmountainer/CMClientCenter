#Requires -Version 5.1
<#
.SYNOPSIS
    Forces the ConfigMgr client to generate a new Hardware ID/GUID by
    stopping CcmExec, clearing SMSCFG.ini and the client certificate, then
    restarting the service.

.DESCRIPTION
    The original version's while-loop waiting for the service to stop had
    no timeout — if CcmExec never reached "Stopped" (e.g. a hung dependent
    process), the script would block forever. This caps the wait instead
    of looping indefinitely.
#>

$serviceName = 'CcmExec'
$service = Get-Service -Name $serviceName
Stop-Service -Name $serviceName

$maxRetries = 12
$retryDelaySeconds = 5
for ($attempt = 1; $attempt -le $maxRetries; $attempt++) {
    $service.Refresh()
    if ($service.Status -eq 'Stopped') { break }
    Write-Output "Waiting for $serviceName to stop (attempt $attempt of $maxRetries, current status: $($service.Status))..."
    Start-Sleep -Seconds $retryDelaySeconds
}

if ($service.Status -ne 'Stopped') {
    Write-Warning "$serviceName did not stop after $($maxRetries * $retryDelaySeconds) seconds — aborting without clearing the GUID."
    return
}

Write-Output "$serviceName is stopped. Clearing SMSCFG.ini and client certificate..."
Remove-Item -Path 'C:\Windows\SMSCFG.ini' -Force -ErrorAction SilentlyContinue
Remove-Item -Path 'HKLM:\Software\Microsoft\SystemCertificates\SMS\Certificates\*' -Force -ErrorAction SilentlyContinue

Start-Service -Name $serviceName
Write-Output 'CcmExec restarted — a new Hardware ID/GUID will be generated on next policy evaluation.'
