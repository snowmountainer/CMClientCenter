#Requires -Version 5.1
<#
.SYNOPSIS
    Reports free and total disk space for a drive (default: C:).
#>

param(
    [string] $Drive = 'C:'
)

$disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='$Drive'" -ErrorAction SilentlyContinue

if ($null -eq $disk) {
    Write-Warning "Drive '$Drive' not found."
    return
}

$freeGB  = [Math]::Round($disk.FreeSpace  / 1GB, 2)
$totalGB = [Math]::Round($disk.Size       / 1GB, 2)
$usedGB  = [Math]::Round(($disk.Size - $disk.FreeSpace) / 1GB, 2)
$freePct = [Math]::Round(($disk.FreeSpace / $disk.Size) * 100, 1)

Write-Output "$Drive  Free: ${freeGB} GB  Used: ${usedGB} GB  Total: ${totalGB} GB  ($freePct% free)"
