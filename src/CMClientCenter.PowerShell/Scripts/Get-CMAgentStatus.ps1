# Get-CMAgentStatus.ps1 — PS 5.1 kompatibel

$result = [PSCustomObject]@{
    ClientVersion     = ""
    ClientId          = ""
    ClientState       = "NotInstalled"
    IsEnabled         = $false
    SiteCode          = ""
    ManagementPoint   = ""
    CacheSize         = "-"
    LastHWInventory   = $null
    LastSWInventory   = $null
    LastPolicyRequest = $null
    DiagInfo          = ""
}

function Get-ValidDate { param($d); if ($d -eq $null -or $d.Year -le 1970) { return $null }; return $d }

# Client ID
try {
    $ccm = Get-CimInstance -Namespace "ROOT\ccm" -ClassName "CCM_Client" -ErrorAction Stop
    $p   = $ccm.CimInstanceProperties["ClientId"]
    if ($p -ne $null) { $result.ClientId = [string]$p.Value }
    $result.IsEnabled = $true
} catch {}

# Version (SMS_Client.AssignedSite ist auf diesem System leer — SiteCode kommt aus MPInfo)
try {
    $sms = Get-CimInstance -Namespace "ROOT\ccm" -ClassName "SMS_Client" -ErrorAction Stop
    if ($sms.ClientVersion) { $result.ClientVersion = [string]$sms.ClientVersion }
    if ($sms.AssignedSite)  { $result.SiteCode      = [string]$sms.AssignedSite }
    $result.ClientState = if ($result.ClientVersion) { "Healthy" } else { "Unknown" }
} catch {}

# MP + SiteCode aus SMS_MPInformation (primäre Quelle für Site Code auf diesem System)
try {
    $mpInfo = Get-CimInstance -Namespace "ROOT\ccm\locationservices" `
                  -ClassName "SMS_MPInformation" -ErrorAction Stop | Select-Object -First 1
    if ($mpInfo -ne $null) {
        $result.ManagementPoint = [string]$mpInfo.MP
        # SiteCode aus MPInfo wenn SMS_Client.AssignedSite leer
        if (-not $result.SiteCode -and $mpInfo.SiteCode) {
            $result.SiteCode = [string]$mpInfo.SiteCode
        }
    }
} catch {}

# Cache
try {
    $cache = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" `
                 -ClassName "CacheConfig" -ErrorAction SilentlyContinue
    if ($cache -ne $null) { $result.CacheSize = "$([math]::Round($cache.Size / 1024, 0)) MB" }
} catch {}

# Inventory Timestamps (Epoch 1970 = nie ausgeführt)
try {
    $statuses = Get-CimInstance -Namespace "ROOT\ccm\invagt" `
                    -ClassName "InventoryActionStatus" -ErrorAction SilentlyContinue
    $hw = $statuses | Where-Object { $_.InventoryActionID -eq "{00000000-0000-0000-0000-000000000001}" } |
          Select-Object -First 1
    if ($hw -ne $null) { $result.LastHWInventory = Get-ValidDate $hw.LastReportDate }

    $sw = $statuses | Where-Object { $_.InventoryActionID -eq "{00000000-0000-0000-0000-000000000002}" } |
          Select-Object -First 1
    if ($sw -ne $null) { $result.LastSWInventory = Get-ValidDate $sw.LastReportDate }
} catch {}

$result
