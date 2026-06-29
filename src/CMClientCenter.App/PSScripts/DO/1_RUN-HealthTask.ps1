#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the (optional, third-party) ConfigMgr Client Health scheduled
    task, installing it first from a network share if it isn't present
    yet.

.DESCRIPTION
    For the "ConfigMgr Client Health" tool by Anders Rodland —
    https://www.andersrodland.com/configmgr-client-health-0-8-1-bugfixes/
    — not something this project ships itself. If you don't use Client
    Health, this script will just report that it's not configured.

    The original version built a deeply nested schtasks /tr string
    (a scheduled task whose action is itself an schtasks /create command)
    with an unfilled \\LOCAL-SERVER_HERE\ placeholder — fragile to edit and
    never actually pointed anywhere. This version uses a single
    -InstallXmlPath parameter and registers the task directly from
    PowerShell instead of through a nested scheduled-task indirection.
#>

param(
    # UNC path to the Client Health task definition XML. Leave as the
    # default placeholder and the script will just report that Client
    # Health isn't configured for this environment, rather than failing.
    [string]$InstallXmlPath = '\\LOCAL-SERVER_HERE\ConfigMgr-Client-Health.xml'
)

$taskRegistryPath = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\ConfigMgr Client Health'

if (Test-Path -Path $taskRegistryPath) {
    schtasks /Run /TN 'ConfigMgr Client Health' | Out-Null
    Write-Output 'ConfigMgr Client Health task started.'
    return
}

if ($InstallXmlPath -eq '\\LOCAL-SERVER_HERE\ConfigMgr-Client-Health.xml' -or -not (Test-Path -Path $InstallXmlPath)) {
    Write-Output 'ConfigMgr Client Health is not installed on this client, and no reachable -InstallXmlPath was provided — nothing to run.'
    return
}

Write-Output "ConfigMgr Client Health task not found — installing from $InstallXmlPath..."
schtasks /create /f /xml $InstallXmlPath /tn 'ConfigMgr Client Health' | Out-Null
schtasks /Run /TN 'ConfigMgr Client Health' | Out-Null
Write-Output 'ConfigMgr Client Health installed and started.'
