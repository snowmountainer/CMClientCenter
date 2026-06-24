# Releasing CMClientCenter

This describes how to build and publish a release ZIP (e.g. v0.1.0.0) as a
GitHub Release asset. **This must run on Windows** with Visual Studio 2022+
(Windows application development workload) or the standalone .NET 10 SDK +
Windows 10/11 SDK installed — it cannot run on Linux/macOS/WSL.

The release artifact is **only the built app** (CMClientCenter.exe + its
dependencies + the `PSScripts\` script library) — not a source code archive.
Anyone who wants to build from source already has the full repository.

## 1. Build the release

```powershell
cd CMClientCenter
.\scripts\publish-release.ps1 -Version 0.1.0.0
```

This produces:
- `publish\CMClientCenter-0.1.0.0-win-x64\` — the runnable folder
- `publish\CMClientCenter-0.1.0.0-win-x64.zip` — the same thing, zipped

The `publish\` folder is gitignored — it's build output, never commit it.

## 2. Smoke-test before publishing

Run `CMClientCenter.exe` from the published folder, ideally on a machine or
VM that has never had the Windows App SDK runtime installed, to make sure
the self-contained deployment actually carries everything it needs:

- App launches without a missing-runtime prompt
- Connect to a local or remote computer
- Open the Console page, confirm the built-in script list isn't empty
  (`scripts\publish-release.ps1` already warns you if `PSScripts\` ended up
  empty, but a visual check doesn't hurt)
- Toggle the theme in Settings, confirm the restart flow works

## 3. Create the GitHub Release

1. On GitHub: **Releases → Draft a new release**
2. Tag: `v0.1.0.0` (target: whatever commit you just tested)
3. Title: `v0.1.0.0` (or a short name, e.g. "First preview")
4. Description: what's in this first version — see the Features table in
   `README.md` for a ready-made list, plus anything from `CLAUDE.md`'s
   "Bekannte Einschränkungen / TODOs" section worth calling out as known
   limitations for this early release
5. Mark as **Pre-release** (this is a 0.x first preview, not a stable 1.0)
6. Attach `CMClientCenter-0.1.0.0-win-x64.zip` as a release asset
7. Publish

## Notes on what's deliberately NOT in the release build

- **Languages:** `Directory.Build.props` sets `SatelliteResourceLanguages`
  to `en;de;fr;it`, which trims .NET/library satellite resource assemblies
  to those four languages. The Windows App SDK's own resources are not
  affected by this setting due to a known limitation
  ([microsoft/WindowsAppSDK#4288](https://github.com/microsoft/WindowsAppSDK/issues/4288))
  when `WindowsAppSDKSelfContained=true` + `WindowsPackageType=None` — those
  still ship for every language WinAppSDK itself supports. There's currently
  no supported way around this for this deployment model.
- **No installer / MSIX:** the app is unpackaged (xcopy-deployable). Users
  unzip and run `CMClientCenter.exe` directly — no Start menu entry, no
  auto-update, no uninstaller. That's a deliberate trade-off for a first
  preview; see `README.md`'s requirements section.
- **No PublishSingleFile:** considered, but the core Windows App SDK native
  binaries can't be merged into the single EXE regardless, so the practical
  size/file-count benefit is small — and it adds complexity (extraction to a
  temp folder on first launch, possible AV false positives on self-extracting
  EXEs) that isn't worth it for an early preview where straightforward
  debugging matters more than a slightly tidier file listing.
