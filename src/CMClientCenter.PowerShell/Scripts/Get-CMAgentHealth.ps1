# Get-CMAgentHealth.ps1 — PS 5.1 kompatibel, keine Helper-Funktionen

$checks = [System.Collections.Generic.List[PSCustomObject]]::new()

# ── Service ────────────────────────────────────────────────────────────────
try {
    $svc = Get-Service -Name "CcmExec" -ErrorAction Stop
    $svcStatus = "Healthy"
    if ($svc.Status -ne "Running") { $svcStatus = "Error" }
    $checks.Add([PSCustomObject]@{ Category="Service"; Name="CcmExec"; Status=$svcStatus; Value=$svc.Status.ToString(); Detail="" })
} catch {
    $checks.Add([PSCustomObject]@{ Category="Service"; Name="CcmExec"; Status="Error"; Value="Nicht gefunden"; Detail=$_.Exception.Message })
}

# ── Client Version ─────────────────────────────────────────────────────────
try {
    $sms = Get-CimInstance -Namespace "ROOT\ccm" -ClassName "SMS_Client" -ErrorAction Stop
    $ver = ""
    if ($sms.ClientVersion) { $ver = [string]$sms.ClientVersion }
    $verStatus = "Healthy"
    if (-not $ver) { $verStatus = "Warning" }
    $verVal = $ver
    if (-not $ver) { $verVal = "Unbekannt" }
    $checks.Add([PSCustomObject]@{ Category="Client"; Name="Version"; Status=$verStatus; Value=$verVal; Detail="" })
} catch {
    $checks.Add([PSCustomObject]@{ Category="Client"; Name="Version"; Status="Error"; Value="Kein Zugriff"; Detail=$_.Exception.Message })
}

# ── Site Code + MP ─────────────────────────────────────────────────────────
try {
    $mpInfo = Get-CimInstance -Namespace "ROOT\ccm\locationservices" `
                  -ClassName "SMS_MPInformation" -ErrorAction Stop | Select-Object -First 1
    if ($mpInfo -ne $null) {
        $sc = [string]$mpInfo.SiteCode
        $mp = [string]$mpInfo.MP

        $scStatus = "Healthy"
        $scVal    = $sc
        if (-not $sc) { $scStatus = "Warning"; $scVal = "Nicht zugewiesen" }
        $checks.Add([PSCustomObject]@{ Category="Netzwerk"; Name="Site Code"; Status=$scStatus; Value=$scVal; Detail="" })

        $mpStatus = "Healthy"
        $mpVal    = $mp
        if (-not $mp) { $mpStatus = "Warning"; $mpVal = "Nicht gefunden" }
        $checks.Add([PSCustomObject]@{ Category="Netzwerk"; Name="Management Point"; Status=$mpStatus; Value=$mpVal; Detail="" })

        $lastReqVal = "-"
        if ($mpInfo.MPLastRequestTime -ne $null -and $mpInfo.MPLastRequestTime.Year -gt 1970) {
            $lastReqVal = $mpInfo.MPLastRequestTime.ToString("dd.MM.yyyy HH:mm")
        }
        $checks.Add([PSCustomObject]@{ Category="Netzwerk"; Name="Letzte MP-Anfrage"; Status="Info"; Value=$lastReqVal; Detail="" })
    } else {
        $checks.Add([PSCustomObject]@{ Category="Netzwerk"; Name="Site Code";        Status="Warning"; Value="Kein MP registriert"; Detail="" })
        $checks.Add([PSCustomObject]@{ Category="Netzwerk"; Name="Management Point"; Status="Warning"; Value="Kein MP registriert"; Detail="" })
    }
} catch {
    $checks.Add([PSCustomObject]@{ Category="Netzwerk"; Name="MP / Site Code"; Status="Error"; Value="Kein Zugriff"; Detail=$_.Exception.Message })
}

# ── Cache ──────────────────────────────────────────────────────────────────
try {
    $cache = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" `
                 -ClassName "CacheConfig" -ErrorAction Stop
    $mb = [int]$cache.Size
    $cacheStatus = "Healthy"
    if ($mb -le 0) { $cacheStatus = "Warning" }
    $checks.Add([PSCustomObject]@{ Category="Cache"; Name="Größe"; Status=$cacheStatus; Value="$mb MB"; Detail="" })
    $checks.Add([PSCustomObject]@{ Category="Cache"; Name="Pfad";  Status="Info"; Value=[string]$cache.Location; Detail="" })
} catch {
    $checks.Add([PSCustomObject]@{ Category="Cache"; Name="Cache"; Status="Error"; Value="Kein Zugriff"; Detail=$_.Exception.Message })
}

