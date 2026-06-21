# Invoke-CCMSoftwareUpdate.ps1 — $UpdateIdToInstall is set by the caller
#
# Installiert ein einzelnes Software Update ueber CCM_SoftwareUpdatesManager.
# Verifizierte Methode/Signatur (Microsoft Learn — InstallUpdates Method in
# Class CCM_SoftwareUpdatesManager):
#   UInt32 InstallUpdates( [IN] CCM_SoftwareUpdate CCMUpdates[] );
#   Rueckgabe: 0 = Erfolg, ungleich 0 = Fehler.
#
# WICHTIG, anders als beim TaskSequence-Pattern (ExecuteProgram nimmt nur
# zwei String-IDs): InstallUpdates erwartet ein ARRAY von CIM-Instanzen,
# keine ID. Die Instanz wird deshalb hier frisch per UpdateID-Filter geholt
# und als Ein-Element-Array uebergeben (Pattern aus mehreren unabhaengigen,
# verifizierten Community-Beispielen, z.B. wetterssource.com/CCM-SoftwareUpdate).
#
# Der Aufrufer (UI) MUSS die UpdateID bereits ueber Get-CCMSoftwareUpdates.ps1
# / InstallableUpdateId bezogen haben — eine UpdateID ohne vorherigen
# Title/Article-Abgleich gegen CCM_SoftwareUpdate existiert nicht zuverlaessig.

$result = [PSCustomObject]@{ Success = $false; Message = "" }
try {
    $instance = @(Get-CimInstance -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_SoftwareUpdate" `
                      -Filter "UpdateID = '$UpdateIdToInstall'" -ErrorAction Stop)

    if ($instance.Count -eq 0) {
        $result.Message = "Update not found on client (UpdateID may be stale — try Refresh)"
    } else {
        $invokeResult = Invoke-CimMethod -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_SoftwareUpdatesManager" `
                            -MethodName "InstallUpdates" -Arguments @{ CCMUpdates = [ciminstance[]]$instance } `
                            -ErrorAction Stop

        if ($invokeResult.ReturnValue -eq 0) {
            $result.Success = $true
            $result.Message = "Update installation started"
        } else {
            $result.Message = "InstallUpdates returned code $($invokeResult.ReturnValue)"
        }
    }
} catch {
    $result.Message = $_.Exception.Message
}
$result
