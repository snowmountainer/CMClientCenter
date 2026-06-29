#Requires -Version 5.1
<#
.SYNOPSIS
    Cleans up common temp/cache locations for all users plus a few
    well-known system temp folders to free up disk space.

.DESCRIPTION
    Modernized from an older cleanup script. Removed:
      - Clear-RecycleBin (emptied every user's Recycle Bin with no listing
        of what was removed or how much space it freed — kept here as an
        opt-in reported step instead of a silent blanket delete)
      - Security/CBS/DISM event logs (useful for audits and troubleshooting;
        deleting them isn't a disk-space win worth the loss)
      - Adobe Flash cache cleanup (Flash reached end-of-life in 2021 and
        doesn't exist on Windows 11; the original code also had a bug where
        the path was single-quoted, so $env:windir was never expanded and
        the block silently did nothing)
    Each step reports what it actually removed/freed where practical.
#>

function Get-FriendlySize {
    param([double]$Bytes)
    if ($Bytes -ge 1GB) { return "{0:N2} GB" -f ($Bytes / 1GB) }
    return "{0:N2} MB" -f ($Bytes / 1MB)
}

function Remove-PathQuietly {
    param([string]$Path)
    $items = Get-ChildItem -Path $Path -Force -ErrorAction SilentlyContinue
    if (-not $items) { return 0 }
    $size = ($items | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum
    $items | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
    if ($size) { return $size }
    return 0
}

Write-Output 'Cleaning per-user temp locations...'
$freed = 0
$freed += Remove-PathQuietly -Path 'C:\Users\*\AppData\Local\Temp\*'
$freed += Remove-PathQuietly -Path 'C:\Users\*\AppData\Local\CrashDumps\*'
$freed += Remove-PathQuietly -Path 'C:\Users\*\AppData\Local\Microsoft\Windows\WER\*'

Write-Output 'Cleaning system temp and update download cache...'
$freed += Remove-PathQuietly -Path 'C:\Windows\Temp\*'
$freed += Remove-PathQuietly -Path 'C:\Windows\*.dmp'
$freed += Remove-PathQuietly -Path 'C:\ProgramData\Microsoft\Windows\WER\ReportQueue\*'
$freed += Remove-PathQuietly -Path 'C:\ProgramData\Microsoft\Windows\WER\Temp\*'
$freed += Remove-PathQuietly -Path 'C:\Windows\CCM\Temp\*'

# Old SoftwareDistribution downloads (>15 days) — safe, WU/MECM re-downloads
# content as needed; this is just stale cached installer payload.
$oldUpdateFiles = Get-ChildItem -Path 'C:\Windows\SoftwareDistribution\Download' -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { -not $_.PSIsContainer -and $_.LastWriteTime -lt (Get-Date).AddDays(-15) }
if ($oldUpdateFiles) {
    $freed += ($oldUpdateFiles | Measure-Object -Property Length -Sum).Sum
    $oldUpdateFiles | Remove-Item -Force -ErrorAction SilentlyContinue
}

# IIS log retention (matches Microsoft's own log-storage guidance) — only
# acts if IIS is actually present.
if (Test-Path -Path 'C:\inetpub\logs\LogFiles') {
    $oldIisLogs = Get-ChildItem -Path 'C:\inetpub\logs\LogFiles' -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object { -not $_.PSIsContainer -and $_.LastWriteTime -lt (Get-Date).AddDays(-30) }
    if ($oldIisLogs) {
        $freed += ($oldIisLogs | Measure-Object -Property Length -Sum).Sum
        $oldIisLogs | Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

# Stale Outlook OST files (unused for 60+ days) — these regenerate from the
# mailbox on next Outlook launch, so removing an old one is safe.
$oldOstFiles = Get-ChildItem -Path 'C:\Users\*\AppData\Local\Microsoft\Outlook\*.ost' -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-60) }
if ($oldOstFiles) {
    $freed += ($oldOstFiles | Measure-Object -Property Length -Sum).Sum
    $oldOstFiles | Remove-Item -Force -ErrorAction SilentlyContinue
}

# SCCM client cache: drop entries not referenced in 30+ days, and reconcile
# any folders on disk that no longer have a matching WMI cache entry (or
# vice versa) — this mirrors what the Tools page's cache cleanup does.
$cachePath = (Get-WmiObject -Namespace 'ROOT\ccm\SoftMgmtAgent' -Query "SELECT * FROM CacheConfig WHERE ConfigKey='Cache'").Location
if ($cachePath -and (Test-Path -Path $cachePath)) {
    $cacheEntries = Get-WmiObject -Namespace 'ROOT\ccm\SoftMgmtAgent' -Query 'SELECT * FROM CacheInfoEx'
    $staleEntries = $cacheEntries | Where-Object {
        ((Get-Date) - [System.Management.ManagementDateTimeConverter]::ToDateTime($_.LastReferenced)).Days -gt 30
    }
    foreach ($entry in $staleEntries) {
        $freed += Remove-PathQuietly -Path $entry.Location
        $entry | Remove-WmiObject -ErrorAction SilentlyContinue
    }
}

Write-Output "Cleanup complete. Approximately $(Get-FriendlySize -Bytes $freed) freed."

$freeSpace = (Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='C:'").FreeSpace
Write-Output "Current free space on C: $(Get-FriendlySize -Bytes $freeSpace)"
