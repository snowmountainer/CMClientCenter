# Get-CCMTools.ps1 — PS 5.1 kompatibel
$result = [PSCustomObject]@{
    CacheSize=0; CacheUsedMB=0; CacheFreeMB=0; CachePath=""
    CacheItems=@(); RebootPending=$false; RebootSources=@()
    Applications=@(); CCMSetupRunning=$false
}

try {
    $cache = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" -ClassName "CacheConfig" -ErrorAction Stop
    $result.CacheSize = [math]::Round($cache.Size / 1024, 0)
    $result.CachePath = [string]$cache.Location
    $items = Get-CimInstance -Namespace "ROOT\ccm\SoftMgmtAgent" -ClassName "CacheInfoEx" -ErrorAction SilentlyContinue
    if ($items -ne $null) {
        $usedKB = ($items | Measure-Object -Property ContentSize -Sum).Sum
        $result.CacheUsedMB = [math]::Round($usedKB / 1024, 0)
        $result.CacheFreeMB = $result.CacheSize - $result.CacheUsedMB
        $result.CacheItems = $items | ForEach-Object {
            [PSCustomObject]@{
                ContentId=[string]$_.ContentId; ContentVer=[string]$_.ContentVersion
                Location=[string]$_.Location; SizeMB=[math]::Round($_.ContentSize/1024,0)
                LastRefTime=if($_.LastReferenced){$_.LastReferenced.ToString("dd.MM.yyyy HH:mm")}else{""}
            }
        }
    }
} catch {}

$sources = [System.Collections.Generic.List[string]]::new()
if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") { $sources.Add("CBS"); $result.RebootPending=$true }
if (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update" -Name "RebootRequired" -ErrorAction SilentlyContinue) { $sources.Add("WU"); $result.RebootPending=$true }
try {
    $r = Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_ClientUtilities" -MethodName "DetermineIfRebootPending" -ErrorAction SilentlyContinue
    if ($r -and ($r.RebootPending -or $r.IsHardRebootPending)) { $sources.Add("CCM"); $result.RebootPending=$true }
} catch {}
$result.RebootSources = $sources.ToArray()

try {
    $apps = Get-CimInstance -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -ErrorAction SilentlyContinue
    if ($apps -ne $null) {
        $result.Applications = $apps | ForEach-Object {
            [PSCustomObject]@{
                Id=[string]$_.Id; Revision=[string]$_.Revision; Name=[string]$_.Name
                Publisher=[string]$_.Publisher; SoftwareVersion=[string]$_.SoftwareVersion
                InstallState=[string]$_.InstallState; ResolvedState=[string]$_.ResolvedState
            }
        } | Sort-Object Name
    }
} catch {}

$result.CCMSetupRunning = (Get-Process -Name "ccmsetup" -ErrorAction SilentlyContinue) -ne $null
$result
