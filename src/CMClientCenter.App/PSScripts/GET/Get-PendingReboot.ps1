#Requires -Version 5.1
<#
.SYNOPSIS
    Checks whether this client has a pending reboot, and from what source
    (Component-Based Servicing, Windows Update, a pending computer
    rename/domain join, pending file rename operations, or the ConfigMgr
    client SDK).

.DESCRIPTION
    Adapted from Brian Wilhite's widely-used Get-PendingReboot function
    (2012/2015). The original supported querying an arbitrary list of
    remote computers via -ComputerName/-ErrorLog and WMI's StdRegProv over
    DCOM. CMClientCenter only ever needs the local machine — this script
    already runs on the target client via WinRM — so all of the
    multi-computer plumbing, the -ComputerName splats on every WMI/CIM
    call, and the per-computer try/catch/error-log path have been removed.
    The underlying registry checks themselves are unchanged and remain
    valid on Windows 11 / Windows Server 2022+.
#>

$hklm = [UInt32] '0x80000002'
$registryProvider = [WMIClass] 'root\default:StdRegProv'

# Component-Based Servicing reboot-pending key (Vista/Server 2008 and
# later — i.e. every currently supported Windows version).
$cbsSubKeys = $registryProvider.EnumKey($hklm, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\')
$cbsRebootPending = $cbsSubKeys.sNames -contains 'RebootPending'

# Windows Update / Auto Update reboot-required key.
$wuauSubKeys = $registryProvider.EnumKey($hklm, 'SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\')
$windowsUpdateRebootRequired = $wuauSubKeys.sNames -contains 'RebootRequired'

# PendingFileRenameOperations — populated by installers (and sometimes AV
# definition updates) that need a reboot to finish replacing a locked file.
$pendingFileRenameValue = $registryProvider.GetMultiStringValue($hklm, 'SYSTEM\CurrentControlSet\Control\Session Manager\', 'PendingFileRenameOperations').sValue
$pendingFileRename = [bool]$pendingFileRenameValue

# A pending computer rename or domain join also requires a reboot to apply.
$netlogonSubKeys = $registryProvider.EnumKey($hklm, 'SYSTEM\CurrentControlSet\Services\Netlogon').sNames
$pendingDomainJoin = ($netlogonSubKeys -contains 'JoinDomain') -or ($netlogonSubKeys -contains 'AvoidSpnSet')

$activeComputerName = $registryProvider.GetStringValue($hklm, 'SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName\', 'ComputerName').sValue
$pendingComputerName = $registryProvider.GetStringValue($hklm, 'SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName\', 'ComputerName').sValue
$pendingComputerRename = ($activeComputerName -ne $pendingComputerName) -or $pendingDomainJoin

# ConfigMgr client's own reboot-pending determination, when the client is
# installed and CcmExec is running.
$ccmRebootPending = $null
try {
    $ccmResult = Invoke-WmiMethod -Namespace 'ROOT\ccm\ClientSDK' -Class CCM_ClientUtilities -Name DetermineIfRebootPending -ErrorAction Stop
    if ($ccmResult.ReturnValue -ne 0) {
        Write-Warning "CCM_ClientUtilities.DetermineIfRebootPending returned error code $($ccmResult.ReturnValue)"
    }
    $ccmRebootPending = [bool]($ccmResult.IsHardRebootPending -or $ccmResult.RebootPending)
} catch {
    # CCM_ClientUtilities not present (client not installed) or CcmExec not
    # running — leave $ccmRebootPending as $null rather than $false, since
    # "unknown" and "confirmed not pending" aren't the same thing.
    $ccmService = Get-Service -Name CcmExec -ErrorAction SilentlyContinue
    if ($ccmService -and $ccmService.Status -ne 'Running') {
        Write-Warning 'CcmExec service is not running — could not query ConfigMgr reboot-pending state.'
    }
}

[PSCustomObject]@{
    ComputerName        = $env:COMPUTERNAME
    CBServicing         = $cbsRebootPending
    WindowsUpdate       = $windowsUpdateRebootRequired
    CCMClientSDK        = $ccmRebootPending
    PendComputerRename  = $pendingComputerRename
    PendFileRename      = $pendingFileRename
    PendFileRenameValue = $pendingFileRenameValue
    RebootPending       = [bool]($pendingComputerRename -or $cbsRebootPending -or $windowsUpdateRebootRequired -or $ccmRebootPending -or $pendingFileRename)
}
