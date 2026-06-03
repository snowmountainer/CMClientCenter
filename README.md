# CMClientCenter

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
| **Tools** | Clear CCM cache, Client repair/reinstall, Pending reboot, CCM Applications |
| **Logs** | CCM log viewer with CMTrace format parsing, filter, color-coded severity |

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

**PowerShell scripts** are embedded as resources in `CMClientCenter.PowerShell` and are compatible with **PS 5.1 and PS 7+**.

## Acknowledgements

Inspired by [Client Center for Configuration Manager](https://github.com/rzander/sccmclictr) by Roger Zander.

## License

GPL-3.0 — see [LICENSE.txt](LICENSE.txt)
