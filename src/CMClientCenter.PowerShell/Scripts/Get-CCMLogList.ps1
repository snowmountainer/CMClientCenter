# Get-CCMLogList.ps1 — List available CCM + PSADT log files
# PS 5.1 compatible
# Source: CCM = WinDir\CCM\Logs, CCMSetup = WinDir\ccmsetup\Logs, PSADT = WinDir\Logs\Software (flat, no recursion)

$logSources = @(
    @{ Path = "$env:WinDir\CCM\Logs";      Source = "CCM";      Folder = "CCM" }
    @{ Path = "$env:WinDir\ccmsetup\Logs"; Source = "CCMSetup"; Folder = "CCMSetup" }
    @{ Path = "$env:WinDir\Logs\Software"; Source = "PSADT";    Folder = "PSADT" }
)

foreach ($src in $logSources) {
    if (Test-Path $src.Path) {
        Get-ChildItem -Path $src.Path -Filter "*.log" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            ForEach-Object {
                [PSCustomObject]@{
                    Name     = $_.Name
                    SizeMB   = [math]::Round($_.Length / 1KB, 0)
                    Modified = $_.LastWriteTime.ToString("dd.MM.yyyy HH:mm")
                    Path     = $_.FullName
                    Folder   = $src.Folder
                    Source   = $src.Source
                }
            }
    }
}
