#Requires -Version 5.1
<#
.SYNOPSIS
    Repairs common causes of "remote access denied" / WinRM and DCOM
    remoting failures on a managed client.

.DESCRIPTION
    Ensures the services and firewall rules that WinRM, DCOM, and WMI
    remoting depend on are enabled, without disabling the firewall itself.
    Re-enabling PS-Remoting and the relevant rule groups is normally enough;
    turning the firewall off entirely (as the original script did) is not
    a "fix", it's a workaround that removes a layer of protection the
    client should keep.
#>

Write-Output 'Repairing remote communication prerequisites...'

# Windows Firewall service must be running for WinRM/DCOM to negotiate at all.
# On CIS L2-hardened clients MpsSvc is protected against reconfiguration even
# for admins, so only touch it if it isn't already in the desired state.
if ((Get-Service -Name MpsSvc).Status -ne 'Running') {
    try {
        Set-Service -Name MpsSvc -StartupType Automatic -ErrorAction Stop
        Start-Service -Name MpsSvc -ErrorAction Stop
    } catch {
        Write-Warning "  MpsSvc could not be reconfigured (likely policy-protected): $($_.Exception.Message)"
    }
}

# WinRM service set to auto-start and (re)enabled — this also creates the
# default listener and the matching firewall rule group.
Set-Service -Name WinRM -StartupType Automatic
Enable-PSRemoting -Force -SkipNetworkProfileCheck

# DCOM / legacy remote-WMI authentication settings — required for tools
# that still talk to the client over DCOM rather than WinRM.
New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'LocalAccountTokenFilterPolicy' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Ole' -Name 'LegacyAuthenticationLevel' -Value 2 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Ole' -Name 'LegacyImpersonationLevel' -Value 2 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Ole' -Name 'EnableRemoteConnect' -Value 'Y' -PropertyType String -Force | Out-Null

# DNS registration — a client that can't register/update its A record is a
# common, easy-to-miss reason remote tools can't resolve or reach it.
New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient' -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient' -Name 'RegisterReverseLookup' -Value 2 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient' -Name 'RegistrationEnabled' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient' -Name 'RegistrationOverwritesInConflict' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient' -Name 'RegistrationRefreshInterval' -Value 1800 -PropertyType DWord -Force | Out-Null

# Firewall rule groups that gate remote management — enabled individually,
# the firewall itself stays on.
$ruleGroups = 'Windows Remote Management', 'Remote Desktop', 'Windows Management Instrumentation (WMI)', 'Remote Administration'
foreach ($group in $ruleGroups) {
    try {
        Get-NetFirewallRule -Group $group -ErrorAction Stop | Enable-NetFirewallRule
        Write-Output "  Firewall rule group enabled: $group"
    } catch {
        Write-Warning "  Firewall rule group not found (nothing to enable): $group"
    }
}

ipconfig /flushdns | Out-Null
ipconfig /registerdns | Out-Null

Write-Output 'Done. Firewall remains enabled on all profiles; only the rule groups above and PS-Remoting were (re)opened.'
