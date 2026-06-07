# Invoke-CCMTool.ps1 — PS 5.1 kompatibel
# $ToolAction wird vor dem Script gesetzt

$result = [PSCustomObject]@{ Success = $false; Message = "" }

switch ($ToolAction) {

    "ClearCache" {
        try {
            # CcmExec stoppen damit keine Dateien gesperrt sind
            $svc = Get-Service -Name "CcmExec" -ErrorAction SilentlyContinue
            $wasRunning = ($svc -ne $null -and $svc.Status -eq "Running")

            if ($wasRunning) {
                Stop-Service -Name "CcmExec" -Force -ErrorAction Stop
                Start-Sleep -Seconds 3
            }

            # Cache-Pfad aus WMI lesen
            $cacheConfig = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" `
                               -ClassName "CacheConfig" -ErrorAction Stop
            $cachePath = [string]$cacheConfig.Location

            if (-not $cachePath -or -not (Test-Path $cachePath)) {
                $cachePath = "$env:WinDir\ccmcache"
            }

            # Alle Unterordner löschen (nicht den Cache-Ordner selbst)
            $folders = Get-ChildItem -Path $cachePath -Directory -ErrorAction SilentlyContinue
            $count   = 0
            foreach ($folder in $folders) {
                try {
                    Remove-Item -Path $folder.FullName -Recurse -Force -ErrorAction SilentlyContinue
                    $count++
                } catch {}
            }

            # CcmExec wieder starten
            if ($wasRunning) {
                Start-Service -Name "CcmExec" -ErrorAction SilentlyContinue
            }

            $result.Success = $true
            $result.Message = "$count Cache-Ordner gelöscht. CCM-Service neu gestartet."
        } catch {
            # Sicherstellen dass CcmExec wieder läuft
            try { Start-Service -Name "CcmExec" -ErrorAction SilentlyContinue } catch {}
            $result.Message = "Cache leeren fehlgeschlagen: $($_.Exception.Message)"
        }
    }

    "RepairClient" {
        try {
            $ccmRepair = "$env:WinDir\CCM\ccmrepair.exe"
            if (Test-Path $ccmRepair) {
                Start-Process -FilePath $ccmRepair -NoNewWindow
                $result.Success = $true
                $result.Message = "ccmrepair.exe gestartet"
            } else {
                $result.Message = "ccmrepair.exe nicht gefunden: $ccmRepair"
            }
        } catch {
            $result.Message = "Client Repair fehlgeschlagen: $($_.Exception.Message)"
        }
    }

    "ReinstallClient" {
        try {
            $ccmSetup = "$env:WinDir\ccmsetup\ccmsetup.exe"
            if (Test-Path $ccmSetup) {
                Start-Process -FilePath $ccmSetup -NoNewWindow
                $result.Success = $true
                $result.Message = "ccmsetup.exe gestartet"
            } else {
                $result.Message = "ccmsetup.exe nicht gefunden"
            }
        } catch {
            $result.Message = "Client Reinstall fehlgeschlagen: $($_.Exception.Message)"
        }
    }

    "RebootNow" {
        try {
            Start-Process -FilePath "shutdown.exe" `
                -ArgumentList "/r /t 30 /c `"CMClientCenter: Neustart ausgeloest`"" `
                -NoNewWindow
            $result.Success = $true
            $result.Message = "Neustart in 30 Sekunden eingeplant"
        } catch {
            $result.Message = "Neustart fehlgeschlagen: $($_.Exception.Message)"
        }
    }

    "CancelReboot" {
        try {
            Start-Process -FilePath "shutdown.exe" -ArgumentList "/a" -NoNewWindow
            $result.Success = $true
            $result.Message = "Geplanter Neustart abgebrochen"
        } catch {
            $result.Message = "Neustart abbrechen fehlgeschlagen: $($_.Exception.Message)"
        }
    }

    default {
        $result.Message = "Unbekannte Aktion: $ToolAction"
    }
}

$result
