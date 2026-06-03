# Invoke-CCMTool.ps1 — $ToolAction wird vor Script gesetzt
$result = [PSCustomObject]@{ Success=$false; Message="" }
switch ($ToolAction) {
    "ClearCache" {
        try {
            $items = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" -ClassName "CacheInfoEx" -ErrorAction Stop
            $count = 0
            foreach ($item in $items) {
                if ($item.Location -and (Test-Path $item.Location)) {
                    Remove-Item -Path $item.Location -Recurse -Force -ErrorAction SilentlyContinue; $count++
                }
            }
            $result.Success=$true; $result.Message="$count Cache-Einträge gelöscht"
        } catch { $result.Message="Fehler: $($_.Exception.Message)" }
    }
    "RepairClient" {
        try {
            $exe = "$env:WinDir\CCM\ccmrepair.exe"
            if (Test-Path $exe) { Start-Process -FilePath $exe -NoNewWindow; $result.Success=$true; $result.Message="ccmrepair.exe gestartet" }
            else { $result.Message="ccmrepair.exe nicht gefunden" }
        } catch { $result.Message=$_.Exception.Message }
    }
    "ReinstallClient" {
        try {
            $exe = "$env:WinDir\ccmsetup\ccmsetup.exe"
            if (Test-Path $exe) { Start-Process -FilePath $exe -NoNewWindow; $result.Success=$true; $result.Message="ccmsetup.exe gestartet" }
            else { $result.Message="ccmsetup.exe nicht gefunden" }
        } catch { $result.Message=$_.Exception.Message }
    }
    "RebootNow"    { shutdown.exe /r /t 30 /c "CMClientCenter Neustart"; $result.Success=$true; $result.Message="Neustart in 30s" }
    "CancelReboot" { shutdown.exe /a; $result.Success=$true; $result.Message="Neustart abgebrochen" }
    default        { $result.Message="Unbekannte Aktion: $ToolAction" }
}
$result
