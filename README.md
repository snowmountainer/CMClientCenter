# CMClientCenter
<img src="assets/images/icon.svg" align="right" width="120" alt="CMClientCenter Logo">

Modern rebuild of [sccmclictr (Client Center for Configuration Manager)](https://github.com/rzander/sccmclictr) using **WinUI 3** and **PowerShell in-process Runspaces**.

Built for Workplace Engineers who need a fast, modern tool to inspect and manage ConfigMgr clients — locally and remotely.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![WinUI](https://img.shields.io/badge/WinUI-3-0078D4)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1%2B-5391FE)
![License](https://img.shields.io/badge/license-GPL--3.0-blue)
[![Latest Release](https://img.shields.io/github/v/release/snowmountainer/CMClientCenter?include_prereleases)](https://github.com/snowmountainer/CMClientCenter/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/snowmountainer/CMClientCenter/total)](https://github.com/snowmountainer/CMClientCenter/releases)

<img alt="CMClientCenter Dashboard" src="assets/images/Dashboard.png">

<details>
<summary>🌙 Show Dark Mode</summary>
<img alt="CMClientCenter Dashboard (Dark)" src="assets/images/Dashboard_Dark.png">
</details>

---

> [!WARNING]
> **CMClientCenter can trigger destructive, hard-to-undo actions** on the
> machines it connects to — resetting the Client GUID, purging cached policy,
> repairing/reinstalling the CM client, forced reboots, and OSD task sequence
> deployment. It is provided "as is", without warranty of any kind (see
> [License](#license)). **Test against a non-production client or a lab
> environment first**, and make sure whoever runs it understands what each
> action does before pointing it at production. Actions with irreversible or
> high-impact effects (e.g. OSD) already require an in-app confirmation
> dialog, but that's not a substitute for knowing what you're about to do.

---

## Features

| Page | Description |
|---|---|
| **Dashboard** | CM Agent version, Site Code, Management Point, Cache, Hardware overview |
| **Agent Status** | Grouped health checks — Service, Client, Network, Cache, Inventory, Updates, System |
| **Hardware** | System, CPU, RAM slots, GPU, Disks, OS with full UBR build number |
| **Software** | Installed apps with filter and install date |
| **Actions** | Trigger CM client schedules — 13 Standard actions (Machine/User Policy, HW/SW Inventory, Discovery, File Collection, Software Metering, Source List Update, Software/Application Updates) plus a collapsible "Advanced" section with 13 further schedules (SUM install, DCM policy, Endpoint Protection, state messaging, ...) for troubleshooting |
| **Software Center** | Applications (Install/Repair/Uninstall) and Operating Systems (Task Sequences, incl. OSD with high-impact confirmation dialog) |
| **Updates** | All Updates / Pending Updates, with per-update Install action |
| **Tools** | Clear CCM cache, Client repair/reinstall, Reset Policy (purge cached machine policy + force re-download), Pending reboot |
| **Console** | **Open Console** — interactive remote PowerShell session (`Enter-PSSession`) in a new window, pass-through Kerberos/NTLM. **Run PS** — built-in script library (70 scripts from the original Client Center tool, grouped by folder) plus your own custom `.ps1` scripts from a configurable folder, run against the connected computer with live, copyable output |
| **Logs** | CCM log viewer with CMTrace format parsing, filter, color-coded severity, separate tabs for CCM Client / CCMSetup / PSADT logs, selectable/copyable entries (per-field selection, "Copy All", right-click "Copy line") |
| **Settings** | Light/Dark/System theme, custom scripts folder location |

## Screenshots

Click **🌙 Dark** under any screenshot to toggle it.

<table>
<tr>
<td width="50%">

**Dashboard**
<img alt="Dashboard" src="assets/images/Dashboard.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Dashboard (Dark)" src="assets/images/Dashboard_Dark.png">
</details>

</td>
<td width="50%">

**Agent Status**
<img alt="Agent Status" src="assets/images/Agent%20Status.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Agent Status (Dark)" src="assets/images/Agent%20Status_Dark.png">
</details>

</td>
</tr>
<tr>
<td width="50%">

**Hardware**
<img alt="Hardware" src="assets/images/Hardware.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Hardware (Dark)" src="assets/images/Hardware_Dark.png">
</details>

</td>
<td width="50%">

**Software**
<img alt="Software" src="assets/images/Software.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Software (Dark)" src="assets/images/Software_Dark.png">
</details>

</td>
</tr>
<tr>
<td width="50%">

**Actions**
<img alt="Actions" src="assets/images/Actions.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Actions (Dark)" src="assets/images/Actions_Dark.png">
</details>

</td>
<td width="50%">

**Software Center**
<img alt="Software Center" src="assets/images/Software%20Center.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Software Center (Dark)" src="assets/images/Software%20Center_Dark.png">
</details>

</td>
</tr>
<tr>
<td width="50%">

**Updates**
<img alt="Updates" src="assets/images/Updates.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Updates (Dark)" src="assets/images/Updates_Dark.png">
</details>

</td>
<td width="50%">

**Tools**
<img alt="Tools" src="assets/images/Tools.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Tools (Dark)" src="assets/images/Tools_Dark.png">
</details>

</td>
</tr>
<tr>
<td width="50%">

**Console**
<img alt="Console" src="assets/images/Console.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Console (Dark)" src="assets/images/Console_Dark.png">
</details>

</td>
<td width="50%">

**Logs**
<img alt="Logs" src="assets/images/Logs.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Logs (Dark)" src="assets/images/Logs_Dark.png">
</details>

</td>
</tr>
<tr>
<td width="50%">

**Settings**
<img alt="Settings" src="assets/images/Settings.png">
<details>
<summary>🌙 Dark</summary>
<img alt="Settings (Dark)" src="assets/images/Settings_Dark.png">
</details>

</td>
<td width="50%"></td>
</tr>
</table>

## Installation

**Download the latest release:** [github.com/snowmountainer/CMClientCenter/releases/latest](https://github.com/snowmountainer/CMClientCenter/releases/latest)

Two artifacts are attached to every release:

| Artifact | Use case | How |
|---|---|---|
| `CMClientCenter-<version>-win-x64-Setup.msi` | Normal install — Start Menu shortcut, clean uninstall via Add/Remove Programs, silent-deployable | Double-click, or `msiexec /i CMClientCenter-<version>-win-x64-Setup.msi /quiet` for Intune/MECM/GPO deployment. Installs to `C:\Program Files\snowmountainer\CMClientCenter`, requires admin rights |
| `CMClientCenter-<version>-win-x64.zip` | Portable / xcopy-deploy, no install, no admin rights needed to unpack | Unzip anywhere, run `CMClientCenter.App.exe` directly |

Both are self-contained — no separate .NET or Windows App SDK runtime install
needed. See [Requirements](#requirements) below for OS/rights prerequisites.

Building from source instead (e.g. to contribute) is covered under
[Build](#build).

## Requirements

- Windows 10 1809+ / Windows 11
- .NET 10 SDK
- Visual Studio 2022+ with **Windows Application Development** workload
- Admin rights (for WMI/CCM access)

## Remote Connection (WinRM)

Connect to remote machines using pass-through Kerberos/NTLM — no credentials dialog needed when running from an admin server in the same domain.

TrustedHosts are set automatically on first connection. For manual setup:

```powershell
# On the admin server (once):
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "*.yourdomain.local" -Force
```

## Build

```powershell
# Restore packages
dotnet restore CMClientCenter.sln

# Build (x64 required for WinUI 3)
dotnet build CMClientCenter.sln --configuration Release --arch x64

# Run
dotnet run --project src/CMClientCenter.App --arch x64
```

## Architecture

```
CMClientCenter.App          WinUI 3 Frontend (Views, ViewModels, Controls)
CMClientCenter.Core         Services, Interfaces, Models
CMClientCenter.PowerShell   Runspace Engine, Executors, PS Scripts (Embedded Resources)
CMClientCenter.Shared       DTOs, Enums, Result<T>
```

**PowerShell scripts** come from three places, each serving a different purpose:

| Source | Location | Used by | Editable? |
|---|---|---|---|
| Embedded resources | `CMClientCenter.PowerShell/Scripts/*.ps1` | The app's own pages (Dashboard, Hardware, Actions, ...) | No — compiled into the app |
| Built-in script library | `CMClientCenter.App/PSScripts/**/*.ps1` (loose files next to the .exe) | "Console" page → "Run PS — Built-in Scripts" | Yes — originally from [Client Center for Configuration Manager](https://github.com/rzander/sccmclictr) (Ms-PL, see `PSScripts/LICENSE-and-SOURCE.md`) |
| Custom scripts folder | `%LOCALAPPDATA%\CMClientCenter\Scripts` by default, configurable in Settings | "Console" page → "Run PS — Custom Scripts" | Yes — your own scripts, subfolders are grouped automatically |

All scripts are compatible with **PS 5.1 and PS 7+**.

## Tests

```
tests/CMClientCenter.Core.Tests        Unit tests for Core services/models
tests/CMClientCenter.PowerShell.Tests  Unit tests for the Runspace engine/executors
```

Project scaffolding is in place; test coverage is still being built out.

## Contributing

Bug reports, feature requests, and PRs are welcome — see
[CONTRIBUTING.md](CONTRIBUTING.md) for how to get set up and what to know
before opening a PR.

Found a security issue? Please don't open a public issue — see
[SECURITY.md](SECURITY.md) for how to report it.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for what's new in each release.

## Acknowledgements

Inspired by [Client Center for Configuration Manager](https://github.com/rzander/sccmclictr) by Roger Zander.

## License

GPL-3.0 — see [LICENSE.txt](LICENSE.txt)
