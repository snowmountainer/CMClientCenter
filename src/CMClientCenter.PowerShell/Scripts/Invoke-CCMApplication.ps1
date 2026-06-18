# Invoke-CCMApplication.ps1 — $AppId, $AppRevision, $AppAction are set by the caller
$result = [PSCustomObject]@{ Success=$false; Message="" }
try {
    $params = @{
        Id                = [string]$AppId
        Revision          = [string]$AppRevision
        IsMachineTarget   = [bool]$true
        EnforcePreference = [uint32]0
        Priority          = [string]"High"
        IsRebootIfNeeded  = [bool]$false
    }
    switch ($AppAction) {
        "Install"   { Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -MethodName "Install"   -Arguments $params -ErrorAction Stop | Out-Null; $result.Success=$true; $result.Message="Installation started" }
        "Repair"    { Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -MethodName "Repair"    -Arguments $params -ErrorAction Stop | Out-Null; $result.Success=$true; $result.Message="Repair started" }
        "Uninstall" { Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -MethodName "Uninstall" -Arguments $params -ErrorAction Stop | Out-Null; $result.Success=$true; $result.Message="Uninstallation started" }
        default     { $result.Message="Unknown action: $AppAction" }
    }
} catch { $result.Message=$_.Exception.Message }
$result
