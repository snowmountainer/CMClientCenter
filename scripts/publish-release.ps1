#Requires -Version 5.1
<#
.SYNOPSIS
    Builds a self-contained, unpackaged release of CMClientCenter and zips it
    up as a release artifact (e.g. for a GitHub Release).

.DESCRIPTION
    Runs `dotnet publish` for the App project only (Core/PowerShell/Shared
    are pulled in automatically via project references — they are NOT meant
    to be published as their own apps). The output folder is everything an
    end user needs to run the app: CMClientCenter.exe, its DLLs, the Windows
    App SDK runtime files (bundled in via WindowsAppSDKSelfContained), and
    the PSScripts\ folder with the built-in "Run PS" script library.

    Requires the .NET SDK and the Windows App SDK / Windows 10/11 SDK
    components (Visual Studio 2022 with the "Windows application
    development" workload) to be installed — this script must run on
    Windows, it cannot run in WSL/Linux/macOS.

.PARAMETER Version
    Version string used only for the output folder/zip file name
    (e.g. "0.1.0.0"). Does NOT change any AssemblyVersion/FileVersion in the
    project — those are intentionally left untouched.

.PARAMETER Configuration
    Build configuration, defaults to Release.

.EXAMPLE
    .\publish-release.ps1 -Version 0.1.0.0

.NOTES
    If you hit "The platform 'AnyCPU' is not supported for Self Contained
    mode" (a known Windows App SDK quirk, see
    github.com/microsoft/WindowsAppSDK#3026), it means -p:Platform=x64 above
    didn't take effect for some reason — try adding --arch x64 to the
    dotnet publish call as well, or run `dotnet build -c Release --arch x64`
    once first to make sure the x64 configuration exists.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot "src\CMClientCenter.App\CMClientCenter.App.csproj"
$publishDir = Join-Path $repoRoot "publish\CMClientCenter-$Version-win-x64"
$zipPath    = Join-Path $repoRoot "publish\CMClientCenter-$Version-win-x64.zip"

if (-not (Test-Path $appProject)) {
    throw "Could not find $appProject run this script from its own location inside the repo (scripts\publish-release.ps1) so `$repoRoot resolves correctly."
}

Write-Host "==> Publishing CMClientCenter $Version ($Configuration, win-x64, self-contained, unpackaged)" -ForegroundColor Cyan

if (Test-Path $publishDir) {
    Write-Host "==> Removing previous output at $publishDir" -ForegroundColor Yellow
    Remove-Item $publishDir -Recurse -Force
}

# WindowsPackageType=None + WindowsAppSDKSelfContained=true are already set
# in the .csproj — passing them again here is redundant but harmless, and
# makes this command self-documenting if you ever copy just the command line
# elsewhere. SatelliteResourceLanguages comes from Directory.Build.props.
# PublishReadyToRun is left at its default (false) — R2R roughly doubles
# assembly size for a startup-time tradeoff that isn't worth it here, and
# (per community reports) has caused launch failures in some unpackaged
# WinUI 3 + WindowsAppSDKSelfContained configurations.
dotnet publish $appProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    -p:Platform=x64 `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE see output above."
}

# --- Sanity checks ---------------------------------------------------------

$exePath = Join-Path $publishDir "CMClientCenter.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish finished but CMClientCenter.exe is missing from $publishDir something is wrong with the publish profile."
}

$psScriptsDir = Join-Path $publishDir "PSScripts"
$scriptCount  = if (Test-Path $psScriptsDir) {
    (Get-ChildItem $psScriptsDir -Recurse -Filter *.ps1).Count
} else { 0 }

if ($scriptCount -eq 0) {
    Write-Warning "PSScripts folder is missing or has no .ps1 files in the publish output  the Console page's built-in script library will be empty. Check the <Content Include=`"PSScripts\**\*.ps1`"> item in CMClientCenter.App.csproj."
} else {
    Write-Host "==> PSScripts: $scriptCount built-in script(s) included" -ForegroundColor Green
}

$sizeMB = [math]::Round((Get-ChildItem $publishDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
Write-Host "==> Publish output: $publishDir ($sizeMB MB)" -ForegroundColor Green

# --- Zip it up --------------------------------------------------------------

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host "==> Creating $zipPath" -ForegroundColor Cyan
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

$zipSizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "==> Done: $zipPath ($zipSizeMB MB)" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Smoke-test: run $exePath on a clean-ish machine/VM (one that doesn't already have the Windows App SDK runtime installed, to catch missing-dependency issues)."
Write-Host "  2. Create a GitHub Release tagged v$Version and attach $zipPath as a release asset."
Write-Host "  3. Do NOT commit the publish\ folder to the repo  it's build output, not source (see .gitignore)."
