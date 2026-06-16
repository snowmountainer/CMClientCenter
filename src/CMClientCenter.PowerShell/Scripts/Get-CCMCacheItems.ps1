# Get-CCMCacheItems.ps1 — PS 5.1 kompatibel
# Gibt CacheItems als flache Liste zurück — verschachtelte Arrays über WinRM unzuverlässig

try {
    $cache = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" -ClassName "CacheConfig" -ErrorAction Stop
    $cachePath = [string]$cache.Location
} catch {
    return
}

if (-not (Test-Path $cachePath)) { return }

$folders = Get-ChildItem -Path $cachePath -Directory -ErrorAction SilentlyContinue
foreach ($folder in $folders) {
    $measure = Get-ChildItem -Path $folder.FullName -Recurse -File -ErrorAction SilentlyContinue |
               Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue
    $folderSize = [double]($measure.Sum)
    if (-not $folderSize) { $folderSize = 0 }
    $sizeMB = if ($folderSize -gt 0) { [math]::Round($folderSize / 1048576, 1) } else { 0 }

    [PSCustomObject]@{
        ContentId   = $folder.Name
        ContentVer  = ""
        Location    = $folder.FullName
        SizeMB      = [int][math]::Round($sizeMB, 0)
        LastRefTime = $folder.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
    }
}
