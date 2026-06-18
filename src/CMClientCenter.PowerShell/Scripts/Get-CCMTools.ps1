# Get-CCMTools.ps1 — PS 5.1 kompatibel

$result = [PSCustomObject]@{
    CacheSizeMB      = 0
    CacheUsedMB      = 0
    CacheFreeMB      = 0
    CachePath        = ""
    # CacheItems werden separat via Get-CCMCacheItems.ps1 abgefragt
    # RebootSources als pipe-getrennter String serialisiert (WinRM-sicher)
    RebootPending    = $false
    RebootSourcesRaw = ""
    CCMSetupRunning  = $false
}

# ── Cache ──────────────────────────────────────────────────────────────────
try {
    $cache = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" -ClassName "CacheConfig" -ErrorAction Stop
    $cachePath = [string]$cache.Location

    # CacheConfig.Size ist direkt in MB
    $configuredMB = [int]$cache.Size
    $result.CachePath   = $cachePath
    $result.CacheSizeMB = $configuredMB

    # Read actual disk usage directly from the file system (not WMI)
    $usedBytes = 0

    if (Test-Path $cachePath) {
        # Unterordner (klassischer CCM Cache)
        $folders = Get-ChildItem -Path $cachePath -Directory -ErrorAction SilentlyContinue
        foreach ($folder in $folders) {
            $measure = Get-ChildItem -Path $folder.FullName -Recurse -File -ErrorAction SilentlyContinue |
                       Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue
            $folderSize = [double]($measure.Sum)
            if (-not $folderSize) { $folderSize = 0 }
            $usedBytes += $folderSize
        }

        # Files directly in the cache root
        $rootFiles = Get-ChildItem -Path $cachePath -File -ErrorAction SilentlyContinue
        foreach ($file in $rootFiles) {
            $usedBytes += $file.Length
        }

        # Fallback wenn keine Ordner gefunden
        if ($usedBytes -eq 0) {
            $allFiles = Get-ChildItem -Path $cachePath -Recurse -File -ErrorAction SilentlyContinue
            $usedBytes = ($allFiles | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum
            if ($null -eq $usedBytes) { $usedBytes = 0 }
        }
    }

    $usedMB = [math]::Round([double]$usedBytes / 1048576, 0)
    $result.CacheUsedMB = $usedMB
    $result.CacheFreeMB = $configuredMB - $usedMB

} catch {
    $result.CachePath = "Error: $($_.Exception.Message)"
}

# ── Reboot Pending ─────────────────────────────────────────────────────────
$sources = [System.Collections.Generic.List[string]]::new()

if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") {
    $sources.Add("Windows Update (CBS)")
    $result.RebootPending = $true
}
if (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update" `
        -Name "RebootRequired" -ErrorAction SilentlyContinue) {
    $sources.Add("Windows Update (AU)")
    $result.RebootPending = $true
}
try {
    $ccmReboot = Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_ClientUtilities" `
                     -MethodName "DetermineIfRebootPending" -ErrorAction SilentlyContinue
    if ($ccmReboot -and ($ccmReboot.RebootPending -or $ccmReboot.IsHardRebootPending)) {
        $sources.Add("CCM Client")
        $result.RebootPending = $true
    }
} catch {}

# Pipe-getrennt serialisieren — WinRM-sicher, kein verschachteltes Array
$result.RebootSourcesRaw = $sources -join "|"

# ── Is CCMSetup running? ──────────────────────────────────────────────────
$result.CCMSetupRunning = ($null -ne (Get-Process -Name "ccmsetup" -ErrorAction SilentlyContinue))

$result
