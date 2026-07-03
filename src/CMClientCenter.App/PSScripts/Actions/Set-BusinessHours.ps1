#Requires -Version 5.1
<#
.SYNOPSIS
    Sets the ConfigMgr client's "business hours" window so that MECM
    avoids forced reboots and mandatory deployment windows during those
    hours.

.DESCRIPTION
    Defaults to 08:00-17:00, Monday-Friday (WorkingDays bitmask 62 =
    Mon 2 + Tue 4 + Wed 8 + Thu 16 + Fri 32). Pass -StartHour, -EndHour,
    and/or -WorkingDaysMask to override without editing the script.

    WorkingDays bitmask (additive):
      Sun=1  Mon=2  Tue=4  Wed=8  Thu=16  Fri=32  Sat=64
      Mon-Fri = 62, Mon-Sat = 126, Every day = 127
#>

param(
    [ValidateRange(0, 23)] [int] $StartHour      = 8,
    [ValidateRange(1, 23)] [int] $EndHour        = 17,
    [ValidateRange(1, 127)][int] $WorkingDaysMask = 62
)

$uxSettings   = [WmiClass]'\\.\ROOT\ccm\ClientSDK:CCM_ClientUXSettings'
$methodParams = $uxSettings.PSBase.GetMethodParameters('SetBusinessHours')
$methodParams.StartTime   = $StartHour
$methodParams.EndTime     = $EndHour
$methodParams.WorkingDays = $WorkingDaysMask

try {
    $result = $uxSettings.PSBase.InvokeMethod('SetBusinessHours', $methodParams, $null)
    if ($result.ReturnValue -eq 0) {
        Write-Output "Business hours set: $StartHour:00-$EndHour:00, WorkingDays mask $WorkingDaysMask."
    } else {
        Write-Warning "SetBusinessHours returned error code $($result.ReturnValue)."
    }
} catch {
    Write-Warning "Failed to set business hours: $($_.Exception.Message)"
}
