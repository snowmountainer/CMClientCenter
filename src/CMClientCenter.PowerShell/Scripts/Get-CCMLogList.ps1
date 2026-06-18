# Get-CCMLogList.ps1 — List available CCM log files

$logPaths = @(
    "$env:WinDir\CCM\Logs",
    "$env:WinDir\ccmsetup\Logs"
)

foreach ($path in $logPaths) {
    if (Test-Path $path) {
        Get-ChildItem -Path $path -Filter "*.log" -ErrorAction SilentlyContinue |
            Sort-Object Name |
            ForEach-Object {
                [PSCustomObject]@{
                    Name     = $_.Name
                    SizeMB   = [math]::Round($_.Length / 1KB, 0)
                    Modified = $_.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
                    Path     = $_.FullName
                    Folder   = Split-Path $path -Leaf
                }
            }
    }
}
