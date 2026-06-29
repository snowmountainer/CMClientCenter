#Requires -Version 5.1
<#
.SYNOPSIS
    Notifies the logged-on user of pending software updates and the
    number of days left before they install automatically.

.DESCRIPTION
    Same notification approach as Notify-UserOfReboot.ps1 in this folder —
    native toast notification, with msg * as a fallback for when this runs
    as SYSTEM (toast notifications require the user's own session) or any
    other reason the toast call fails. The original version also had
    '$updates -ne $null' the wrong way round (PowerShell style is
    '$null -ne $updates'; functionally identical here, but fixed for
    consistency with the rest of this library).
#>

$updates = Get-WmiObject -Namespace 'ROOT\ccm\ClientSDK' -Class CCM_SoftwareUpdate

if ($null -eq $updates) {
    Write-Output 'No pending updates — nothing to notify the user about.'
    return
}

$deadline = $updates.Deadline | Select-Object -First 1
$deadlineUtc = [System.Management.ManagementDateTimeConverter]::ToDateTime($deadline)
$daysRemaining = [Math]::Max(0, [Math]::Ceiling((New-TimeSpan -Start (Get-Date) -End $deadlineUtc).TotalDays))

$title = 'Pending Updates'
$message = "You have software updates available to install. They will install automatically in $daysRemaining day(s)."

try {
    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
    [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

    $toastXml = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
    $textNodes = $toastXml.GetElementsByTagName('text')
    $textNodes.Item(0).AppendChild($toastXml.CreateTextNode($title)) | Out-Null
    $textNodes.Item(1).AppendChild($toastXml.CreateTextNode($message)) | Out-Null

    $toast = [Windows.UI.Notifications.ToastNotification]::new($toastXml)
    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('CMClientCenter').Show($toast)

    Write-Output 'Pending-updates notification toast sent.'
} catch {
    Write-Warning "Toast notification failed ($($_.Exception.Message)) — falling back to msg *."
    msg * "$title : $message"
}
