# CLAUDE.md — CMClientCenter

Kontext für Claude Code (claude.ai/code oder VS Code Extension).

## Projekt-Überblick

Moderner Rebuild von sccmclictr (rzander/sccmclictr) mit:
- **WinUI 3** (Windows App SDK 1.6) als Frontend
- **PowerShell In-Process Runspaces** (System.Management.Automation) als Backend
- **MVVM** mit CommunityToolkit.Mvvm
- **DI** mit Microsoft.Extensions.DependencyInjection

## Solution-Struktur

```
src/CMClientCenter.App/        WinUI 3 Frontend (Views, ViewModels, Controls)
src/CMClientCenter.Core/       Services, Interfaces, Models
src/CMClientCenter.PowerShell/ Runspace Engine + Executors + PS-Skripte
src/CMClientCenter.Shared/     DTOs, Enums, Result<T>
```

## Wichtige Konventionen

- **Result<T>** Pattern für alle Service-Rückgaben (kein Exception-Bubbling bis UI)
- **AsyncRelayCommand** für alle async ViewModel-Aktionen
- **EmbeddedScripts.Load("Name.ps1")** für PS-Skripte (keine externen Dateien)
- **PSObjectMapper** für PSObject → C# Model Konvertierung
- WinUI 3 Pages: immer `x:Bind` (compiled bindings), kein `Binding`
- Alle PS-Skripte in `src/CMClientCenter.PowerShell/Scripts/` → werden als Embedded Resource eingebettet

## Build

```bash
# Restore
dotnet restore CMClientCenter.sln

# Build (nur x64, WinUI3 benötigt Plattform-Angabe)
dotnet build CMClientCenter.sln --configuration Debug --arch x64

# Run
dotnet run --project src/CMClientCenter.App --arch x64
```

## Bekannte Einschränkungen / TODOs

- [ ] `NotNullToBoolConverter` in App.xaml noch definieren
- [ ] LogsPage: CCM Log-Viewer implementieren (tail -f Äquivalent)
- [ ] SettingsPage: WinRM HTTPS-Support, Credential-Manager
- [ ] AgentStatusPage: vollständige CCM Health-Checks
- [ ] HardwarePage: Detail-Ansicht mit Disk-Liste
- [ ] Theme-Toggle (Light/Dark) in Settings
- [ ] Unit Tests für Executors (mit Mock-Runspace)

## WMI-Namespaces (Referenz)

| Namespace                  | Wichtige Klassen                          |
|----------------------------|-------------------------------------------|
| ROOT\ccm                   | CCM_Client, CCM_InstalledComponent        |
| ROOT\ccm\clientsdk         | CCM_ClientUtilities, CCM_SoftwareUpdate   |
| ROOT\ccm\SoftMgmtAgent     | CacheConfig                               |
| ROOT\ccm\policy\machine\.. | CCM_Authority (für Management Point)      |

## CM Action Schedule IDs

| Action                | ScheduleId                              |
|-----------------------|-----------------------------------------|
| Machine Policy        | {00000000-0000-0000-0000-000000000021}  |
| Hardware Inventory    | {00000000-0000-0000-0000-000000000001}  |
| Software Inventory    | {00000000-0000-0000-0000-000000000002}  |
| Discovery Data        | {00000000-0000-0000-0000-000000000003}  |
| Update Deployment     | {00000000-0000-0000-0000-000000000108}  |
| Update Scan           | {00000000-0000-0000-0000-000000000113}  |
| App Deployment        | {00000000-0000-0000-0000-000000000121}  |
