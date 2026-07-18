#Requires -Version 5.1
<#
.SYNOPSIS
    Builds a self-contained, unpackaged release of CMClientCenter and
    packages it as both a ZIP and an MSI installer (e.g. for a GitHub
    Release).

.DESCRIPTION
    Runs `dotnet publish` for the App project only (Core/PowerShell/Shared
    are pulled in automatically via project references — they are NOT meant
    to be published as their own apps). The output folder is everything an
    end user needs to run the app: CMClientCenter.App.exe, its DLLs, the
    Windows App SDK runtime files (bundled in via WindowsAppSDKSelfContained),
    and the PSScripts\ folder with the built-in "Run PS" script library.

    That same staged output is then packaged two ways: a ZIP (xcopy-deploy,
    same as before) and an MSI built from installer\Package.wxs (installs to
    C:\Program Files\snowmountainer\CMClientCenter, creates an All Users
    Start Menu shortcut, supports silent install via msiexec /quiet for
    Intune/MECM/GPO deployment). Use -SkipInstaller to produce just the ZIP.

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

.PARAMETER SkipInstaller
    Skips building CMClientCenter-Setup.msi and produces only the ZIP, e.g.
    on a machine that doesn't have the WiX Toolset available. The ZIP has
    always been the primary artifact; the MSI is additive.

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

    [string]$Configuration = "Release",

    [switch]$SkipInstaller
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

$exePath = Join-Path $publishDir "CMClientCenter.App.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish finished but CMClientCenter.App.exe is missing from $publishDir something is wrong with the publish profile."
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
# .pdb files (debug symbols) are excluded from the distributed ZIP to keep it
# smaller — they stay in $publishDir itself in case you need to debug locally
# from this exact build. Compress-Archive has no built-in exclude filter, so
# we stage a temp copy without the .pdb files (and trimmed language folders,
# see below) instead.

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

$stagingDir = Join-Path $repoRoot "publish\.staging-$Version"
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }

Write-Host "==> Staging release contents (excluding .pdb symbol files)" -ForegroundColor Cyan
Copy-Item $publishDir $stagingDir -Recurse
Get-ChildItem $stagingDir -Recurse -Filter *.pdb | Remove-Item -Force

# Windows App SDK ships its own per-language *.mui resource folders (one per
# language it supports, ~80 of them) regardless of SatelliteResourceLanguages
# — a known limitation (microsoft/WindowsAppSDK#4288) for
# WindowsAppSDKSelfContained=true + WindowsPackageType=None builds like this
# one. There's no supported MSBuild property to limit this at build time, so
# we trim it here instead. NOT an officially supported approach — if app
# startup or any WinAppSDK control ever behaves oddly after this, the
# language-folder trim below is the first thing to suspect; comment out this
# block to rule it in or out.
#
# Deliberately an explicit allow-list of known WinAppSDK language folder
# names, NOT a regex pattern matching "looks like a language code" — a
# pattern like that also matches "ref" (the .NET reference-assemblies
# folder, NOT a language folder) and could delete something the runtime
# actually needs. List taken from an actual publish output of this project;
# if a future Windows App SDK version adds new languages, this list (and the
# $keepLanguageFolders below it) may need a one-time update.
$allWinAppSdkLanguageFolders = @(
    "af-ZA","am-ET","ar-SA","as-IN","az-Latn-AZ","bg-BG","bn-IN","bs-Latn-BA",
    "ca-ES","ca-Es-VALENCIA","cs-CZ","cy-GB","da-DK","de-DE","el-GR","en-GB",
    "en-us","es-ES","es-MX","et-EE","eu-ES","fa-IR","fi-FI","fil-PH","fr-CA",
    "fr-FR","ga-IE","gd-gb","gl-ES","gu-IN","he-IL","hi-IN","hr-HR","hu-HU",
    "hy-AM","id-ID","is-IS","it-IT","ja-JP","ka-GE","kk-KZ","km-KH","kn-IN",
    "ko-KR","kok-IN","lb-LU","lo-LA","lt-LT","lv-LV","mi-NZ","mk-MK","ml-IN",
    "mr-IN","ms-MY","mt-MT","nb-NO","ne-NP","nl-NL","nn-NO","or-IN","pa-IN",
    "pl-PL","pt-BR","pt-PT","quz-PE","ro-RO","ru-RU","sk-SK","sl-SI","sq-AL",
    "sr-Cyrl-BA","sr-Cyrl-RS","sr-Latn-RS","sv-SE","ta-IN","te-IN","th-TH",
    "tr-TR","tt-RU","ug-CN","uk-UA","ur-PK","uz-Latn-UZ","vi-VN","zh-CN","zh-TW"
)
# .NET/library satellite resource folders use bare two-letter codes (de, fr,
# it, ...) — Directory.Build.props already limits these to en/de/fr/it at
# build time, so "de"/"fr"/"it" never even get created; "en" doesn't either
# since English needs no satellite folder. Nothing further to trim there.
$keepLanguageFolders = @("en-us", "en-GB", "de-DE", "fr-FR", "it-IT")
$removedFolders = Get-ChildItem $stagingDir -Directory | Where-Object {
    $_.Name -in $allWinAppSdkLanguageFolders -and $_.Name -notin $keepLanguageFolders
}
if ($removedFolders) {
    Write-Host "==> Trimming $($removedFolders.Count) Windows App SDK language folder(s) (keeping: $($keepLanguageFolders -join ', '))" -ForegroundColor Cyan
    $removedFolders | Remove-Item -Recurse -Force
}

