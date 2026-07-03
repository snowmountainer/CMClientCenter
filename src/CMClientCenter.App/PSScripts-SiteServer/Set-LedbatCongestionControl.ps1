#Requires -Version 5.1
<#
.SYNOPSIS
    Site-server/DP tool: checks whether LEDBAT (Low Extra Delay Background
    Transport) congestion control is enabled for content transfer, and
    enables it on ports 80/443 if not.

.DESCRIPTION
    LEDBAT lets a Distribution Point's content traffic yield bandwidth to
    other network activity, so client content downloads don't compete with
    higher-priority traffic. The concept and these cmdlets remain valid in
    MECM 2509 and on Windows Server hosting a DP role. See this folder's
    LICENSE-and-SOURCE.md for why this script lives outside the
    client-facing PSScripts folder — it targets DP network settings, not
    anything on a managed client.
#>

$ledbatSetting = Get-NetTCPSetting -SettingName InternetCustom | Select-Object -ExpandProperty CongestionProvider

if ($ledbatSetting -eq 'LEDBAT') {
    Write-Output 'LEDBAT is already enabled on the InternetCustom TCP profile.'
} else {
    Write-Output 'LEDBAT is OFF — enabling it for ports 80 and 443.'
    Set-NetTCPSetting -SettingName InternetCustom -CongestionProvider LEDBAT
    New-NetTransportFilter -SettingName InternetCustom -LocalPortStart 80 -LocalPortEnd 80 -RemotePortStart 0 -RemotePortEnd 65535 | Out-Null
    New-NetTransportFilter -SettingName InternetCustom -LocalPortStart 443 -LocalPortEnd 443 -RemotePortStart 0 -RemotePortEnd 65535 | Out-Null
    Write-Output 'LEDBAT enabled.'
}
