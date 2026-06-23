using CMClientCenter.Core.Models;
using CMClientCenter.Shared.Enums;
using CMClientCenter.Shared.Results;

namespace CMClientCenter.Core.Interfaces;

public interface IRunspaceManagerService
{
    Task<Result<RunspaceInitResult>> InitializeAsync(TargetComputer target, string? password = null, CancellationToken ct = default);
    ValueTask DisposeAsync();
}

public record RunspaceInitResult(string? OSVersion, string? PSVersion);

public interface IConnectionService
{
    Task<Result<ConnectionResult>> ConnectAsync(string target, string? username = null, string? password = null, CancellationToken ct = default);
    Task DisconnectAsync();
    bool IsConnected { get; }
    TargetComputer? CurrentTarget { get; }
    event EventHandler<ConnectionResult>? ConnectionStateChanged;
}

public interface ICMAgentService
{
    Task<Result<CMAgentInfo>> GetAgentInfoAsync(CancellationToken ct = default);
}

public interface IHardwareService
{
    Task<Result<HardwareInfo>> GetHardwareInfoAsync(CancellationToken ct = default);
}

public interface ISoftwareService
{
    Task<Result<List<SoftwareItem>>> GetInstalledSoftwareAsync(CancellationToken ct = default);
}

public interface IActionService
{
    Task<Result> TriggerActionAsync(CMActionType action, CancellationToken ct = default);
    IReadOnlyList<CMAction> GetAvailableActions();
}

public interface IAgentHealthService
{
    Task<Result<List<HealthCheck>>> GetHealthChecksAsync(CancellationToken ct = default);
}

public interface ILogService
{
    Task<Result<List<LogFileInfo>>> GetLogFilesAsync(CancellationToken ct = default);
    Task<Result<List<LogEntry>>> GetLogEntriesAsync(string logName, int maxLines = 200, CancellationToken ct = default);
}

public interface IToolsService
{
    Task<Result<CCMToolsInfo>> GetToolsInfoAsync(CancellationToken ct = default);
    Task<Result> InvokeToolAsync(string action, CancellationToken ct = default);
}

// Software Center: SCCM/MECM Applications (and, later, Task Sequences /
// Operating Systems) that the user can install, repair, or uninstall —
// the equivalent of the native Windows "Software Center" app.
public interface ISoftwareCenterService
{
    Task<Result<List<CCMApplication>>> GetApplicationsAsync(CancellationToken ct = default);
    Task<Result> InvokeApplicationAsync(string appId, string revision, string action, CancellationToken ct = default);

    Task<Result<List<CCMTaskSequence>>> GetTaskSequencesAsync(CancellationToken ct = default);
    Task<Result> InvokeTaskSequenceAsync(string programId, string packageId, CancellationToken ct = default);
}

// "Updates" page — "All Updates" / "Pending Updates", analog to the old
// "Client Center for Configuration Manager" tool's Updates view.
// See Get-CCMSoftwareUpdates.ps1 for why two WMI classes are combined.
public interface IUpdatesService
{
    Task<Result<List<CCMSoftwareUpdate>>> GetUpdatesAsync(CancellationToken ct = default);
    Task<Result> InstallUpdateAsync(string updateId, CancellationToken ct = default);
}

public interface IAppSettingsService
{
    AppSettings Current { get; }

    // Resolved, always-non-empty scripts folder: AppSettings.ScriptsFolder
    // if set, otherwise %LOCALAPPDATA%\CMClientCenter\Scripts.
    string EffectiveScriptsFolder { get; }

    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
    event EventHandler<AppSettings>? SettingsChanged;
}

// "Console" page — mirrors the old "Client Center for Configuration Manager"
// tool's "Open Console" (interactive Enter-PSSession in a new powershell.exe
// window) and "Run PS" (built-in + user-supplied .ps1 scripts).
public interface IConsoleService
{
    // Opens a new powershell.exe window running Enter-PSSession against the
    // given host. Uses the current Windows identity (Kerberos/NTLM
    // pass-through) — no password is stored or re-entered, consistent with
    // how the existing WinRM connection in RunspaceWrappers.cs behaves when
    // no explicit credential is supplied.
    Result OpenConsole(string hostname);

    // Lists *.ps1 files from BOTH sources, newest-first within each group:
    //   - Built-in:  <app folder>\PSScripts\**  (shipped with the app, read-only)
    //   - Custom:    IAppSettingsService.EffectiveScriptsFolder\**  (user-supplied)
    // CustomScriptInfo.IsBuiltin distinguishes the two for the UI's grouping.
    Task<Result<List<CustomScriptInfo>>> GetCustomScriptsAsync(CancellationToken ct = default);

    // Runs a script's content in the *current* runspace/session
    // (local or the already-connected remote target) — same execution
    // path as every other PS-Skript-Ausführung in der App, also no separate
    // process is spawned and the script runs PS 5.1-compatible as required.
    Task<Result<string>> RunCustomScriptAsync(string scriptPath, CancellationToken ct = default);
}

