$HardwareInventoryID = '{00000000-0000-0000-0000-000000000001}'
Get-WmiObject -Namespace 'Root\CCM\INVAGT' -Class 'InventoryActionStatus' -Filter "InventoryActionID='$HardwareInventoryID'" | Remove-WmiObject
$HardwareInventoryID = '{00000000-0000-0000-0000-000000000002}'
Get-WmiObject -Namespace 'Root\CCM\INVAGT' -Class 'InventoryActionStatus' -Filter "InventoryActionID='$HardwareInventoryID'" | Remove-WmiObject
$HardwareInventoryID = '{00000000-0000-0000-0000-000000000003}'
Get-WmiObject -Namespace 'Root\CCM\INVAGT' -Class 'InventoryActionStatus' -Filter "InventoryActionID='$HardwareInventoryID'" | Remove-WmiObject
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000001}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000002}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000003}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').ResetPolicy(1) | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000040}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000021}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000022}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000108}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000113}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000112}') | Out-Null
([wmiclass]'ROOT\ccm:SMS_Client').TriggerSchedule('{00000000-0000-0000-0000-000000000111}') | Out-Null
(New-Object -ComObject Microsoft.CCM.UpdatesStore).RefreshServerComplianceState()
$ccmEvalProcess = Start-Process -FilePath 'C:\Windows\CCM\ccmeval.exe' -PassThru
"ccmeval.exe started (PID $($ccmEvalProcess.Id))"
