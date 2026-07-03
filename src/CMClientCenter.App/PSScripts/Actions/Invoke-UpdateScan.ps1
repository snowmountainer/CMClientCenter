#Requires -Version 5.1
<#
.SYNOPSIS
    Triggers a full software update scan, deployment re-evaluation, and
    compliance state refresh.

.DESCRIPTION
    wuauclt.exe /ResetAuthorization /DetectNow and /reportnow have been
    no-ops since Windows 10 1809 — wuauclt no longer talks to the Update
    Orchestrator the way it used to. The two WMI schedule triggers below
    are what actually drive a rescan on Windows 10/11 clients.
#>

$sms = [wmiclass]'ROOT\ccm:SMS_Client'
$sms.TriggerSchedule('{00000000-0000-0000-0000-000000000113}') | Out-Null   # Software update scan
$sms.TriggerSchedule('{00000000-0000-0000-0000-000000000108}') | Out-Null   # Software update deployment evaluation
(New-Object -ComObject Microsoft.CCM.UpdatesStore).RefreshServerComplianceState()

Write-Output 'Full update scan and deployment evaluation triggered.'
