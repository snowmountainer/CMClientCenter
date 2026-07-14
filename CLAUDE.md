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

## PSScripts-Bibliothek (Console-Page "Run PS")

- Built-in-Scripts werden über `ps.AddScript(scriptContent)` + `ps.InvokeAsync()` ausgeführt (`RunspaceManager.InvokeRawAsync`, genutzt von `ConsoleExecutor.RunCustomScriptAsync`) — das ist Datei-Semantik, nicht Pipeline-Semantik. `return` auf Top-Level (außerhalb einer Funktion) ist hier gültig und beendet das Script wie bei einer dot-gesourcten `.ps1`-Datei.
- `schtasks.exe` signalisiert Fehler über den Exit-Code, **wirft aber nie eine PowerShell-Exception** — `try { schtasks /Run ... } catch { ... }` fängt den Fehlerfall nie ab. Stattdessen `$LASTEXITCODE` nach dem Aufruf prüfen.
- Destruktive "Fix"-Scripts gezielt statt global wirken lassen: SCCM-Policy-Cache-Probleme über `ROOT\ccm\Policy\Machine\RequestedConfig`/`ActualConfig` (per WMI) leeren statt den gesamten lokalen GPO-Ordner (`C:\Windows\System32\GroupPolicy\*`) zu löschen — letzteres nimmt alle GPO-Settings mit, nicht nur SCCM-relevante.
- Firewall-Reparatur-Scripts: gezielt einzelne Firewall-Regelgruppen aktivieren (`Get-NetFirewallRule -Group ... | Enable-NetFirewallRule`), niemals `Set-NetFirewallProfile -Enabled False` als "Fix" einsetzen — das deaktiviert die Firewall komplett und dauerhaft.
- `wuauclt.exe` ist seit Windows 10 1809 ein wirkungsloser Stub — `/ResetAuthorization`, `/DetectNow`, `/reportnow` haben keinen Effekt mehr; nicht mehr verwenden, WMI-Schedule-Trigger (`SMS_Client.TriggerSchedule`) reichen aus.
- `appidsvc` (Application Identity) heißt unter diesem Namen seit Win10/11 oft nicht mehr direkt ansprechbar — vor `Stop-Service`/`Start-Service`-Aufrufen auf Dienstnamen immer `-ErrorAction SilentlyContinue` setzen, da sich Dienstnamen über Windows-Versionen ändern können.
- `quser`/`query user`-Textausgabe nie per fixer `.Substring(n, m)`-Zeichenposition parsen (bricht bei langen Benutzernamen oder Locale-Änderungen) — Spaltenstart stattdessen über `IndexOf()` auf der Header-Zeile ermitteln.
- Site-Server/DP-Dienste (WSUS, IIS `W3SVC`, `MSSQL$MICROSOFT##WID`) gehören nicht in die Client-Script-Bibliothek (`PSScripts/`) — eigener Ordner `PSScripts-SiteServer/`, der **nicht** im `.csproj` per `<Content Include>` referenziert wird, damit er nicht mit ausgeliefert wird und nicht in der "Run PS"-Liste auftaucht.
- Toast-Benachrichtigungen über `[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]` funktionieren in PowerShell 5.1 nativ ohne Zusatzmodul (anders als in PowerShell Core/7, wo die WinRT-Assemblies fehlen) — aber **nur in der Session des angemeldeten Benutzers**, nicht aus einem SYSTEM-Kontext heraus. Da CMClientCenter-WinRM-Sessions typischerweise als SYSTEM laufen, jeden Toast-Aufruf in try/catch mit `msg *`-Fallback wrappen.
- `while`-Schleifen ohne Timeout/Max-Retry sind ein wiederkehrendes Bug-Muster in mehreren der Original-Scripts (Dienst-Stop/Start-Polling) — immer mit fester Obergrenze (`for`-Schleife mit Attempt-Counter) statt unbegrenztem `while` umsetzen.
- Bei parallelen/kopierten Codeblöcken (z.B. zwei fast identische Scheduled-Task-Definitionen) immer prüfen, ob Variablennamen beim Kopieren korrekt umbenannt wurden (`$T1.EndBoundary` vs. versehentlich `$T.EndBoundary`) — copy-paste-Tippfehler dieser Art sind leicht zu übersehen, da der Code syntaktisch gültig bleibt.

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

Quelle: `_scheduleIds` in `Executors.cs` (Trigger via `SMS_Client.TriggerSchedule`, siehe `Invoke-CMAction.ps1`). `CMAction.AllActions` in `Models.cs` hat zusätzlich ein `ActionCategory`-Flag (`Standard`/`Advanced`), das steuert, ob eine Action auf der Actions-Page in der Hauptliste oder im eingeklappten "Advanced Actions"-Expander landet.

**Standard** (immer registriert, entspricht der klassischen ConfigMgr-Systemsteuerung-Actions-Seite):

