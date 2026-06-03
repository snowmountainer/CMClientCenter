# Invoke-CCMApplication.ps1 — $AppId, $AppRevision, $AppAction werden gesetzt
$result = [PSCustomObject]@{ Success=$false; Message="" }
try {
    $params = @{ Id=$AppId; Revision=$AppRevision; IsMachineTarget=$true; EnforcePreference=0; Priority="High"; IsRebootIfNeeded=$false }
    switch ($AppAction) {
        "Install"   { Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -MethodName "Install"   -Arguments $params -ErrorAction Stop | Out-Null; $result.Success=$true; $result.Message="Installation gestartet" }
        "Repair"    { Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -MethodName "Repair"    -Arguments $params -ErrorAction Stop | Out-Null; $result.Success=$true; $result.Message="Reparatur gestartet" }
        "Uninstall" { Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -MethodName "Uninstall" -Arguments $params -ErrorAction Stop | Out-Null; $result.Success=$true; $result.Message="Deinstallation gestartet" }
        default     { $result.Message="Unbekannte Aktion: $AppAction" }
    }
} catch { $result.Message=$_.Exception.Message }
$result
