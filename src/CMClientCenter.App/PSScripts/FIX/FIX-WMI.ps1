#Requires -Version 5.1
<#
.SYNOPSIS
    Repairs a corrupted WMI repository: re-registers the core WMI binaries,
    resets/salvages the repository, and recompiles the SCCM client's own
    MOF files.

.DESCRIPTION
    The original version branched on [System.Environment]::OSVersion's
    major/minor version to support Windows 2000/XP/2003 (Major -eq 5),
    which used a different, older repair mechanism (mofcomp against every
    .mof/.mfl file plus rundll32 wbemupgd). That branch can never be
    reached on Windows 10/11 (Major -eq 10) and has been removed — only the
    Vista-and-later winmgmt.exe /resetrepository + /salvagerepository path
    remains, which is also Microsoft's current documented approach.
#>

Write-Output 'Stopping WMI-dependent services...'
Stop-Service -Name CcmExec -Force -ErrorAction SilentlyContinue
Stop-Service -Name Winmgmt -Force

Write-Output 'Re-registering core WMI binaries...'
$wmiBinaries = 'unsecapp.exe', 'wmiadap.exe', 'wmiapsrv.exe', 'wmiprvse.exe', 'scrcons.exe'
foreach ($wbemPath in @("$env:SystemRoot\System32\wbem", "$env:SystemRoot\SysWOW64\wbem")) {
    if (-not (Test-Path -Path $wbemPath)) { continue }
    Push-Location -Path $wbemPath
    foreach ($binary in $wmiBinaries) {
        if (Test-Path -Path $binary) {
            Write-Output "  Registering $binary"
            & ".\$binary" /RegServer
        } elseif ($wbemPath -eq "$env:SystemRoot\System32\wbem") {
            Write-Warning "  $binary not found in $wbemPath"
        }
    }
    Pop-Location
}

Write-Output 'Resetting and salvaging the WMI repository...'
& "$env:SystemRoot\System32\wbem\winmgmt.exe" /resetrepository
& "$env:SystemRoot\System32\wbem\winmgmt.exe" /salvagerepository

Write-Output 'Recompiling ConfigMgr client WMI managed objects...'
Push-Location -Path 'C:\Windows\CCM'
Get-ChildItem -Path . -Include '*.mof', '*.mfl' -Name | ForEach-Object {
    & mofcomp.exe $_
}
Pop-Location

Write-Output 'Restarting services...'
Start-Service -Name Winmgmt
Start-Service -Name CcmExec -ErrorAction SilentlyContinue

Write-Output 'WMI repair complete.'
