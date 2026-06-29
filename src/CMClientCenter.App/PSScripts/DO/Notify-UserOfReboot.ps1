#Requires -Version 5.1
<#
.SYNOPSIS
    Notifies the logged-on user that a reboot is required.

.DESCRIPTION
    The original version called .\SCToastNotification.exe, a helper binary
    from the source project that doesn't ship as part of CMClientCenter, so
    that line would always fail; the line below it (msg *) was the only
    code that actually ran, and the real toast-notification logic was left
    entirely commented out. This uses Windows' native toast notification
    API (Windows.UI.Notifications), which needs no extra binary or module
    and works the same way on Windows 10 and 11.

    Toast notifications can only be raised in the logged-on user's own
    session, not from a SYSTEM context — and CMClientCenter's WinRM
    sessions typically run as SYSTEM. If the toast call throws for that
    reason (or any other, e.g. no interactive session at all), this falls
    back to msg *, which does work from SYSTEM.
#>

$title = 'Reboot Required'
$message = 'Your PC needs to restart as soon as possible to finish applying updates.'

try {
    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
    [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

    $toastXml = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
    $textNodes = $toastXml.GetElementsByTagName('text')
    $textNodes.Item(0).AppendChild($toastXml.CreateTextNode($title)) | Out-Null
    $textNodes.Item(1).AppendChild($toastXml.CreateTextNode($message)) | Out-Null

    $toast = [Windows.UI.Notifications.ToastNotification]::new($toastXml)
    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('CMClientCenter').Show($toast)

    Write-Output 'Reboot notification toast sent.'
} catch {
    Write-Warning "Toast notification failed ($($_.Exception.Message)) — falling back to msg *."
    msg * "$title : $message"
}
