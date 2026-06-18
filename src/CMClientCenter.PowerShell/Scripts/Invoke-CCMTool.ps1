# Invoke-CCMTool.ps1 — PS 5.1 compatible
# $ToolAction is set by the caller before the script runs

$result = [PSCustomObject]@{ Success = $false; Message = "" }

switch ($ToolAction) {

    "ClearCache" {
        try {
            # Stop CcmExec so no files are locked
            $svc = Get-Service -Name "CcmExec" -ErrorAction SilentlyContinue
            $wasRunning = ($svc -ne $null -and $svc.Status -eq "Running")

            if ($wasRunning) {
                Stop-Service -Name "CcmExec" -Force -ErrorAction Stop
                Start-Sleep -Seconds 3
            }

            # Read cache path from WMI
            $cacheConfig = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" `
                               -ClassName "CacheConfig" -ErrorAction Stop
            $cachePath = [string]$cacheConfig.Location

            if (-not $cachePath -or -not (Test-Path $cachePath)) {
                $cachePath = "$env:WinDir\ccmcache"
            }

            # Delete all subfolders (not the cache folder itself)
            $folders = Get-ChildItem -Path $cachePath -Directory -ErrorAction SilentlyContinue
            $count   = 0
            foreach ($folder in $folders) {
                try {
                    Remove-Item -Path $folder.FullName -Recurse -Force -ErrorAction SilentlyContinue
                    $count++
                } catch {}
            }

            # Restart CcmExec
            if ($wasRunning) {
                Start-Service -Name "CcmExec" -ErrorAction SilentlyContinue
            }

            $result.Success = $true
            $result.Message = "$count cache folder(s) deleted. CCM service restarted."
        } catch {
            # Ensure CcmExec is running again
            try { Start-Service -Name "CcmExec" -ErrorAction SilentlyContinue } catch {}
            $result.Message = "Clear cache failed: $($_.Exception.Message)"
        }
    }

    "RepairClient" {
        try {
            $ccmRepair = "$env:WinDir\CCM\ccmrepair.exe"
            if (Test-Path $ccmRepair) {
                Start-Process -FilePath $ccmRepair -NoNewWindow
                $result.Success = $true
                $result.Message = "ccmrepair.exe started"
            } else {
                $result.Message = "ccmrepair.exe not found: $ccmRepair"
            }
        } catch {
            $result.Message = "Client repair failed: $($_.Exception.Message)"
        }
    }

    "ReinstallClient" {
        try {
            $ccmSetup = "$env:WinDir\ccmsetup\ccmsetup.exe"
            if (Test-Path $ccmSetup) {
                Start-Process -FilePath $ccmSetup -NoNewWindow
                $result.Success = $true
                $result.Message = "ccmsetup.exe started"
            } else {
                $result.Message = "ccmsetup.exe not found"
            }
        } catch {
            $result.Message = "Client reinstall failed: $($_.Exception.Message)"
        }
    }

    "RebootNow" {
        try {
            Start-Process -FilePath "shutdown.exe" `
                -ArgumentList "/r /t 30 /c `"CMClientCenter: Reboot triggered`"" `
                -NoNewWindow
            $result.Success = $true
            $result.Message = "Reboot scheduled in 30 seconds"
        } catch {
            $result.Message = "Reboot failed: $($_.Exception.Message)"
        }
    }

    "CancelReboot" {
        try {
            Start-Process -FilePath "shutdown.exe" -ArgumentList "/a" -NoNewWindow
            $result.Success = $true
            $result.Message = "Scheduled reboot cancelled"
        } catch {
            $result.Message = "Cancel reboot failed: $($_.Exception.Message)"
        }
    }

    default {
        $result.Message = "Unknown action: $ToolAction"
    }
}

$result
