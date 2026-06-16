# Get-CCMApplications.ps1 — Separates Script für Applications
# PS 5.1 kompatibel

try {
    $apps = Get-CimInstance -Namespace "ROOT\ccm\clientsdk" -ClassName "CCM_Application" -ErrorAction SilentlyContinue
    if ($apps -eq $null) { return }

    foreach ($app in @($apps)) {
        [PSCustomObject]@{
            Id              = [string]$app.Id
            Revision        = [string]$app.Revision
            Name            = [string]$app.Name
            Publisher       = [string]$app.Publisher
            SoftwareVersion = [string]$app.SoftwareVersion
            InstallState    = [string]$app.InstallState
            ResolvedState   = [string]$app.ResolvedState
        }
    }
} catch {}
