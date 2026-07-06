#Requires -Version 5.1
<#
.SYNOPSIS
    Reports the on-disk size of each user profile directory under
    C:\Users.
#>

Get-ChildItem -Path 'C:\Users' -Directory | ForEach-Object {
    $profileDir = $_
    $bytes = (Get-ChildItem -Path $profileDir.FullName -Recurse -Force -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum -ErrorAction SilentlyContinue).Sum

    [PSCustomObject]@{
        Profile  = $profileDir.Name
        Path     = $profileDir.FullName
        SizeMB   = [Math]::Round($bytes / 1MB, 2)
        SizeGB   = [Math]::Round($bytes / 1GB, 2)
    }
} | Sort-Object SizeMB -Descending | Format-Table -AutoSize | Out-String -Width 200
