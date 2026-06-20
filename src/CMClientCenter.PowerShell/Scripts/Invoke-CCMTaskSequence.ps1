# Invoke-CCMTaskSequence.ps1 — $TSProgramID, $TSPackageID are set by the caller
#
# Startet eine Task Sequence (inkl. OSD) ueber die CCM_ProgramsManager-Klasse.
# Verifizierte Methode/Signatur (Microsoft Learn — ExecuteProgram Method in
# Class CCM_ProgramsManager):
#   uint32 ExecuteProgram( [IN] String ProgramID, [IN] String PackageID );
#   Rueckgabe: 0 = Erfolg, ungleich 0 = Fehler.
#
# Bewusst KEINE eigene Sicherheitsabfrage/Warnung hier im Script — das
# High-Impact-Warn-Dialog (mit den ConfigMgr-eigenen CustomHighImpact*-Texten)
# muss bereits in der App-UI bestaetigt worden sein, BEVOR dieses Script
# aufgerufen wird. Dieses Script fuehrt nur aus, was die UI freigegeben hat.

$result = [PSCustomObject]@{ Success = $false; Message = "" }
try {
    $cimClass = Get-CimClass -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_ProgramsManager" -ErrorAction Stop

    $params = @{
        ProgramID = [string]$TSProgramID
        PackageID = [string]$TSPackageID
    }

    $invokeResult = Invoke-CimMethod -CimClass $cimClass -MethodName "ExecuteProgram" -Arguments $params -ErrorAction Stop

    if ($invokeResult.ReturnValue -eq 0) {
        $result.Success = $true
        $result.Message = "Task Sequence started"
    } else {
        $result.Message = "ExecuteProgram returned code $($invokeResult.ReturnValue)"
    }
} catch {
    $result.Message = $_.Exception.Message
}
$result
