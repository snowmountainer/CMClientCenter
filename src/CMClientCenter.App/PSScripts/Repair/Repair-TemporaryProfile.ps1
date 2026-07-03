#Requires -Version 5.1
<#
.SYNOPSIS
    Fixes temporary/BAK profile entries in the registry so Windows uses
    the correct profile on next logon.

.DESCRIPTION
    When Windows can't load a user profile it creates a temporary one and
    leaves the real profile key with a ".bak" suffix in the ProfileList
    registry hive. This script finds those ".bak" keys, renames the
    original (non-.bak) key to ".temp", then renames the ".bak" key back
    to the original name, which is what Windows needs to find the profile.

    Changes from the original:
    - Removed the final 'Remove-Item C:\Users\Temp*' line — deleting
      folders from C:\Users\ has nothing to do with fixing registry profile
      keys, and silently wiping profile-looking folders is destructive.
    - Fixed a Do/While logic inversion: the original looped While
      ($TempKeyExists -eq $false), meaning it deleted the .temp key as long
      as it *didn't* exist — the condition should be While ($TempKeyExists
      -eq $true), i.e. keep trying to remove it until it's gone.
    - $null comparisons put on the left per PowerShell convention.
    - Write-Host replaced with Write-Output (compatible with the
      CMClientCenter runspace output stream).
    - Write-Host -ForegroundColor removed (no color in the Console page
      output panel).
#>

function Repair-TemporaryProfile {
    $profileListPath = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
    $bakKeys = Get-ChildItem -Path $profileListPath | Where-Object { $_.Name -clike '*.bak' }

    if ($null -eq $bakKeys) {
        Write-Warning 'No temporary/BAK profile keys found — fix does not apply to this computer.'
        return
    }

    foreach ($key in $bakKeys) {
        $pathBak      = $key.Name
        $nameOriginal = $key.PSChildName -replace '\.bak$'
        $pathOriginal = $pathBak -replace '\.bak$'
        $pathTemp     = $pathOriginal + '.temp'
        $nameTemp     = $nameOriginal + '.temp'

        # Remove any pre-existing .temp key first (up to 3 attempts).
        $retry = 3
        do {
            $tempExists = Test-Path -Path "Registry::$pathTemp"
            if ($tempExists) {
                Remove-Item -Path "Registry::$pathTemp" -Force -Recurse -Confirm:$false -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 1
            }
            $retry--
        } while ($tempExists -and $retry -gt 0)

        $tempExists = Test-Path -Path "Registry::$pathTemp"
        if (-not $tempExists) {
            Rename-Item -Path "Registry::$pathOriginal" -NewName $nameTemp  -Force
            Rename-Item -Path "Registry::$pathBak"      -NewName $nameOriginal -Force
        }

        $bakStillExists = Test-Path -Path "Registry::$pathBak"
        if (-not $bakStillExists) {
            Write-Output "Fixed. SID: $nameOriginal — please restart the computer for the change to take effect."
        } else {
            Write-Warning "Could not fix SID: $nameOriginal — rename the .bak key manually."
        }
    }
}

Repair-TemporaryProfile