| Action                          | ScheduleId                              |
|----------------------------------|-----------------------------------------|
| Hardware Inventory Cycle         | {00000000-0000-0000-0000-000000000001}  |
| Software Inventory Cycle         | {00000000-0000-0000-0000-000000000002}  |
| Discovery Data Collection Cycle  | {00000000-0000-0000-0000-000000000003}  |
| File Collection Cycle            | {00000000-0000-0000-0000-000000000010}  |
| Machine Policy Retrieval Cycle   | {00000000-0000-0000-0000-000000000021}  |
| Machine Policy Evaluation Cycle  | {00000000-0000-0000-0000-000000000022}  |
| User Policy Retrieval Cycle      | {00000000-0000-0000-0000-000000000026}  |
| User Policy Evaluation Cycle     | {00000000-0000-0000-0000-000000000027}  |
| Software Metering Usage Report Cycle | {00000000-0000-0000-0000-000000000031} |
| Windows Installer Source List Update Cycle | {00000000-0000-0000-0000-000000000032} |
| Software Updates Deployment Evaluation Cycle | {00000000-0000-0000-0000-000000000108} |
| Software Updates Scan Cycle      | {00000000-0000-0000-0000-000000000113}  |
| Application Deployment Evaluation Cycle | {00000000-0000-0000-0000-000000000121} |

**Advanced** (nur registriert, wenn die zugehörige Komponente/Policy auf dem Client aktiv ist — siehe Hinweis unten):

| Action                          | ScheduleId                              |
|----------------------------------|-----------------------------------------|
| Software Updates Install Cycle (SUM) | {00000000-0000-0000-0000-000000000063} |
| DCM Policy                       | {00000000-0000-0000-0000-000000000110}  |
| Send Unsent State Messages       | {00000000-0000-0000-0000-000000000111}  |
| State System Policy Cache Cleanout | {00000000-0000-0000-0000-000000000112} |
| Update Store Policy              | {00000000-0000-0000-0000-000000000114}  |
| State System Bulk Send (High)    | {00000000-0000-0000-0000-000000000115}  |
| State System Bulk Send (Low)     | {00000000-0000-0000-0000-000000000116}  |
| Application Manager User Policy Action | {00000000-0000-0000-0000-000000000122} |
| Application Manager Global Evaluation | {00000000-0000-0000-0000-000000000123} |
| Power Management Start Summarizer | {00000000-0000-0000-0000-000000000131} |
| Endpoint Protection Deployment Reevaluate | {00000000-0000-0000-0000-000000000221} |
| Endpoint AM Policy Reevaluate    | {00000000-0000-0000-0000-000000000222}  |
| External Event Detection         | {00000000-0000-0000-0000-000000000223}  |

**Bewusst nicht als Action aufgenommen:** Site-/DP-seitige Schedules ({...061} Peer DP Status, {...062} Peer DP Pending Package Check, {...109} PDP Maintenance — auf einem normalen Client wirkungslos/no-op) sowie reine interne Wartungsjobs ohne Mehrwert als manueller Button ({...011} IDMIF, {...023}–{...025}, {...037}, {...040}–{...043}, {...051}). Ausnahme: {...040} (Machine Policy Agent Cleanup) wird als Teil der zusammengesetzten "Reset Policy"-Tools-Aktion verwendet (siehe unten), nicht als eigenständige Actions-Page-Zeile.

**WBEM_E_NOT_FOUND (0x80041002) ist bei TriggerSchedule kein Bug, sondern erwartetes Verhalten** für Schedules, die der Client nur dynamisch registriert, wenn die zugehörige Komponente/Policy aktiv ist — z.B. User-Policy-Schedules ohne interaktive Logon-Session, DCM Policy ohne deployte Compliance-Baseline, Endpoint-Protection-Schedules ohne installierte EP-Komponente, SUM Install Cycle ohne anstehende Update-Installation. `BuildErrorMessage()` in `ViewModels.cs` matcht dafür auf den Hex-HResult (nicht auf den lokalisierten Exception-Text — `[wmiclass]`-Fehlermeldungen sind OS-Sprache-abhängig) und zeigt einen erklärenden Hinweis statt der rohen WMI-Fehlermeldung. `Invoke-CMAction.ps1` hängt den HResult deshalb immer als fixen Hex-Code an die Message an.

**Reset Policy** (Tools-Page, "Client Policy"-Karte, `Invoke-CCMTool.ps1` Case `"ResetPolicy"`): zusammengesetzte Aktion, mirrort "Reset Policy" aus dem Original Client Center — `SMS_Client.ResetPolicy(uFlags=1)`, dann `TriggerSchedule` auf {...040} (Machine Policy Agent Cleanup) und {...021} (Machine Policy Retrieval Cycle), in dieser Reihenfolge. Bewusst auf der Tools- statt der Actions-Page, da mehrstufig und mit Nebenwirkung (Client ist bis zum Re-Download kurzzeitig ohne Assignments) — passt zum bestehenden Muster von Clear Cache/Repair/Reinstall, nicht zu den 1:1-Schedule-Triggern auf Actions.

**Projekt-Konvention:** Alle nutzersichtbaren Strings (Button-Labels, InfoBar-Texte, Fehlermeldungen, Log-Zeilen) sind konsequent Englisch — unabhängig von der Sprache der Konversation mit Claude/Entwickler beim Erstellen des Features.
