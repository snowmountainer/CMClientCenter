# CMClientCenter
<img src="assets/images/icon.svg" align="right" width="120" alt="CMClientCenter Logo">

Modern rebuild of [sccmclictr (Client Center for Configuration Manager)](https://github.com/rzander/sccmclictr) using **WinUI 3** and **PowerShell in-process Runspaces**.

Built for Workplace Engineers who need a fast, modern tool to inspect and manage ConfigMgr clients — locally and remotely.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![WinUI](https://img.shields.io/badge/WinUI-3-0078D4)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1%2B-5391FE)
![License](https://img.shields.io/badge/license-GPL--3.0-blue)

---

## Features

| Page | Description |
|---|---|
| **Dashboard** | CM Agent version, Site Code, Management Point, Cache, Hardware overview |
| **Agent Status** | Grouped health checks — Service, Client, Network, Cache, Inventory, Updates, System |
| **Hardware** | System, CPU, RAM slots, GPU, Disks, OS with full UBR build number |
| **Software** | Installed apps with filter and install date |
| **Actions** | Trigger CM policies (Machine Policy, HW/SW Inventory, Updates, Discovery) |
| **Software Center** | Applications (Install/Repair/Uninstall) and Operating Systems (Task Sequences, incl. OSD with high-impact confirmation dialog) |
| **Updates** | All Updates / Pending Updates, with per-update Install action |
| **Tools** | Clear CCM cache, Client repair/reinstall, Pending reboot |
| **Console** | **Open Console** — interactive remote PowerShell session (`Enter-PSSession`) in a new window, pass-through Kerberos/NTLM. **Run PS** — built-in script library (70 scripts from the original Client Center tool, grouped by folder) plus your own custom `.ps1` scripts from a configurable folder, run against the connected computer with live, copyable output |
| **Logs** | CCM log viewer with CMTrace format parsing, filter, color-coded severity, separate tabs for CCM Client / CCMSetup / PSADT logs |
| **Settings** | Light/Dark/System theme, custom scripts folder location |

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

## Acknowledgements

Inspired by [Client Center for Configuration Manager](https://github.com/rzander/sccmclictr) by Roger Zander.

## License

GPL-3.0 — see [LICENSE.txt](LICENSE.txt)
