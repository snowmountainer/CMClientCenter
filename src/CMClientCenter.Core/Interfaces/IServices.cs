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
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
    event EventHandler<AppSettings>? SettingsChanged;
}

