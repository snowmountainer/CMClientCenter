# Get-InstalledSoftware.ps1
# InstallDate is passed as a string — C# handles the parsing

$paths = @(
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
)

$paths | ForEach-Object {
    Get-ItemProperty $_ -ErrorAction SilentlyContinue
} | Where-Object { $_.DisplayName -and $_.DisplayName -ne "" } |
    Select-Object @{N="Name";       E={$_.DisplayName}},
                  @{N="Version";    E={$_.DisplayVersion}},
                  @{N="Publisher";  E={$_.Publisher}},
                  @{N="InstallDate";E={
                      # Pass as a plain string (yyyyMMdd) — no DateTime cast
                      if ($_.InstallDate -and $_.InstallDate -match '^\d{8}$') {
                          $_.InstallDate
                      } else { "" }
                  }} |
    Sort-Object Name