# ── Inventar ───────────────────────────────────────────────────────────────
try {
    $statuses = Get-CimInstance -Namespace "ROOT\ccm\invagt" `
                    -ClassName "InventoryActionStatus" -ErrorAction Stop

    # Hardware Inventory {0001}
    $hw = $statuses | Where-Object { $_.InventoryActionID -eq "{00000000-0000-0000-0000-000000000001}" } | Select-Object -First 1
    if ($hw -ne $null -and $hw.LastReportDate.Year -gt 1970) {
        $days = ([datetime]::Now - $hw.LastReportDate).Days
        $hwStatus = "Healthy"
        if ($days -gt 14) { $hwStatus = "Error" } elseif ($days -gt 7) { $hwStatus = "Warning" }
        $checks.Add([PSCustomObject]@{ Category="Inventar"; Name="Hardware Inventar"; Status=$hwStatus; Value=$hw.LastReportDate.ToString("dd.MM.yyyy HH:mm"); Detail="vor $days Tag(en)" })
    } else {
        $checks.Add([PSCustomObject]@{ Category="Inventar"; Name="Hardware Inventar"; Status="Warning"; Value="Noch nie ausgeführt"; Detail="" })
    }

    # Software Inventory {0002} — 1970 = nicht konfiguriert = Info
    $sw = $statuses | Where-Object { $_.InventoryActionID -eq "{00000000-0000-0000-0000-000000000002}" } | Select-Object -First 1
    if ($sw -ne $null -and $sw.LastReportDate.Year -gt 1970) {
        $days = ([datetime]::Now - $sw.LastReportDate).Days
        $swStatus = "Healthy"
        if ($days -gt 14) { $swStatus = "Error" } elseif ($days -gt 7) { $swStatus = "Warning" }
        $checks.Add([PSCustomObject]@{ Category="Inventar"; Name="Software Inventar"; Status=$swStatus; Value=$sw.LastReportDate.ToString("dd.MM.yyyy HH:mm"); Detail="vor $days Tag(en)" })
    } else {
        $checks.Add([PSCustomObject]@{ Category="Inventar"; Name="Software Inventar"; Status="Info"; Value="Nicht konfiguriert"; Detail="" })
    }

    # Discovery Data {0003}
    $dd = $statuses | Where-Object { $_.InventoryActionID -eq "{00000000-0000-0000-0000-000000000003}" } | Select-Object -First 1
    if ($dd -ne $null -and $dd.LastReportDate.Year -gt 1970) {
        $days = ([datetime]::Now - $dd.LastReportDate).Days
        $ddStatus = "Healthy"
        if ($days -gt 14) { $ddStatus = "Error" } elseif ($days -gt 7) { $ddStatus = "Warning" }
        $checks.Add([PSCustomObject]@{ Category="Inventar"; Name="Discovery Data"; Status=$ddStatus; Value=$dd.LastReportDate.ToString("dd.MM.yyyy HH:mm"); Detail="vor $days Tag(en)" })
    }
} catch {
    $checks.Add([PSCustomObject]@{ Category="Inventar"; Name="Inventar-Status"; Status="Error"; Value="Kein Zugriff"; Detail=$_.Exception.Message })
}

# ── Software Updates ───────────────────────────────────────────────────────
try {
    $updates = Get-CimInstance -Namespace "ROOT\ccm\clientsdk" `
                   -ClassName "CCM_SoftwareUpdate" -ErrorAction SilentlyContinue
    if ($updates -ne $null) {
        $all     = @($updates)
        $total   = $all.Count
        $pending = ($all | Where-Object { $_.ComplianceState -eq 0 }).Count
        $upStatus = "Healthy"
        if ($pending -gt 5)  { $upStatus = "Error" }
        elseif ($pending -gt 0) { $upStatus = "Warning" }
        $upDetail = "Alles aktuell"
        if ($pending -gt 0) { $upDetail = "$pending Update(s) verfügbar" }
        $checks.Add([PSCustomObject]@{ Category="Updates"; Name="Ausstehende Updates"; Status=$upStatus; Value="$pending / $total"; Detail=$upDetail })
    } else {
        $checks.Add([PSCustomObject]@{ Category="Updates"; Name="Ausstehende Updates"; Status="Info"; Value="Keine Daten"; Detail="" })
    }
} catch {
    $checks.Add([PSCustomObject]@{ Category="Updates"; Name="Software Updates"; Status="Info"; Value="Nicht konfiguriert"; Detail="" })
}

# ── Neustart ───────────────────────────────────────────────────────────────
try {
    $reboot = $false
    if (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing" -Name "RebootPending" -ErrorAction SilentlyContinue) { $reboot = $true }
    if (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update" -Name "RebootRequired" -ErrorAction SilentlyContinue) { $reboot = $true }
    $rebootStatus = "Healthy"
    $rebootVal    = "Nein"
    if ($reboot) { $rebootStatus = "Warning"; $rebootVal = "Ja" }
    $checks.Add([PSCustomObject]@{ Category="System"; Name="Neustart ausstehend"; Status=$rebootStatus; Value=$rebootVal; Detail="" })
} catch {}

# ── Systemdisk ─────────────────────────────────────────────────────────────
try {
    $drive  = $env:SystemDrive
    $disk   = Get-CimInstance -ClassName Win32_LogicalDisk -Filter "DeviceID='$drive'" -ErrorAction SilentlyContinue
    if ($disk -ne $null) {
        $freeGB  = [math]::Round($disk.FreeSpace / 1GB, 1)
        $totalGB = [math]::Round($disk.Size / 1GB, 0)
        $pct     = [math]::Round($freeGB / $totalGB * 100, 0)
        $dStatus = "Healthy"
        if ($pct -lt 10) { $dStatus = "Error" } elseif ($pct -lt 20) { $dStatus = "Warning" }
        $checks.Add([PSCustomObject]@{ Category="System"; Name="Systemdisk ($drive)"; Status=$dStatus; Value="$freeGB GB frei ($pct%)"; Detail="von $totalGB GB" })
    }
} catch {}

$checks.ToArray()
