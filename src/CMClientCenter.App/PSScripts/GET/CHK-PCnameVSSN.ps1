#Requires -Version 5.1
<#
.SYNOPSIS
    Compares the BIOS serial number with the computer name to help spot
    machines whose name was supposed to be derived from the serial number
    but may have drifted.
#>

$serialNumber = (Get-WmiObject -Class Win32_BIOS).SerialNumber
$computerName = $env:COMPUTERNAME

if ($serialNumber -ne $computerName) {
    Write-Output "MISMATCH: ComputerName='$computerName'  SerialNumber='$serialNumber'"
} else {
    Write-Output "MATCH: ComputerName and SerialNumber are both '$computerName'."
}
