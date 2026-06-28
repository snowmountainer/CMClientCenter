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
                                + PSScripts/  — Built-in "Run PS" Skript-Bibliothek (loose .ps1, Copy-to-Output)
src/CMClientCenter.Core/       Services, Interfaces, Models
src/CMClientCenter.PowerShell/ Runspace Engine + Executors + PS-Skripte (Embedded Resources)
src/CMClientCenter.Shared/     DTOs, Enums, Result<T>
tests/CMClientCenter.Core.Tests/       Unit Tests für Core (Scaffold vorhanden, noch keine Tests)
tests/CMClientCenter.PowerShell.Tests/ Unit Tests für Runspace/Executors (Scaffold vorhanden, noch keine Tests)
```

## Wichtige Konventionen

- **Result<T>** Pattern für alle Service-Rückgaben (kein Exception-Bubbling bis UI)
- **AsyncRelayCommand** für alle async ViewModel-Aktionen
- **[ObservableProperty] public partial T PropertyName { get; set; }** für alle ViewModel-Properties
  (nicht der ältere `private T _field`-Stil — der ist nicht AOT/WinRT-marshalling-freundlich, siehe MVVMTK0045)
- **PSObjectMapper** für PSObject → C# Model Konvertierung — **immer `Unwrap()` vor Type-Checks**, sonst schlägt z.B. `GetDateTime()` auf yyyyMMdd-Strings fehl
- WinUI 3 Pages: bevorzugt `x:Bind` (compiled bindings), kein `Binding`.
  Ausnahme: gruppierte `ListView`s (`CollectionViewSource.IsSourceGrouped`) setzen
  `ItemsSource` im Code-Behind, da `x:Bind` auf eine in `Page.Resources` deklarierte
  `CollectionViewSource` nicht zuverlässig funktioniert (siehe `ConsolePage.xaml.cs`)
- **PS-Skripte — drei Quellen, nicht verwechseln:**
  - `src/CMClientCenter.PowerShell/Scripts/*.ps1` → **Embedded Resource**, geladen via `EmbeddedScripts.Load("Name.ps1")`, für die App-eigenen Pages (Dashboard, Hardware, Actions, ...)
  - `src/CMClientCenter.App/PSScripts/**/*.ps1` → lose Dateien, `<Content CopyToOutputDirectory="PreserveNewest">`, Built-in-Bibliothek für die Console-Seite ("Run PS — Built-in Scripts"), Quelle: [sccmclictr](https://github.com/rzander/sccmclictr) (Ms-PL)
  - `%LOCALAPPDATA%\CMClientCenter\Scripts` (konfigurierbar) → Benutzer-eigene Skripte, "Run PS — Custom Scripts"
- Bei nested arrays in `PSCustomObject`s über WinRM: **immer separate Skript-Aufrufe mit flachen Objekten**, nie verschachtelte Arrays zurückgeben — unzuverlässig über WinRM-Serialisierung (siehe `Get-CCMApplications.ps1`)

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

- [x] Theme-Toggle (Light/Dark/System) in Settings — erledigt, inkl. Restart-Hinweis für Application-level Brushes
- [x] Console-Seite (Open Console + Run PS, Built-in- und Custom-Skripte, Output kopierbar, Output-Panel per Drag größenverstellbar)
- [ ] `NotNullToBoolConverter` — referenziert in früheren Notizen, im aktuellen Code nicht (mehr) vorhanden; bei Bedarf neu anlegen oder Referenz entfernen
- [ ] LogsPage: CCM Log-Viewer hat Filter + Severity-Färbung, aber noch kein "tail -f"-Äquivalent (Live-Update bei wachsender Logdatei)
- [ ] SettingsPage: WinRM HTTPS-Support, Credential-Manager-Integration (aktuell nur Pass-Through Kerberos/NTLM)
- [ ] AgentStatusPage: Health-Checks vorhanden (Service/Client/Network/Cache/Inventory/Updates/System) — Vollständigkeit gegenüber dem Original-Tool noch nicht final verifiziert
- [ ] HardwarePage: Disk-Liste ist im Model (`HardwareInfo.Disks`) vorhanden, aber noch nicht im XAML dargestellt
- [ ] Unit Tests für Executors (mit Mock-Runspace) — `tests/CMClientCenter.Core.Tests` und `tests/CMClientCenter.PowerShell.Tests` sind als Projekt-Scaffold angelegt, enthalten aber noch keine Tests
- [x] Console-Seite: Output-Panel-Breite (Drag-Splitter) wird nicht in `AppSettings` persistiert — nach Neustart wieder Standardbreite (420px)

## WMI-Namespaces (Referenz)

| Namespace                  | Wichtige Klassen                          |
|----------------------------|-------------------------------------------|
| ROOT\ccm                   | CCM_Client, CCM_InstalledComponent        |
| ROOT\ccm\clientsdk         | CCM_ClientUtilities, CCM_SoftwareUpdate, CCM_Program (Task Sequences, gefiltert auf `TaskSequence=True` — `CCM_TaskSequence` existiert in diesem Environment nicht) |
| ROOT\ccm\SoftMgmtAgent     | CacheConfig (Size bereits in MB — **nicht** durch 1024 teilen) |
| ROOT\ccm\SoftwareUpdates\UpdatesStore | CCM_UpdateStatus (Scan-History, **nicht** dedupliziert — `Group-Object` nach Title+RevisionNumber+Status mit neuestem ScanTime nötig) |
| ROOT\ccm\locationservices  | SMS_MPInformation (Fallback-Quelle für SiteCode/MP, falls `SMS_Client.AssignedSite` leer ist) |
| ROOT\ccm\policy\machine\.. | CCM_Authority (für Management Point)      |

**Allgemeine WMI/CIM-Hinweise** (nicht standortspezifisch — gelten unabhängig von Site Code oder Management Point):
- `ClientVersion` über `SMS_Client` lesen, nicht über `CCM_Client` — `SMS_Client.ClientVersion` ist die von Microsoft dokumentierte Standard-Property dafür; `CCM_Client` ist für anderes gedacht (Health-/Installations-Status) und führt `ClientVersion` nicht zuverlässig
- `EnforcePreference`-CIM-Methodenparameter muss als `UInt32` typisiert sein
- Site Code und Management Point werden **immer zur Laufzeit ermittelt, nie hartcodiert**: `SMS_Client.AssignedSite` zuerst versuchen, falls leer (kommt je nach Client-Installationsart vor) auf `SMS_MPInformation.SiteCode`/`.MP` zurückfallen (siehe `Get-CMAgentStatus.ps1`). `TT1` / `VSRV-SCCM-002.TINUTEST.LOCAL` in früheren Notizen waren nur Beispielwerte aus der Entwicklungsumgebung, nicht Teil des Codes.

## WinRM / PowerShell-Serialisierung

- Verschachtelte Arrays in `PSCustomObject`s sind über WinRM unzuverlässig — immer separate Skript-Aufrufe mit flachen Objekten zurückgeben (Ursache für leere Applications-Liste über WinRM, behoben mit eigenem `Get-CCMApplications.ps1`)
- `PSObjectMapper` muss `Unwrap()` vor Type-Checks aufrufen, sonst schlägt `GetDateTime()` auf yyyyMMdd-Strings fehl

## WinUI 3 Constraints

- `Application.RequestedTheme` muss vor dem ersten `Window.Activate()` gesetzt werden — zur Laufzeit gesetzt wirft es eine `COMException`
- UI-Zugriff nach `await` (z.B. `RestartBar.IsOpen = true`) braucht `DispatcherQueue.TryEnqueue` — WinUI 3 hat keinen automatischen `SynchronizationContext`-Capture wie WPF/WinForms
- Einen Schließen-`Button` direkt in `InfoBar.Content` einzubetten verursacht `InvalidCastException` im generierten `.g.cs` (Connect-ID-Mehrdeutigkeit) — als Sibling außerhalb der `InfoBar` platzieren
- `x:Bind`-Funktionsbindung braucht `IValueConverter` (z.B. `BoolToVisibilityConverter`), implementiert in `Converters/`
- `AppInstance.Restart()` hat einen `FileNotFoundException`-Bug bei unpackaged self-contained Apps — `Process.Start()` + `Environment.Exit(0)` verwenden
- `GridSplitter` ist **kein** Bestandteil des nativen Windows App SDK, nur über das separate `CommunityToolkit.WinUI`-Paket — und hat dort einen dokumentierten Absturz in Kombination mit `ListView` im selben `Grid`. Für Resize-Handles stattdessen einen eigenen Pointer-Event-Splitter bauen (siehe `ConsolePage.xaml.cs`)
- `TextBlock` unterstützt standardmäßig keine Textauswahl/Kopieren:
  - Für einen einzelnen, zusammenhängenden Ausgabe-Block (z.B. Script-Output, Console-Page) eine `TextBox` mit `IsReadOnly="True"`, `BorderThickness="0"`, `Background="Transparent"` verwenden statt `TextBlock`
  - Für `TextBlock`s **innerhalb eines `ListView.ItemTemplate`** (mehrspaltiges Zeilen-Layout, z.B. Logs-Page) stattdessen `IsTextSelectionEnabled="True"` direkt auf den jeweiligen `TextBlock`s setzen — das erlaubt Markieren/Strg+C pro Spalte ohne das Grid-Layout (Severity-Farbbalken etc.) zu zerstören. `ListViewBase.IsTextSelectionEnabled` existiert in WinUI 3/Windows App SDK **nicht** (nur UWP-Altlast) — nicht verwenden.
  - Da `TextBlock.IsTextSelectionEnabled` nur innerhalb eines einzelnen `TextBlock` markierbar macht (kein zeilenübergreifendes Markieren wie bei `TextBox`), zusätzlich einen "Copy All"-Button (kopiert alle gefilterten Zeilen als Tab-separierten Text) und ein Rechtsklick-Kontextmenü "Copy line" pro Zeile anbieten (siehe `LogsPage.xaml(.cs)`)
  - `Clipboard.SetContent()` (`Windows.ApplicationModel.DataTransfer`) in try/catch wrappen — kann in unpackaged Apps theoretisch `CO_E_NOTINITIALIZED` werfen, falls außerhalb des STA-UI-Threads aufgerufen; `Program.cs` mit `[STAThread]` + synchronem `Main` (bereits vorhanden) ist die eigentliche Absicherung

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