Write-Host "==> Creating $zipPath" -ForegroundColor Cyan
Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

$zipSizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "==> Done: $zipPath ($zipSizeMB MB)" -ForegroundColor Green

# --- Build the MSI installer -------------------------------------------------
# Reuses $stagingDir (same pdb-free, language-trimmed content as the ZIP) as
# the MSI's source, so the two release artifacts never drift apart. Built
# from source here (not restored from a prior build) with `dotnet build`
# against the WixToolset.Sdk-based .wixproj — requires the WiX v5 MSBuild SDK
# to already be resolvable via NuGet (first run downloads it automatically,
# no separate `wix` CLI install needed).

$msiPath = $null
if (-not $SkipInstaller) {
    Write-Host "==> Building CMClientCenter-Setup.msi" -ForegroundColor Cyan

    # MSI ProductVersion is strictly Major.Minor.Build (3 fields, each
    # capped — Windows Installer has no 4th field like AssemblyVersion does).
    # A 4-part -Version (e.g. 0.1.0.0, matching this script's own examples)
    # is truncated here rather than rejected, since the ZIP/folder name is
    # allowed to keep all 4 parts and there's no reason to force callers to
    # pass two different version strings for one release.
    $parsedVersion = [version]$Version
    $msiVersion = "{0}.{1}.{2}" -f $parsedVersion.Major, $parsedVersion.Minor, [Math]::Max($parsedVersion.Build, 0)

    $installerProject = Join-Path $repoRoot "installer\CMClientCenter.Installer.wixproj"
    $installerOutDir  = Join-Path $repoRoot "publish\.installer-build-$Version"
    if (Test-Path $installerOutDir) { Remove-Item $installerOutDir -Recurse -Force }

    dotnet build $installerProject `
        --configuration $Configuration `
        -p:PublishDir=$stagingDir `
        -p:ProductVersion=$msiVersion `
        -p:RepoRoot=$repoRoot `
        --output $installerOutDir

    if ($LASTEXITCODE -ne 0) {
        throw "MSI build failed with exit code $LASTEXITCODE  see output above. Re-run with -SkipInstaller to produce just the ZIP while you investigate."
    }

    $builtMsi = Join-Path $installerOutDir "CMClientCenter-Setup.msi"
    if (-not (Test-Path $builtMsi)) {
        throw "dotnet build reported success but $builtMsi is missing  check the wixproj OutputName matches."
    }

    $msiPath = Join-Path $repoRoot "publish\CMClientCenter-$Version-win-x64-Setup.msi"
    Copy-Item $builtMsi $msiPath -Force
    Remove-Item $installerOutDir -Recurse -Force

    $msiSizeMB = [math]::Round((Get-Item $msiPath).Length / 1MB, 1)
    Write-Host "==> Done: $msiPath ($msiSizeMB MB)" -ForegroundColor Green
} else {
    Write-Host "==> Skipping MSI build (-SkipInstaller)" -ForegroundColor Yellow
}

Remove-Item $stagingDir -Recurse -Force

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Smoke-test: run $exePath on a clean-ish machine/VM (one that doesn't already have the Windows App SDK runtime installed, to catch missing-dependency issues)."
if ($msiPath) {
    Write-Host "  2. Smoke-test the installer too: msiexec /i `"$msiPath`" /quiet /log install.log on a clean VM, confirm it lands in C:\Program Files\snowmountainer\CMClientCenter and creates the All Users Start Menu shortcut, then msiexec /x `"$msiPath`" /quiet to confirm a clean uninstall."
    Write-Host "  3. Create a GitHub Release tagged v$Version and attach both $zipPath and $msiPath as release assets."
    Write-Host "  4. Do NOT commit the publish\ folder to the repo  it's build output, not source (see .gitignore)."
} else {
    Write-Host "  2. Create a GitHub Release tagged v$Version and attach $zipPath as a release asset."
    Write-Host "  3. Do NOT commit the publish\ folder to the repo  it's build output, not source (see .gitignore)."
}
