#Requires -Version 5.1
<#
.SYNOPSIS
    Reports where this client gets its software update content from
    (its WUAHandler content source, or the WSUS server set via policy).

.DESCRIPTION
    The original version's outer catch block set Status/ContentLocation/
    ContentVersion a second time after the inner try/catch already had,
    which could silently overwrite a successful inner result with the
    outer exception message. This sets each property exactly once.
#>

$result = [PSCustomObject]@{
    ComputerName    = $env:ComputerName
    Status          = $null
    ContentLocation = $null
    ContentVersion  = $null
}

try {
    $wua = Get-WmiObject -Namespace 'ROOT\CCM\SoftwareUpdates\WUAHandler' -Class CCM_UpdateSource -ErrorAction Stop
    $result.Status          = 'OK'
    $result.ContentLocation = $wua.ContentLocation
    $result.ContentVersion  = $wua.ContentVersion
} catch {
    try {
        $wuPolicy = Get-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -ErrorAction Stop
        $result.Status          = 'OK'
        $result.ContentLocation = if ($wuPolicy.WUServer) { $wuPolicy.WUServer } else { 'No Server (policy key present but WUServer empty)' }
        $result.ContentVersion  = 'N/A'
    } catch {
        $result.Status          = $_.Exception.Message
        $result.ContentLocation = 'N/A'
        $result.ContentVersion  = 'N/A'
    }
}

$result
