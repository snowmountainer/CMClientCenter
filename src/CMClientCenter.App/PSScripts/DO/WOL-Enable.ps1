#Requires -Version 5.1
<#
.SYNOPSIS
    Enables Wake-on-LAN (magic packet) on this machine's network adapters.

.DESCRIPTION
    Fixed two issues from the original: $Adapter.enable = "$True" assigned
    a string ("True") rather than a boolean to a WMI property — this
    usually still "worked" because WMI marshals it, but it's not correct
    PowerShell. Also replaced the gwmi alias with the full cmdlet name for
    readability, and added output so the result of running this from the
    Console page is visible rather than silent.
#>

$wakeAdapters = Get-WmiObject -Namespace 'root\wmi' -Class MSPower_DeviceWakeEnable
if ($wakeAdapters) {
    foreach ($adapter in $wakeAdapters) {
        $adapter.Enable = $true
        $adapter.Put() | Out-Null
    }
    Write-Output "Wake-on-LAN enable flag set on $($wakeAdapters.Count) adapter instance(s)."
} else {
    Write-Warning 'No MSPower_DeviceWakeEnable instances found — this adapter/driver may not support Wake-on-LAN.'
}

$magicPacketAdapters = Get-WmiObject -Namespace 'root\wmi' -Class MSNdis_DeviceWakeOnMagicPacketOnly
if ($magicPacketAdapters) {
    foreach ($adapter in $magicPacketAdapters) {
        $adapter.EnableWakeOnMagicPacketOnly = $true
        $adapter.Put() | Out-Null
    }
    Write-Output "Wake-on-Magic-Packet-only flag set on $($magicPacketAdapters.Count) adapter instance(s)."
} else {
    Write-Warning 'No MSNdis_DeviceWakeOnMagicPacketOnly instances found.'
}
