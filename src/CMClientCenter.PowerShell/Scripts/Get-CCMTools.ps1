# Get-CCMTools.ps1 — PS 5.1 kompatibel

$result = [PSCustomObject]@{
    CacheSizeMB     = 0
    CacheUsedMB     = 0
    CacheFreeMB     = 0
    CachePath       = ""
    CacheItems      = @()
    RebootPending   = $false
    RebootSources   = @()
    Applications    = @()
    CCMSetupRunning = $false
}

# ── Cache ──────────────────────────────────────────────────────────────────
try {
    $cache = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" -ClassName "CacheConfig" -ErrorAction Stop
    $cachePath = [string]$cache.Location

    # CacheConfig.Size ist direkt in MB
    $configuredMB = [int]$cache.Size
    $result.CachePath    = $cachePath
    $result.CacheSizeMB  = $configuredMB

    # Tatsächliche Disk-Nutzung direkt aus dem Dateisystem lesen (nicht WMI)
    $usedBytes = 0
    $cacheItems = [System.Collections.Generic.List[PSCustomObject]]::new()

    if (Test-Path $cachePath) {
        # Unterordner (klassischer CCM Cache)
        $folders = Get-ChildItem -Path $cachePath -Directory -ErrorAction SilentlyContinue
        foreach ($folder in $folders) {
            $measure = Get-ChildItem -Path $folder.FullName -Recurse -File -ErrorAction SilentlyContinue |
                       Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue
            # Explizit als Double casten - WinRM gibt PSObject zurück
            $folderSize = [double]($measure.Sum)
            if (-not $folderSize) { $folderSize = 0 }
            $usedBytes += $folderSize
            $lastWrite = $folder.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
            $sizeMB = 0
            if ($folderSize -gt 0) { $sizeMB = [math]::Round([double]$folderSize / 1048576, 1) }
            $cacheItems.Add([PSCustomObject]@{
                ContentId   = $folder.Name
                ContentVer  = ""
                Location    = $folder.FullName
                SizeMB      = $sizeMB
                LastRefTime = $lastWrite
            })
        }

        # Dateien direkt im Cache-Root (z.B. nach manuellem Löschen der Unterordner)
        $rootFiles = Get-ChildItem -Path $cachePath -File -ErrorAction SilentlyContinue
        foreach ($file in $rootFiles) {
            $usedBytes += $file.Length
        }

        # Gesamte Disk-Nutzung als Fallback wenn keine Ordner gefunden
        if ($usedBytes -eq 0) {
            $allFiles = Get-ChildItem -Path $cachePath -Recurse -File -ErrorAction SilentlyContinue
            $usedBytes = ($allFiles | Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum
            if ($usedBytes -eq $null) { $usedBytes = 0 }
        }
    }

    $usedMB = [math]::Round([double]$usedBytes / 1048576, 0)
    $result.CacheUsedMB = $usedMB
    $result.CacheFreeMB = $configuredMB - $usedMB
    $result.CacheItems  = $cacheItems.ToArray()

} catch {
    $result.CachePath = "Fehler: $($_.Exception.Message)"
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

$result.RebootSources = $sources.ToArray()

# ── Applications ───────────────────────────────────────────────────────────
try {
    $apps = Get-CimInstance -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -ErrorAction SilentlyContinue
    if ($apps -ne $null) {
        $result.Applications = @($apps) | ForEach-Object {
            [PSCustomObject]@{
                Id              = [string]$_.Id
                Revision        = [string]$_.Revision
                Name            = [string]$_.Name
                Publisher       = [string]$_.Publisher
                SoftwareVersion = [string]$_.SoftwareVersion
                InstallState    = [string]$_.InstallState
                ResolvedState   = [string]$_.ResolvedState
            }
        } | Sort-Object Name
    }
} catch {}

# ── CCMSetup läuft? ────────────────────────────────────────────────────────
$result.CCMSetupRunning = ($null -ne (Get-Process -Name "ccmsetup" -ErrorAction SilentlyContinue))

$result
