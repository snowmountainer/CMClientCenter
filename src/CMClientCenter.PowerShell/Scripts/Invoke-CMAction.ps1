# Invoke-CMAction.ps1
# Kompatibel mit PS 5.1 und PS 7+
# $ScheduleId wird vor dem Script per C# als Variable gesetzt

try {
    # Invoke-CimMethod — PS 5.1 und PS 7 kompatibel
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
    # Fallback: WMI (PS 5.1 nativ, PS 7 deprecated aber noch verfügbar)
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
        [PSCustomObject]@{
            Success     = $false
            ReturnValue = -1
            Message     = $_.Exception.Message
        }
    }
}
