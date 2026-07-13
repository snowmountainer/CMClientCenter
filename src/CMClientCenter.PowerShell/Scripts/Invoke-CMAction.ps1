# Invoke-CMAction.ps1
# Compatible with PS 5.1 and PS 7+
# $ScheduleId is set as a variable by C# before the script runs

try {
    # Invoke-CimMethod — PS 5.1 and PS 7 compatible
    $cimResult = Invoke-CimMethod -Namespace "ROOT\ccm" `
                     -ClassName "SMS_Client" `
                     -MethodName "TriggerSchedule" `
                     -Arguments @{ sScheduleID = $ScheduleId } `
                     -ErrorAction Stop

    [PSCustomObject]@{
        Success     = $true
        ReturnValue = [int]$cimResult.ReturnValue
        Message     = "OK (ReturnValue=$($cimResult.ReturnValue))"
    }
}
catch {
    # Fallback: WMI (native on PS 5.1, deprecated but still available on PS 7)
    try {
        $sms = [wmiclass]"ROOT\ccm:SMS_Client"
        $sms.TriggerSchedule($ScheduleId) | Out-Null
        [PSCustomObject]@{
            Success     = $true
            ReturnValue = 0
            Message     = "OK via WMI"
        }
    }
    catch {
        # Append the HResult as a fixed hex code so callers can match on it
        # regardless of the OS UI language (the exception text itself is
        # localized by Windows, e.g. "Not found" vs. "Nicht gefunden").
        $hresult = "0x{0:X8}" -f $_.Exception.HResult
        [PSCustomObject]@{
            Success     = $false
            ReturnValue = -1
            Message     = "$($_.Exception.Message) ($hresult)"
        }
    }
}
